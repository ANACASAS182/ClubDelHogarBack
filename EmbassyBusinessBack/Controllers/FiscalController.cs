using Dapper;
using EBDTOs;
using EmbassyBusinessBack.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Security.Claims;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection; // <= IMPORTANTE

namespace EmbassyBusinessBack.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class FiscalController : ControllerBase
    {
        private static string Cs() => DataAccess.ConnectionString(); // ajusta a tu helper
        private readonly string _constanciasRoot;
        private readonly string? _publicBase;
        private readonly IMemoryCache _cache;

        [ActivatorUtilitiesConstructor]
        public FiscalController(IConfiguration cfg, IMemoryCache cache)
        {
            _constanciasRoot = cfg.GetValue<string>("Storage:ConstanciasPath")
                               ?? Path.Combine(AppContext.BaseDirectory, "constancias");
            Directory.CreateDirectory(_constanciasRoot);
            _publicBase = cfg.GetValue<string>("PublicBaseUrl");
            _cache = cache;
        }
        private string BuildFullPath(string nameOrRelative)
        {
            if (Path.IsPathRooted(nameOrRelative)) return nameOrRelative;

            var clean = nameOrRelative.Replace('\\', Path.DirectorySeparatorChar)
                                      .Replace('/', Path.DirectorySeparatorChar);

            if (clean.Contains("..")) throw new InvalidOperationException("Ruta inválida.");
            
            var full = Path.GetFullPath(Path.Combine(_constanciasRoot, clean));
            if (!full.StartsWith(Path.GetFullPath(_constanciasRoot), StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Ruta fuera del storage.");

            return full;
        }


        public class RegimenItem
        {
            public string Clave { get; set; } = "";
            public string Descripcion { get; set; } = "";
        }

        // ======== Helpers ========
        private bool TryGetUserId(out long uid)
        {
            uid = 0;
            var keys = new[] { "UsuarioId", "UserId", ClaimTypes.NameIdentifier, "sub", "nameid" };
            foreach (var k in keys)
            {
                var val = HttpContext?.User?.FindFirst(k)?.Value;
                if (!string.IsNullOrWhiteSpace(val) && long.TryParse(val, out uid))
                    return true;
            }
            return false;
        }

        

        // ======== Catálogo de regímenes ========
        [AllowAnonymous]
        [HttpGet("regimenes")]
        public async Task<IActionResult> GetRegimenes([FromQuery] char? persona = null)
        {
            using var cn = new SqlConnection(Cs());
            var sql = @"
SELECT Clave, Descripcion
FROM fiscal.CatRegimenFiscal
WHERE (@persona IS NULL OR UPPER(Persona)=UPPER(@persona) OR UPPER(Persona)='A')
ORDER BY Clave";

            // 👇 Mapear a RegimenItem (no dinámico)
            var list = await cn.QueryAsync<RegimenItem>(sql, new { persona });

            // Si usas System.Text.Json con CamelCase (default), expondrá: clave, descripcion
            return Ok(GenericResponseDTO<IEnumerable<RegimenItem>>.Ok(list));
        }


        // ======== Obtener mis datos fiscales ========
        [HttpGet("mis-datos")]
        public async Task<IActionResult> GetMisDatos()
        {
            if (!TryGetUserId(out var uid))
                return Unauthorized(GenericResponseDTO<string>.Fail("No se pudo identificar al usuario.", true));

            const string sql = @"
SELECT TOP 1
    UsuarioID,
    NombreSAT,
    RFC,
    CURP,
    CAST(CodigoPostal AS nvarchar(5))   AS CodigoPostal,   -- 👈 evita int→string
    CAST(RegimenClave  AS nvarchar(10)) AS RegimenClave,   -- 👈 homogéneo para FE
    ConstanciaPath,
    ConstanciaHash,
    CAST(VerificadoSAT AS bit)          AS VerificadoSAT,  -- 👈 explícito
    FechaVerificacion,                                   -- (puede ser NULL)
    FechaCreacion,
    FechaActualizacion                                   -- (puede ser NULL)
FROM fiscal.UsuarioFiscal
WHERE UsuarioID = @uid";

            try
            {
                using var cn = new SqlConnection(Cs());
                var dato = await cn.QueryFirstOrDefaultAsync<UsuarioFiscalDTO>(sql, new { uid });
                return Ok(GenericResponseDTO<UsuarioFiscalDTO>.Ok(dato));
            }
            catch (Exception ex)
            {
                // Mientras depuras, deja el mensaje. Luego cámbialo a log.
                return StatusCode(500, ex.Message);
            }
        }

        // ======== Guardar datos fiscales ========
        [HttpPost("guardar")]
        public async Task<IActionResult> Guardar([FromBody] UsuarioFiscalDTO dto)
        {
            if (!TryGetUserId(out var uid))
                return Unauthorized(GenericResponseDTO<string>.Fail("No se pudo identificar al usuario.", true));

            dto.UsuarioID = uid;
            dto.RFC = (dto.RFC ?? "").Trim().ToUpperInvariant();
            dto.CURP = dto.CURP?.Trim().ToUpperInvariant();
            dto.CodigoPostal = (dto.CodigoPostal ?? "").Trim();
            dto.RegimenClave = (dto.RegimenClave ?? "").Trim();

            // 🔑 CURP: si quedó vacía, mándala como NULL para pasar el CHECK
            if (string.IsNullOrWhiteSpace(dto.CURP))
                dto.CURP = null;

            // validaciones
            if (string.IsNullOrWhiteSpace(dto.NombreSAT))
                return BadRequest(GenericResponseDTO<string>.Fail("Nombre SAT es requerido.", true));
            if (!(dto.RFC.Length == 12 || dto.RFC.Length == 13))
                return BadRequest(GenericResponseDTO<string>.Fail("RFC inválido.", true));
            if (!System.Text.RegularExpressions.Regex.IsMatch(dto.CodigoPostal, @"^\d{5}$"))
                return BadRequest(GenericResponseDTO<string>.Fail("Código Postal inválido.", true));
            if (string.IsNullOrWhiteSpace(dto.RegimenClave))
                return BadRequest(GenericResponseDTO<string>.Fail("RegimenClave requerido.", true));

            const string upsert = @"
IF EXISTS (SELECT 1 FROM fiscal.UsuarioFiscal WHERE UsuarioID=@UsuarioID)
BEGIN
  UPDATE fiscal.UsuarioFiscal
     SET NombreSAT=@NombreSAT, RFC=@RFC, CURP=@CURP,
         CodigoPostal=@CodigoPostal, RegimenClave=@RegimenClave
   WHERE UsuarioID=@UsuarioID;
END
ELSE
BEGIN
  INSERT fiscal.UsuarioFiscal(UsuarioID,NombreSAT,RFC,CURP,CodigoPostal,RegimenClave)
  VALUES(@UsuarioID,@NombreSAT,@RFC,@CURP,@CodigoPostal,@RegimenClave);
END";

            using var cn = new SqlConnection(Cs());
            await cn.ExecuteAsync(upsert, dto);
            return Ok(GenericResponseDTO<bool>.Ok(true));
        }

        // ======== Subir constancia (PDF) y guardar ruta + hash ========
        [DisableRequestSizeLimit]
        [HttpPost("subir-constancia")]
        public async Task<IActionResult> SubirConstancia(IFormFile file)
        {
            if (!TryGetUserId(out var uid)) return Unauthorized();
            if (file is null || file.Length == 0)
                return BadRequest(GenericResponseDTO<string>.Fail("Archivo vacío.", true));
            if (!string.Equals(file.ContentType, "application/pdf", StringComparison.OrdinalIgnoreCase))
                return BadRequest(GenericResponseDTO<string>.Fail("Solo PDF.", true));

            var fileName = $"constancia_{uid}_{DateTime.UtcNow:yyyyMMddHHmmss}.pdf";
            var fullPath = BuildFullPath(fileName);

            await using (var fs = System.IO.File.Create(fullPath))
                await file.CopyToAsync(fs);

            string hash;
            await using (var fs = System.IO.File.OpenRead(fullPath))
            using (var sha = System.Security.Cryptography.SHA256.Create())
                hash = BitConverter.ToString(sha.ComputeHash(fs)).Replace("-", "");

            using var cn = new SqlConnection(Cs());
            await cn.ExecuteAsync(
                @"UPDATE fiscal.UsuarioFiscal
            SET ConstanciaPath=@p, ConstanciaHash=@h
          WHERE UsuarioID=@uid;",
                new { p = fileName, h = hash, uid });

            // 👇 para que veas dónde se guardó realmente
            Response.Headers["X-Debug-Uid"] = uid.ToString();
            Response.Headers["X-Debug-FullPath"] = fullPath;

            return Ok(GenericResponseDTO<object>.Ok(new { path = fileName, hash }));
        }




        //------------- Obtener empresa

        [HttpGet("empresa/{empresaId:long}")]
        public async Task<IActionResult> GetEmpresaFiscal(long empresaId)
        {
            using var cn = new SqlConnection(Cs());
            var sql = @"SELECT TOP 1
                    EmpresaID,
                    RFC,
                    RazonSocialSAT,
                    CodigoPostal,
                    MetodoPago,
                    UsoCFDI,
                    RegimenClave
                FROM fiscal.EmpresaFiscal
                WHERE EmpresaID=@empresaId";
            var dato = await cn.QueryFirstOrDefaultAsync<EmpresaFiscalDTO>(sql, new { empresaId });
            return Ok(GenericResponseDTO<EmpresaFiscalDTO>.Ok(dato));
        }

        //------------- Visualizar PDF

        [HttpGet("constancia/descargar")]
        public async Task<IActionResult> DescargarConstancia()
        {
            if (!TryGetUserId(out var uid)) return Unauthorized();

            using var con = new SqlConnection(Cs());
            var stored = await con.ExecuteScalarAsync<string>(
                "SELECT ConstanciaPath FROM fiscal.UsuarioFiscal WHERE UsuarioID=@uid", new { uid });

            if (string.IsNullOrWhiteSpace(stored))
                return NotFound(GenericResponseDTO<string>.Fail("No hay constancia registrada.", true));

            var fullPath = BuildFullPath(stored);

            Response.Headers["X-Debug-Uid"] = uid.ToString();
            Response.Headers["X-Debug-FullPath"] = fullPath;

            if (!System.IO.File.Exists(fullPath))
                return NotFound(GenericResponseDTO<string>.Fail("Archivo no disponible.", true));

            var stream = System.IO.File.OpenRead(fullPath);
            var fileName = Path.GetFileName(fullPath);

            Response.Headers["Content-Disposition"] = $"attachment; filename=\"{fileName}\"";
            Response.Headers["X-Content-Type-Options"] = "nosniff";
            Response.Headers["Cache-Control"] = "no-store, no-cache, must-revalidate";
            Response.Headers["Pragma"] = "no-cache";

            return File(stream, "application/octet-stream", fileName, enableRangeProcessing: false);
        }


        [HttpGet("constancia/debug-path")]
        public async Task<IActionResult> DebugConstanciaPath()
        {
            if (!TryGetUserId(out var uid)) return Unauthorized();

            using var con = new SqlConnection(Cs());
            var stored = await con.ExecuteScalarAsync<string>(
                "SELECT ConstanciaPath FROM fiscal.UsuarioFiscal WHERE UsuarioID=@uid", new { uid });

            var full = string.IsNullOrWhiteSpace(stored) ? null : BuildFullPath(stored);
            var exists = !string.IsNullOrWhiteSpace(full) && System.IO.File.Exists(full);

            return Ok(new { stored, _constanciasRoot, full, exists });
        }


        [HttpGet("constancia")]
        public async Task<IActionResult> VerConstancia()
        {
            if (!TryGetUserId(out var uid)) return Unauthorized();

            using var con = new SqlConnection(Cs());
            var stored = await con.ExecuteScalarAsync<string>(
                "SELECT ConstanciaPath FROM fiscal.UsuarioFiscal WHERE UsuarioID=@uid", new { uid });

            if (string.IsNullOrWhiteSpace(stored))
                return NotFound(GenericResponseDTO<string>.Fail("No hay constancia registrada.", true));

            var fullPath = BuildFullPath(stored);            // 👈 usar helper
            if (!System.IO.File.Exists(fullPath))
                return NotFound(GenericResponseDTO<string>.Fail("Archivo no disponible en este nodo.", true));

            var fs = System.IO.File.OpenRead(fullPath);
            var fileName = Path.GetFileName(fullPath);

            // Fuerza descarga (iOS suele obedecer mejor así)
            Response.Headers["Content-Disposition"] = $"attachment; filename=\"{fileName}\"";
            Response.Headers["X-Content-Type-Options"] = "nosniff";
            // Desactiva ranges para evitar algunos visores integrados
            return File(fs, "application/octet-stream", fileName, enableRangeProcessing: false);
        }


        // crea un link temporal
        [HttpPost("constancia/link-descarga")]
        public IActionResult CrearLinkDescarga()
        {
            if (!TryGetUserId(out var uid)) return Unauthorized();

            var key = Guid.NewGuid().ToString("N");
            _cache.Set($"dl:{key}", uid, TimeSpan.FromMinutes(2)); // 2 min

            var baseUrl = string.IsNullOrWhiteSpace(_publicBase)
                ? $"{Request.Scheme}://{Request.Host.Value}"
                : _publicBase!.TrimEnd('/');

            var url = $"{baseUrl}/api/fiscal/constancia/descargar-by-key?key={key}";
            return Ok(GenericResponseDTO<object>.Ok(new { url }));
        }

        [AllowAnonymous]
        [HttpGet("constancia/descargar-by-key")]
        public async Task<IActionResult> DescargarByKey([FromQuery] string key)
        {
            if (string.IsNullOrWhiteSpace(key)) return Unauthorized();
            if (!_cache.TryGetValue($"dl:{key}", out long uid)) return Unauthorized();
            _cache.Remove($"dl:{key}");

            using var con = new SqlConnection(Cs());
            var stored = await con.ExecuteScalarAsync<string>(
                "SELECT ConstanciaPath FROM fiscal.UsuarioFiscal WHERE UsuarioID=@uid", new { uid });

            if (string.IsNullOrWhiteSpace(stored))
                return NotFound(GenericResponseDTO<string>.Fail("No hay constancia registrada.", true));

            var fullPath = BuildFullPath(stored);

            Response.Headers["X-Debug-Uid"] = uid.ToString();
            Response.Headers["X-Debug-FullPath"] = fullPath;

            if (!System.IO.File.Exists(fullPath))
                return NotFound(GenericResponseDTO<string>.Fail("Archivo no disponible.", true));

            var stream = System.IO.File.OpenRead(fullPath);
            var fileName = Path.GetFileName(fullPath);

            Response.Headers["Content-Disposition"] = $"attachment; filename=\"{fileName}\"";
            Response.Headers["X-Content-Type-Options"] = "nosniff";
            Response.Headers["Cache-Control"] = "no-store, no-cache, must-revalidate";
            Response.Headers["Pragma"] = "no-cache";

            return File(stream, "application/octet-stream", fileName, enableRangeProcessing: false);
        }



        //----------- Guardar empresa

        [HttpPost("empresa/guardar")]
        public async Task<IActionResult> GuardarEmpresa([FromBody] EmpresaFiscalDTO dto)
        {
            try
            {
                dto.RFC = (dto.RFC ?? "").Trim().ToUpperInvariant();
                dto.RazonSocialSAT = (dto.RazonSocialSAT ?? "").Trim();
                dto.CodigoPostal = (dto.CodigoPostal ?? "").Trim();
                dto.MetodoPago = dto.MetodoPago?.Trim().ToUpperInvariant();
                dto.UsoCFDI = dto.UsoCFDI?.Trim().ToUpperInvariant();
                dto.RegimenClave = (dto.RegimenClave ?? "").Trim();

                if (dto.EmpresaID <= 0) return BadRequest(GenericResponseDTO<string>.Fail("EmpresaID requerido.", true));
                if (!RfcValido12o13(dto.RFC))
                    return BadRequest(GenericResponseDTO<string>.Fail("RFC inválido (debe ser 12 o 13 caracteres).", true));
                if (string.IsNullOrWhiteSpace(dto.RazonSocialSAT)) return BadRequest(GenericResponseDTO<string>.Fail("Razón social requerida.", true));
                if (!System.Text.RegularExpressions.Regex.IsMatch(dto.CodigoPostal, @"^\d{5}$"))
                    return BadRequest(GenericResponseDTO<string>.Fail("Código Postal inválido.", true));
                if (string.IsNullOrWhiteSpace(dto.MetodoPago)) return BadRequest(GenericResponseDTO<string>.Fail("Método de pago requerido.", true));
                if (string.IsNullOrWhiteSpace(dto.UsoCFDI)) return BadRequest(GenericResponseDTO<string>.Fail("Uso de CFDI requerido.", true));

                const string upsert = @"
IF EXISTS (SELECT 1 FROM fiscal.EmpresaFiscal WHERE EmpresaID=@EmpresaID)
BEGIN
  UPDATE fiscal.EmpresaFiscal
     SET RFC=@RFC,
         RazonSocialSAT=@RazonSocialSAT,
         CodigoPostal=@CodigoPostal,
         MetodoPago=@MetodoPago,
         UsoCFDI=@UsoCFDI,
         RegimenClave=ISNULL(NULLIF(@RegimenClave,''), RegimenClave)
   WHERE EmpresaID=@EmpresaID;
END
ELSE
BEGIN
  INSERT fiscal.EmpresaFiscal (EmpresaID,RFC,RazonSocialSAT,CodigoPostal,MetodoPago,UsoCFDI,RegimenClave)
  VALUES (@EmpresaID,@RFC,@RazonSocialSAT,@CodigoPostal,@MetodoPago,@UsoCFDI,NULLIF(@RegimenClave,''));
END";

                using var cn = new SqlConnection(Cs());
                await cn.ExecuteAsync(upsert, dto);
                return Ok(GenericResponseDTO<bool>.Ok(true));
            }
            catch (Exception ex)
            {
                // deja esto para depurar; luego cámbialo por log + mensaje genérico
                return StatusCode(500, GenericResponseDTO<string>.Fail(ex.Message, true));
            }
        }

        // ------------ PARA PANEL DE ADMIN

        [HttpGet("~/api/usuario/{usuarioId:long}/fiscal")]
        public async Task<IActionResult> GetUsuarioFiscalById(long usuarioId)
        {
            const string sql = @"
SELECT TOP 1
    UsuarioID         AS UsuarioId,
    NombreSAT         AS NombreSAT,
    RFC               AS RFC,
    CURP              AS CURP,
    CAST(CodigoPostal AS nvarchar(5))   AS CodigoPostal,
    CAST(RegimenClave AS nvarchar(10))  AS RegimenClave,
    ConstanciaPath    AS ConstanciaPath,
    ConstanciaHash    AS ConstanciaHash,
    CAST(VerificadoSAT AS bit)          AS VerificadoSAT,
    FechaVerificacion,
    FechaCreacion,
    FechaActualizacion
FROM fiscal.UsuarioFiscal
WHERE UsuarioID = @usuarioId";
            using var cn = new SqlConnection(Cs());
            var dato = await cn.QueryFirstOrDefaultAsync<UsuarioFiscalDTO>(sql, new { usuarioId });
            return Ok(GenericResponseDTO<UsuarioFiscalDTO>.Ok(dato));
        }

        [HttpGet("~/api/usuario/{usuarioId:long}/bancos")]
        public async Task<IActionResult> GetBancosUsuario(long usuarioId)
        {
            const string sql = @"
SELECT 
    ID            AS Id,
    UsuarioID     AS UsuarioId,
    NombreBanco   AS NombreBanco,
    NombreTitular AS NombreTitular,
    NumeroCuenta  AS NumeroCuenta,
    CatBancoId    AS CatBancoId,
    BancoOtro     AS BancoOtro,
    TipoCuenta    AS TipoCuenta,
    FechaCreacion AS FechaCreacion,
    Eliminado     AS Eliminado
FROM dbo.BancoUsuario
WHERE UsuarioID = @usuarioId AND ISNULL(Eliminado,0)=0
ORDER BY FechaCreacion DESC";
            using var cn = new SqlConnection(Cs());
            var list = await cn.QueryAsync<BancoUsuarioDTO>(sql, new { usuarioId });
            return Ok(GenericResponseDTO<IEnumerable<BancoUsuarioDTO>>.Ok(list));
        }

        // 👇 sólo admins; ajusta el [Authorize] a tu esquema
        // [Authorize(Roles = "Admin")]   // (opcional) si quieres exigir rol
        [HttpGet("constancia/usuario/{usuarioId:long}")]
        public async Task<IActionResult> DescargarConstanciaDeUsuario(long usuarioId)
        {
            using var con = new SqlConnection(Cs());
            var stored = await con.ExecuteScalarAsync<string>(
                "SELECT ConstanciaPath FROM fiscal.UsuarioFiscal WHERE UsuarioID=@usuarioId",
                new { usuarioId });

            if (string.IsNullOrWhiteSpace(stored))
                return NotFound(GenericResponseDTO<string>.Fail("No hay constancia registrada.", true));

            var fullPath = BuildFullPath(stored);

            Response.Headers["X-Debug-Uid"] = usuarioId.ToString();
            Response.Headers["X-Debug-FullPath"] = fullPath;

            if (!System.IO.File.Exists(fullPath))
                return NotFound(GenericResponseDTO<string>.Fail("Archivo no disponible.", true));

            var stream = System.IO.File.OpenRead(fullPath);
            var fileName = Path.GetFileName(fullPath);

            Response.Headers["Content-Disposition"] = $"attachment; filename=\"{fileName}\"";
            Response.Headers["X-Content-Type-Options"] = "nosniff";
            Response.Headers["Cache-Control"] = "no-store, no-cache, must-revalidate";
            Response.Headers["Pragma"] = "no-cache";

            return File(stream, "application/octet-stream", fileName, enableRangeProcessing: false);
        }



       /* [HttpGet("constancia/usuario/{usuarioId:long}")]
        public async Task<IActionResult> DescargarConstanciaDeUsuario(long usuarioId)
        {
            using var con = new SqlConnection(Cs());
            var stored = await con.ExecuteScalarAsync<string>(
                "SELECT ConstanciaPath FROM fiscal.UsuarioFiscal WHERE UsuarioID=@usuarioId",
                new { usuarioId });

            if (string.IsNullOrWhiteSpace(stored))
                return NotFound(GenericResponseDTO<string>.Fail("No hay constancia registrada.", true));

            var fullPath = BuildFullPath(stored);  // usa tu helper para resolver ruta segura
            if (!System.IO.File.Exists(fullPath))
                return NotFound(GenericResponseDTO<string>.Fail("Archivo no disponible.", true));

            var fs = System.IO.File.OpenRead(fullPath);
            var fileName = Path.GetFileName(fullPath);

            Response.Headers["Content-Disposition"] = $"attachment; filename=\"{fileName}\"";
            Response.Headers["X-Content-Type-Options"] = "nosniff";
            return File(fs, "application/octet-stream", fileName, enableRangeProcessing: false);
        }*/


        //----------- Variable
        private static bool RfcValido12o13(string rfc) =>
        !string.IsNullOrWhiteSpace(rfc) &&
        System.Text.RegularExpressions.Regex.IsMatch(
            rfc.Trim().ToUpperInvariant(),
            @"^[A-ZÑ&]{3,4}\d{6}[A-Z0-9]{2,3}$",
        System.Text.RegularExpressions.RegexOptions.IgnoreCase);
    }
}