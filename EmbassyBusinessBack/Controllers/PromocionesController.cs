using EBDTOs;
using EmbassyBusinessBack.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore.Scaffolding.Metadata;
using QRCoder;
using System.Data;
using System.Drawing;
using System.Drawing.Imaging;


// PARA QR
using Microsoft.Extensions.Logging;
using System.Net.Mime;

// Twilio
using Twilio;
using Twilio.Rest.Api.V2010.Account;
// NO uses "using Twilio.Types;" aquí para evitar choques
// NO uses "using static QRCoder.PayloadGenerator;"
using TwilioPhoneNumber = Twilio.Types.PhoneNumber; // << ALIAS
//using static QRCoder.PayloadGenerator;



namespace EmbassyBusinessBack.Controllers
{
    [ApiController]
    [Route("api/[controller]")]              // base: /api/Promociones
    public class PromocionesController : ControllerBase
    {
        private readonly IConfiguration _cfg;
        private readonly ILogger<PromocionesController> _log;   // 👈 logger
        private const string QrFolder = @"C:\tmp\qr";

        public PromocionesController(IConfiguration cfg, ILogger<PromocionesController> log)
        {
            _cfg = cfg;
            _log = log;
        }

        // Usa PublicBaseUrl del appsettings; si está vacío, cae a la API pública.
        private string LandingBaseUrl
        {
            get
            {
                var v = _cfg["PublicBaseUrl"];
                if (string.IsNullOrWhiteSpace(v) || v == "/")
                    return "https://ebg-api.bithub.com.mx";   // fallback seguro
                return v.TrimEnd('/');
            }
        }

        private string BuildPublicUrlForQr(string codigo)
        {
            var baseUrl = LandingBaseUrl; // p.ej. https://ebg-api.bithub.com.mx
            var staticPath = Path.Combine(QrFolder, $"{codigo}.png");

            // Si ya existe, sirve estático; si no, usa el dinámico que siempre responde.
            return System.IO.File.Exists(staticPath)
                ? $"{baseUrl}/public/qr/{codigo}.png"
                : $"{baseUrl}/api/Promociones/qr/{codigo}";
        }


        [HttpGet("GetPromociones")]
        [AllowAnonymous]
        public async Task<IActionResult> GetPromociones(int UsuarioID)
        {
            List<PromocionDTO> promociones = new List<PromocionDTO>();

            string q = @"select 
Producto.ID, 
EmpresaID,
empresa.NombreComercial as EmpresaNombre,
isnull(empresa.logotipoBase64, '') as logotipoBase64,
Producto.Nombre,
Producto.Descripcion,
Producto.TipoComision,
isnull(ProductoComision.nivel_1, 0) as comisionCantidad,
isnull(ProductoComision.nivel_1, 0) as comisionPorcentaje,
Producto.fechaCaducidad
from Producto
left join ProductoComision on ProductoComision.ProductoID = Producto.ID
left join Empresa on empresa.ID = Producto.EmpresaID";

            promociones = DataAccess.fromQueryListOf<PromocionDTO>(q);

            foreach (PromocionDTO p in promociones)
            {




                if (p.TipoComision == 0)
                {
                    p.comision = "$" + p.comisionCantidad.ToString("N2") + " MXN";
                }

                if (p.TipoComision == 1)
                {
                    p.comision = p.comisionPorcentaje.ToString("N2") + "%";
                }

                p.vigencia = FechaHora(p.fechaCaducidad);
            }

            return Ok(promociones);
        }


        [HttpGet("GetResumenEmbajador")]
        [AllowAnonymous]
        public async Task<IActionResult> GetResumenEmbajador(int UsuarioID)
        {
            // Fechas “mock”
            DateTime fechaInicioMes = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            DateTime fechaPagoMes = fechaInicioMes.AddMonths(1).AddDays(5);

            var r = new ResumenEmbajador
            {
                embajadoresInvitados = new List<ResumenEmbajadorInvitacion>(),
                ingresosDirectos = 0m,
                ingresosIndirectos = 0m,
                proximaFechaPago = fechaPagoMes
            };

            // 1) INGRESOS DIRECTOS
            // Trae la comisión del producto (Nivel_1) como ComisionCantidad
            string qComision = $@"
        SELECT ISNULL(pc.Nivel_1, 0) AS ComisionCantidad
        FROM Referido r
        JOIN Producto p           ON p.ID = r.ProductoID
        LEFT JOIN ProductoComision pc ON pc.ProductoID = p.ID
        WHERE r.EstatusReferenciaID = 3
          AND ISNULL(r.EstatusPago, 0) = 0
          AND r.UsuarioID = {UsuarioID}";

            DataTable dtComision = DataAccess.performQuery(qComision);
            decimal ingresosDirectos = 0m;
            foreach (DataRow dr in dtComision.Rows)
            {
                var valor = dr.IsNull("ComisionCantidad") ? 0m : Convert.ToDecimal(dr["ComisionCantidad"]);
                ingresosDirectos += valor;
            }
            r.ingresosDirectos = ingresosDirectos;

            // 2) INVITACIONES PENDIENTES
            string qInvPend = $"SELECT CorreoElectronico, FechaHora FROM Invitaciones WHERE EmbajadorReferenteID = {UsuarioID}";
            DataTable dtInvPend = DataAccess.performQuery(qInvPend);
            foreach (DataRow dr in dtInvPend.Rows)
            {
                var inv = new ResumenEmbajadorInvitacion
                {
                    nombre = dr["CorreoElectronico"]?.ToString() ?? "",
                    fechaInvitacion = dr.IsNull("FechaHora") ? DateTime.MinValue : Convert.ToDateTime(dr["FechaHora"]),
                    estatus = "Pendiente"
                };
                r.embajadoresInvitados.Add(inv);
            }

            // 3) INVITACIONES ACEPTADAS
            string qInvAcep = $"SELECT Nombres, Apellidos, FechaCreacion FROM Usuario WHERE UsuarioParent = {UsuarioID}";
            DataTable dtInvAcep = DataAccess.performQuery(qInvAcep);
            foreach (DataRow dr in dtInvAcep.Rows)
            {
                var nombre = (dr["Nombres"]?.ToString() ?? "").Trim();
                var apell = (dr["Apellidos"]?.ToString() ?? "").Trim();

                var inv = new ResumenEmbajadorInvitacion
                {
                    nombre = $"{nombre} {apell}".Trim(),
                    fechaInvitacion = dr.IsNull("FechaCreacion") ? DateTime.MinValue : Convert.ToDateTime(dr["FechaCreacion"]),
                    estatus = "Aceptado"
                };
                r.embajadoresInvitados.Add(inv);
            }

            return Ok(r);
        }

        [HttpGet("qr/{codigo}")]
        [AllowAnonymous]
        public IActionResult GetQr(string codigo)
        {
            if (string.IsNullOrWhiteSpace(codigo)) return BadRequest();

            var path = Path.Combine(QrFolder, $"{codigo}.png");

            if (System.IO.File.Exists(path))
                return File(System.IO.File.ReadAllBytes(path), "image/png");

            var urlCodigo = $"{LandingBaseUrl}/val/{codigo}";

            var qrGen = new QRCodeGenerator();
            var qrData = qrGen.CreateQrCode(urlCodigo, QRCodeGenerator.ECCLevel.Q);
            var png = new PngByteQRCode(qrData);
            var bytes = png.GetGraphic(20);

            try
            {
                Directory.CreateDirectory(QrFolder);
                using var fs = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Read, 4096, FileOptions.WriteThrough);
                fs.Write(bytes, 0, bytes.Length);
                fs.Flush(true);
            }
            catch (Exception ex)
            {
                // Si no podemos persistir, igual devolvemos 200 con la imagen en memoria
                _log.LogWarning(ex, "No se pudo persistir el QR en {Path}", path);
            }

            return File(bytes, "image/png");
        }



        [HttpGet("GetPromocionesSocio")]
        [AllowAnonymous]
        public async Task<IActionResult> GetPromocionesSocio(int UsuarioID)
        {
            List<PromocionDTO> promociones = new List<PromocionDTO>();

            string q = $@"
    SELECT 
        p.ID,
        p.EmpresaID,
        e.NombreComercial                AS EmpresaNombre,
        ISNULL(e.logotipoBase64, '')     AS logotipoBase64,
        p.Nombre,
        p.Descripcion,
        p.FechaCaducidad                 AS fechaCaducidad,
        ISNULL(p.TipoComision, 0)        AS TipoComision,

        CASE WHEN ISNULL(p.TipoComision,0) = 0 
             THEN ISNULL(pc.nivel_1, 0) 
             ELSE 0 END                  AS comisionCantidad,

        CASE WHEN ISNULL(p.TipoComision,0) = 1 
             THEN ISNULL(pc.nivel_1, 0)
             ELSE 0 END                  AS comisionPorcentaje
    FROM Producto p
    LEFT JOIN ProductoComision pc ON pc.ProductoID = p.ID
    LEFT JOIN Empresa e           ON e.ID = p.EmpresaID
    LEFT JOIN UsuarioEmpresa ue   ON ue.EmpresaID = e.ID
    WHERE p.Eliminado = 0 AND ue.UsuarioID = {UsuarioID};";

            promociones = DataAccess.fromQueryListOf<PromocionDTO>(q);

            foreach (var p in promociones)
            {
                p.vigencia = FechaHora(p.fechaCaducidad);
                if (p.TipoComision == 1)
                    p.comision = p.comisionPorcentaje.ToString("0.##") + "%";
                else
                    p.comision = "$" + p.comisionCantidad.ToString("0.##") + " MXN";
            }

            return Ok(promociones);
        }

        [HttpGet("GetPromocionesEmpresa")]
        [AllowAnonymous]
        public async Task<IActionResult> GetPromocionesEmpresa(int EmpresaID)
        {
            List<PromocionDTO> promociones = new List<PromocionDTO>();

            string q = $@"
    SELECT 
        p.ID,
        p.EmpresaID,
        e.NombreComercial                AS EmpresaNombre,
        ISNULL(e.logotipoBase64, '')     AS logotipoBase64,
        p.Nombre,
        p.Descripcion,
        p.FechaCaducidad                 AS fechaCaducidad,
        ISNULL(p.TipoComision, 0)        AS TipoComision,        -- 0=MXN, 1=%

        -- VALOR: toma nivel_1, no Precio
        CASE WHEN ISNULL(p.TipoComision,0) = 0 
             THEN ISNULL(pc.nivel_1, 0) 
             ELSE 0 END                  AS comisionCantidad,

        CASE WHEN ISNULL(p.TipoComision,0) = 1 
             THEN ISNULL(pc.nivel_1, 0)
             ELSE 0 END                  AS comisionPorcentaje
    FROM Producto p
    LEFT JOIN ProductoComision pc ON pc.ProductoID = p.ID
    LEFT JOIN Empresa e           ON e.ID = p.EmpresaID
    WHERE p.Eliminado = 0 AND e.ID = {EmpresaID};";

            promociones = DataAccess.fromQueryListOf<PromocionDTO>(q);

            foreach (var p in promociones)
            {
                p.vigencia = FechaHora(p.fechaCaducidad);
                if (p.TipoComision == 1)
                    p.comision = p.comisionPorcentaje.ToString("0.##") + "%";
                else
                    p.comision = "$" + p.comisionCantidad.ToString("0.##") + " MXN";
            }

            return Ok(promociones);
        }


        [NonAction]
        private static string FechaHora(DateTime dt)
        {
            return dt.ToString("dd 'de' MMMM 'de' yyyy", new System.Globalization.CultureInfo("es-ES"));
        }

        [HttpPost("ConsultarEstatusDelCodigoQr")]
        [AllowAnonymous]
        public async Task<IActionResult> ConsultarEstatusDelCodigoQr([FromBody] ValidarPromocionQrRequest request)
        {
            var r = new RespuestaEstatusPromocion { estatus = -1, mensaje = "Ocurrió un error" };
            if (request is null || string.IsNullOrWhiteSpace(request.codigoPromocion))
                return BadRequest(new { mensaje = "codigoPromocion requerido" });

            await using var cn = new SqlConnection(DataAccess.ConnectionString());
            await cn.OpenAsync();

            int estatus = 0;
            long productoId = 0; // ← cupones.productoID es BIGINT

            // 1) Leer cupón
            await using (var cmd = new SqlCommand(
                "SELECT estatus, productoID FROM cupones WHERE codigo = @Codigo", cn))
            {
                cmd.Parameters.Add("@Codigo", SqlDbType.NVarChar, 50).Value = request.codigoPromocion;

                await using var rd = await cmd.ExecuteReaderAsync(CommandBehavior.SingleRow);
                if (!await rd.ReadAsync())
                {
                    r.mensaje = "El cupón no existe o el formato es incorrecto";
                    return Ok(r);
                }

                estatus = Convert.ToInt32(rd.GetValue(0));
                productoId = Convert.ToInt64(rd.GetValue(1)); // ← evita el Int64→Int32 crash
            }

            if (estatus == EstatusCupon.Cerrado)
            {
                r.mensaje = "Este cupón ya fue utilizado";
                return Ok(r);
            }

            // 2) Traer producto (param BIGINT; funciona aunque p.ID sea INT)
            const string Q_PRODUCTO = @"
        SELECT p.ID, p.EmpresaID, e.NombreComercial, ISNULL(e.logotipoBase64,''),
               p.Nombre, p.Descripcion, p.FechaCaducidad
        FROM Producto p
        LEFT JOIN Empresa e ON e.ID = p.EmpresaID
        WHERE p.ID = @ProductoID";

            await using (var cmd2 = new SqlCommand(Q_PRODUCTO, cn))
            {
                cmd2.Parameters.Add("@ProductoID", SqlDbType.BigInt).Value = productoId;

                await using var rd2 = await cmd2.ExecuteReaderAsync(CommandBehavior.SingleRow);
                if (!await rd2.ReadAsync())
                {
                    r.mensaje = "Producto asociado no encontrado";
                    return Ok(r);
                }

                // Si tus DTOs usan int, convierte de forma segura (sirve para INT o BIGINT)
                int prodId = rd2.GetValue(0) is long l0 ? checked((int)l0) : Convert.ToInt32(rd2.GetValue(0));
                int empId = rd2.GetValue(1) is long l1 ? checked((int)l1) : Convert.ToInt32(rd2.GetValue(1));

                var promo = new PromocionDTO
                {
                    iD = prodId,
                    empresaID = empId,
                    empresaNombre = rd2.GetString(2),
                    logotipoBase64 = rd2.GetString(3),
                    nombre = rd2.GetString(4),
                    descripcion = rd2.GetString(5),
                    fechaCaducidad = rd2.GetDateTime(6),
                    vigencia = FechaHora(rd2.GetDateTime(6))
                };

                r.estatus = 1;
                r.mensaje = "Código creado y disponible";
                r.promocion = promo;
                return Ok(r);
            }
        }




        [HttpPost("PostHacerPromocionValida")]
        [AllowAnonymous]
        public async Task<IActionResult> PostHacerPromocionValida([FromBody] ValidarPromocionQrRequest request)
        {
            var resp = new RespuestaEstatusMensaje { estatus = -1, mensaje = "No se pudo cerrar el código" };

            await using var cn = new SqlConnection(DataAccess.ConnectionString());
            await cn.OpenAsync();
            await using var tx = cn.BeginTransaction();

            // 1) Cerrar cupón (sirve si estaba 1 o 2)
            var upd = @"
        UPDATE cupones
           SET estatus = 3,
               usuarioActivaId = @UsuarioID,
               fechaHoraActivacion = GETDATE()
         WHERE codigo = @Codigo AND estatus <> 3;
        SELECT CAST(@@ROWCOUNT AS int);";

            int affected;
            await using (var cmd = new SqlCommand(upd, cn, tx))
            {
                cmd.Parameters.Add("@UsuarioID", SqlDbType.Int).Value = request.UsuarioID;
                cmd.Parameters.Add("@Codigo", SqlDbType.NVarChar, 50).Value = request.codigoPromocion;
                affected = (int)await cmd.ExecuteScalarAsync();
            }
            if (affected == 0)
            {
                await tx.RollbackAsync();
                resp.estatus = 0;
                resp.mensaje = "Código inexistente o ya estaba cerrado.";
                return Ok(resp);
            }

            // 2) Traer referido
            long referidoId = 0;
            await using (var cmd = new SqlCommand("SELECT referidoID FROM cupones WHERE codigo=@Codigo", cn, tx))
            {
                cmd.Parameters.Add("@Codigo", SqlDbType.NVarChar, 50).Value = request.codigoPromocion;
                var obj = await cmd.ExecuteScalarAsync();
                if (obj != null && obj != DBNull.Value) referidoId = Convert.ToInt64(obj);
            }

            // 3) (CLAVE) Actualizar estatus del referido a CERRADO = 3
            if (referidoId > 0)
            {
                await using (var cmd = new SqlCommand(
                    "UPDATE Referido SET estatusReferenciaID = 3 WHERE ID = @RefID", cn, tx))
                {
                    cmd.Parameters.Add("@RefID", SqlDbType.BigInt).Value = referidoId;
                    await cmd.ExecuteNonQueryAsync();
                }

                // Opcional: insertar seguimiento automático
                // await using (var cmd = new SqlCommand(
                //     "INSERT INTO SeguimientoReferido(referidoID, comentario, fechaSeguimiento, usuarioID) VALUES(@RefID, N'Código validado por QR', GETDATE(), @UsuarioID)", cn, tx))
                // {
                //     cmd.Parameters.Add("@RefID", SqlDbType.BigInt).Value = referidoId;
                //     cmd.Parameters.Add("@UsuarioID", SqlDbType.Int).Value = request.UsuarioID;
                //     await cmd.ExecuteNonQueryAsync();
                // }
            }

            await tx.CommitAsync();
            resp.estatus = 1;
            resp.mensaje = "Código cerrado";
            return Ok(resp);
        }


        [HttpPost("CrearNuevaPromocion")]
        [AllowAnonymous]
        public async Task<IActionResult> CrearNuevaPromocion([FromBody] SolicitudCrearPromocion request)
        {

            RespuestaEstatusPromocion response = new RespuestaEstatusPromocion();
            response.estatus = -1;
            response.mensaje = "Solicitud no procesada";

            DataTable dtEmpresaUsuario = DataAccess.performQuery($"select * from UsuarioEmpresa where usuarioId = '{request.usuarioID}'");
            if (dtEmpresaUsuario.Rows.Count > 0)
            {
                int empresaID = Int32.Parse(dtEmpresaUsuario.Rows[0]["EmpresaID"].ToString());
                //Por el momento tipo de comision esta fijo
                int tipoComision = 0;
                //Valida si va a existir un límite de comisiones que estés dispuesto a ofrecer
                int limite = 0;

                var vigencia = DateTime.Today.AddMonths(12);

                string q_insertProducto = $@"insert into Producto 
(EmpresaID, UsuarioID, nombre, descripcion, tipoComision, comisionCantidad, comisionPorcentaje, comisionPorcentajeCantidad, Precio, FechaCaducidad, FechaCreacion, Eliminado) 

output inserted.id  values ('{empresaID}', '{request.usuarioID}', '{request.nombre}', '{request.descripcion}', '0', {request.comisionNivel1}, 0, 0, 0, '{vigencia.ToString("yyyy-MM-dd")}', getdate(), 0)";

                DataTable dtProductoInsertado = DataAccess.performQuery(q_insertProducto);
                if (dtProductoInsertado.Rows.Count > 0)
                {
                    int id = Int32.Parse(dtProductoInsertado.Rows[0]["id"].ToString());
                    response.estatus = 1;
                    response.mensaje = "Producto registrado correctamente";

                    string qInsertComisiones = $@"insert into ProductoComision (ProductoID, tipoComision, nivel_1, nivel_2, nivel_3, nivel_4, nivel_master, nivel_base) 
values ({id}, 0, {request.comisionNivel1}, {request.comisionNivel2}, {request.comisionNivel3}, {request.comisionNivel4}, {request.comisionNivelMaster}, 0) ";

                    DataAccess.performQuery(qInsertComisiones);


                }


            }
            else
            {
                response.mensaje = "No cuentas con permisos para agregar productos";
            }


            return Ok(response);
        }

        [HttpPost("GenerarCodigoPromocion")]
        [AllowAnonymous]
        public async Task<IActionResult> GenerarCodigoPromocion([FromBody] SolicitudCodigoQrRequest request)
        {
            try
            {
                if (request == null) return BadRequest("Solicitud vacía.");
                if (string.IsNullOrWhiteSpace(request.nombres)) return BadRequest("El nombre es requerido.");
                if (request.embajadorID <= 0) return BadRequest("embajadorID inválido.");
                if (request.ProductoID <= 0) return BadRequest("ProductoID inválido.");

                string info = request.InformacionContacto?.Trim();
                string celular = (info?.Contains("@") == true) ? "" : info ?? "";
                string correo = (info?.Contains("@") == true) ? info ?? "" : "";

                using var cn = new SqlConnection(DataAccess.ConnectionString());
                await cn.OpenAsync();
                using var tx = cn.BeginTransaction();

                // 1) Referido
                var insertReferidoSql = @"
INSERT INTO Referido
  (NombreCompleto, Email, Celular, usuarioID, ProductoID, fechaCreacion, estatusReferenciaID, eliminado)
OUTPUT inserted.ID
VALUES
  (@Nombre, @Email, @Celular, @UsuarioID, @ProductoID, GETDATE(), 1, 0);";

                int referidoId;
                using (var cmd = new SqlCommand(insertReferidoSql, cn, tx))
                {
                    cmd.Parameters.AddWithValue("@Nombre", request.nombres);
                    cmd.Parameters.AddWithValue("@Email", string.IsNullOrEmpty(correo) ? (object)DBNull.Value : correo);
                    cmd.Parameters.AddWithValue("@Celular", string.IsNullOrEmpty(celular) ? (object)DBNull.Value : celular);
                    cmd.Parameters.AddWithValue("@UsuarioID", request.embajadorID);
                    cmd.Parameters.AddWithValue("@ProductoID", request.ProductoID);
                    var obj = await cmd.ExecuteScalarAsync();
                    if (obj == null) throw new Exception("No se obtuvo el ID del referido.");
                    referidoId = Convert.ToInt32(obj);
                }

                // 2) Cupón
                var codigo = Guid.NewGuid().ToString("D");
                var insertCuponSql = @"
INSERT INTO cupones
  (codigo, productoID, embajadorId, estatus, vigencia, referidoId, fechaCreacion)
VALUES
  (@Codigo, @ProductoID, @EmbajadorID, 1, @Vigencia, @ReferidoID, GETDATE());";

                using (var cmd = new SqlCommand(insertCuponSql, cn, tx))
                {
                    cmd.Parameters.AddWithValue("@Codigo", codigo);
                    cmd.Parameters.AddWithValue("@ProductoID", request.ProductoID);
                    cmd.Parameters.AddWithValue("@EmbajadorID", request.embajadorID);
                    cmd.Parameters.AddWithValue("@Vigencia", new DateTime(2099, 1, 1));
                    cmd.Parameters.AddWithValue("@ReferidoID", referidoId);
                    await cmd.ExecuteNonQueryAsync();
                }

                tx.Commit();

                // 3) Generar QR (solo backend)
                var urlCodigo = $"{LandingBaseUrl}/val/{codigo}";

                Directory.CreateDirectory(QrFolder);
                var path = Path.Combine(QrFolder, $"{codigo}.png");

                var qrGen = new QRCodeGenerator();
                var qrData = qrGen.CreateQrCode(urlCodigo, QRCodeGenerator.ECCLevel.Q);
                var png = new PngByteQRCode(qrData);
                var bytes = png.GetGraphic(20);

                try
                {
                    using var fs = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Read, 4096, FileOptions.WriteThrough);
                    await fs.WriteAsync(bytes, 0, bytes.Length);
                    await fs.FlushAsync();
                }
                catch (Exception ex)
                {
                    _log.LogError(ex, "Error escribiendo QR en {Path}", path);
                }

                var base64 = Convert.ToBase64String(bytes);

                // 4) Respuesta mínima (sin WhatsApp, sin BuildPublicUrlForQr)
                return Ok(new SolicitudCodigoQrResponse
                {
                    qr64 = base64
                });
            }
            catch (SqlException ex) { return StatusCode(500, $"SQL error: {ex.Message}"); }
            catch (Exception ex) { return StatusCode(500, $"Error: {ex.Message}"); }
        }

        // ========== NUEVO: obtener URL de QR por Referido ==========
        [HttpGet("GetQrUrlByReferido")]
        [AllowAnonymous]
        public async Task<IActionResult> GetQrUrlByReferido(long referidoId)
        {
            if (referidoId <= 0) return BadRequest(new { mensaje = "referidoId inválido" });

            await using var cn = new SqlConnection(DataAccess.ConnectionString());
            await cn.OpenAsync();

            string? codigo = null;
            const string Q = @"
        SELECT TOP 1 codigo
        FROM cupones
        WHERE referidoID = @RefID
        ORDER BY fechaCreacion DESC;";

            await using (var cmd = new SqlCommand(Q, cn))
            {
                cmd.Parameters.Add("@RefID", SqlDbType.BigInt).Value = referidoId;
                var obj = await cmd.ExecuteScalarAsync();
                if (obj != null && obj != DBNull.Value)
                    codigo = Convert.ToString(obj);
            }

            if (string.IsNullOrWhiteSpace(codigo))
                return NotFound(new { mensaje = "Este referido no tiene cupón/QR aún." });

            // Reutiliza tu helper existente
            var url = BuildPublicUrlForQr(codigo);
            return Ok(new { codigo, url });
        }

        // ========== NUEVO: devolver el PNG del QR por Referido ==========
        [HttpGet("QrByReferido/{referidoId:long}")]
        [AllowAnonymous]
        public async Task<IActionResult> QrByReferido(long referidoId)
        {
            if (referidoId <= 0) return BadRequest();

            await using var cn = new SqlConnection(DataAccess.ConnectionString());
            await cn.OpenAsync();

            string? codigo = null;
            const string Q = @"
        SELECT TOP 1 codigo
        FROM cupones
        WHERE referidoID = @RefID
        ORDER BY fechaCreacion DESC;";
            await using (var cmd = new SqlCommand(Q, cn))
            {
                cmd.Parameters.Add("@RefID", SqlDbType.BigInt).Value = referidoId;
                var obj = await cmd.ExecuteScalarAsync();
                if (obj != null && obj != DBNull.Value)
                    codigo = Convert.ToString(obj);
            }

            if (string.IsNullOrWhiteSpace(codigo))
                return NotFound();

            // 1) Si ya existe el archivo en disco, devuélvelo
            var path = Path.Combine(QrFolder, $"{codigo}.png");
            if (System.IO.File.Exists(path))
                return File(System.IO.File.ReadAllBytes(path), "image/png");

            // 2) Si no existe, genera “al vuelo” usando tu lógica actual
            var urlCodigo = $"{LandingBaseUrl}/val/{codigo}";
            var qrGen = new QRCodeGenerator();
            var qrData = qrGen.CreateQrCode(urlCodigo, QRCodeGenerator.ECCLevel.Q);
            var png = new PngByteQRCode(qrData);
            var bytes = png.GetGraphic(20);

            try
            {
                Directory.CreateDirectory(QrFolder);
                using var fs = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Read, 4096, FileOptions.WriteThrough);
                await fs.WriteAsync(bytes, 0, bytes.Length);
                await fs.FlushAsync();
            }
            catch (Exception ex)
            {
                _log.LogWarning(ex, "No se pudo persistir el QR en {Path}", path);
            }

            return File(bytes, "image/png");
        }

        [HttpGet("/val/{codigo}")]
        [AllowAnonymous]
        public ContentResult ValLandingSoloApp(string codigo)
        {
            // Logo servido desde wwwroot/public/brand
            var logoUrl = $"{LandingBaseUrl}/public/qr/embassy-logo.png?v=1";

            var html = $@"<!doctype html>
                <html lang=""es"">
                <head>
                  <meta charset=""utf-8"">
                  <meta name=""viewport"" content=""width=device-width, initial-scale=1"">
                  <title>Validación de cupón — Embassy</title>
                  <style>
                    :root {{
                      --bg:#0b1220; --card:#121a2b; --bd:#22314f; --txt:#ffffff;
                      --muted: rgba(255,255,255,.7);
                    }}
                    * {{ box-sizing:border-box; }}
                    body {{
                      margin:0; font-family: system-ui, -apple-system, Segoe UI, Roboto, Arial, sans-serif;
                      background:var(--bg); color:var(--txt);
                    }}
                    .wrap {{ max-width: 560px; margin: 0 auto; padding: 28px 16px; }}
                    .card {{
                      background:var(--card); border:1px solid var(--bd); border-radius:16px; padding:24px;
                      text-align:center;
                    }}
                    .logo {{
                      width:72px; height:72px; border-radius:14px; margin:0 auto 12px; display:block; background:#fff;
                      object-fit:contain;
                    }}
                    h1 {{ font-size:22px; margin:10px 0 6px; }}
                    p  {{ margin:8px 0; line-height:1.45; }}
                    .muted {{ color:var(--muted); font-size:13px; }}
                    .code {{ font-family: ui-monospace, SFMono-Regular, Menlo, monospace; background:#0a1020; padding:2px 6px; border-radius:6px; }}
                    .btns {{ display:flex; gap:12px; justify-content:center; flex-wrap:wrap; margin-top:14px; }}
                    .btn {{
                      display:inline-block; padding:12px 16px; border-radius:12px; border:1px solid #2d3e6b;
                      text-decoration:none; color:#fff; min-width:180px;
                    }}
                    .primary {{ border-color:#3a63ff; }}
                    .hint-os {{ margin-top:8px; }}
                  </style>
                  <meta name=""apple-itunes-app"" content=""app-id=6747898144"">
                  <script>
                    document.addEventListener('DOMContentLoaded', function(){{
                      var ua = navigator.userAgent || '';
                      var isAndroid = /Android/i.test(ua);
                      var isIOS = /iPhone|iPad|iPod/i.test(ua);
                      var a = document.getElementById('btn-play');
                      var i = document.getElementById('btn-appstore');
                      if(isAndroid) a.classList.add('primary');
                      if(isIOS) i.classList.add('primary');
                    }});
                  </script>
                </head>
                <body>
                  <div class=""wrap"">
                    <div class=""card"">
                      <img class=""logo"" src=""{logoUrl}"" alt=""Embassy"" width=""72"" height=""72"" loading=""lazy"">
                      <h1>Escanéalo desde la app de Embassy</h1>
                      <p>Este cupón es de <b>uso exclusivo para empresas</b> y <b>se valida dentro de la app</b>.</p>
                      <p class=""muted"">Código: <span class=""code"">{codigo}</span></p>

                      <div class=""btns"">
                        <a id=""btn-play"" class=""btn"" href=""https://play.google.com/store/apps/details?id=com.embassybusiness.app"" rel=""noopener"">Abrir en Google Play</a>
                        <a id=""btn-appstore"" class=""btn"" href=""https://apps.apple.com/mx/app/embassy/id6747898144"" rel=""noopener"">Abrir en App Store</a>
                      </div>

                      <div class=""hint-os muted"">
                        Instala la app y valida el cupón desde el módulo correspondiente.
                      </div>
                    </div>

                    <p class=""muted"" style=""text-align:center; margin-top:12px;"">
                      Si ya estás dentro de la app, vuelve atrás e intenta nuevamente.
                    </p>
                  </div>
                </body>
                </html>";

            Response.Headers["Cache-Control"] = "no-store";
            return Content(html, "text/html; charset=utf-8");
        }



        public class PromocionDTO
        {
            public long iD { get; set; }        // ← long
            public int empresaID { get; set; }  // deja int si Empresa.ID es int
            public string empresaNombre { get; set; }
            public string nombre { get; set; }
            public string descripcion { get; set; }
            public string logotipoBase64 { get; set; }
            public int TipoComision { get; set; }
            public decimal comisionCantidad { get; set; }
            public decimal comisionPorcentaje { get; set; }
            public string comision { get; set; }
            public DateTime fechaCaducidad { get; set; }
            public string vigencia { get; set; }
        }

        public class SolicitudCodigoQrResponse
        {
            public string qr64 { get; set; }
            public bool whatsappEnviado { get; set; }
            public string? whatsappSid { get; set; }
            public string? whatsappError { get; set; }   // <- nuevo (solo para debug)
        }


        public class SolicitudCodigoQrRequest
        {

            public int ProductoID { get; set; }
            public int embajadorID { get; set; }
            public string nombres { get; set; }
            public string InformacionContacto { get; set; }

        }


        public class ValidarPromocionQrRequest
        {

            public int UsuarioID { get; set; }
            public string codigoPromocion { get; set; }
        }

        public class RespuestaEstatusPromocion
        {
            public int estatus { get; set; }
            public string mensaje { get; set; }
            public PromocionDTO promocion { get; set; }
        }


        public class ResumenEmbajador
        {
            public decimal ingresosDirectos { get; set; }
            public decimal ingresosIndirectos { get; set; }
            public DateTime proximaFechaPago { get; set; }
            public List<ResumenEmbajadorInvitacion> embajadoresInvitados { get; set; }
        }

        public class ResumenEmbajadorInvitacion
        {
            public string nombre { get; set; }
            public DateTime fechaInvitacion { get; set; }
            public string estatus { get; set; }
        }

        public class SolicitudCrearPromocion
        {
            public int usuarioID { get; set; }
            public string nombre { get; set; }
            public string descripcion { get; set; }
            public string VigenciaISO { get; set; }

            public decimal comisionNivel1 { get; set; }
            public decimal comisionNivel2 { get; set; }
            public decimal comisionNivel3 { get; set; }
            public decimal comisionNivel4 { get; set; }
            public decimal comisionNivelMaster { get; set; }

        }
        public static class EstatusCupon
        {
            public const int Creado = 1;
            public const int Seguimiento = 2; // no se usa en QR
            public const int Cerrado = 3;
        }

    }
}
