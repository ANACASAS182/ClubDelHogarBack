using EBDTOs;
using EBEntities;
using EBEnums;
using EBServices;
using EBServices.Interfaces;
using EmbassyBusinessBack.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Swashbuckle.AspNetCore.Annotations;
using System.Data;
using System.Security.Claims;
using Dapper;

namespace EmbassyBusinessBack.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ReferidoController : ControllerBase
    {
        private readonly IReferidoService _referidoSercvice;

        public ReferidoController(IReferidoService referidoSercvice)
        {
            _referidoSercvice = referidoSercvice;
        }


        private async Task<ReferidoDTO?> GetReferidoByIDCore(long id)
        {
            using var cn = new SqlConnection(DataAccess.ConnectionString());
            const string sql = @"
            SELECT
                r.ID,
                r.NombreCompleto                          AS NombreCompleto,
                ISNULL(r.Email,'')                        AS Email,
                ISNULL(r.Celular,'')                      AS Celular,
                r.UsuarioID,
                r.ProductoID,
                r.FechaCreacion,
                ISNULL(r.Eliminado,0)                     AS Eliminado,
                r.FechaEliminacion,
                r.EstatusReferenciaID,
                ISNULL(r.EstatusPago,0)                   AS EstatusPago,
                COALESCE(r.FechaEfectiva, r.FechaCreacion) AS FechaEfectiva,
                ISNULL(r.Simulado,0)                      AS Simulado,
                ISNULL(r.PrecioBasePorcentaje,0)          AS PrecioBasePorcentaje,
                p.Nombre                                  AS ProductoNombre,
                e.NombreComercial                         AS EmpresaRazonSocial
            FROM dbo.Referido r
            JOIN dbo.Producto p ON p.ID = r.ProductoID
            JOIN dbo.Empresa  e ON e.ID = p.EmpresaID
            WHERE r.ID = @Id;";

            return await cn.QueryFirstOrDefaultAsync<ReferidoDTO>(sql, new { Id = id });
        }

        [HttpGet("GetReferidoByID")]
        [SwaggerResponse(200, "Objeto de respuesta", typeof(ReferidoDTO))]
        public async Task<IActionResult> GetReferidoByID([FromQuery] long id)
        {
            var dto = await GetReferidoByIDCore(id);
            if (dto == null)
                return NotFound(GenericResponseDTO<string>.Fail("No encontrado", false));

            return Ok(GenericResponseDTO<ReferidoDTO>.Ok(dto, "Consulta exitosa"));
        }

        [HttpGet("GetReferidosUsuario")]
        [SwaggerResponse(200, "Objeto de respuesta", typeof(List<ReferidoDTO>))]
        public async Task<IActionResult> GetReferidosUsuario([FromQuery] long usuarioID)
        {

            var result = await _referidoSercvice.GetReferidosByUsuario(usuarioID);

            if (_referidoSercvice.HasError == false)
            {
                if (result == null || result.Count == 0)
                {
                    return NotFound(GenericResponseDTO<string>.Fail("No se encontraron registros en catalogo de referidos.", false));
                }
                return Ok(GenericResponseDTO<List<ReferidoDTO>>.Ok(result, "Consulta exitosa"));
            }
            else
            {
                return BadRequest(GenericResponseDTO<string>.Fail(_referidoSercvice.LastError));
            }
        }

        [HttpGet("GetReferidosSimple")]
        [AllowAnonymous]
        public IActionResult GetReferidosSimple([FromQuery] long usuarioID)
        {

            string q = $@"select 
Referido.ID,
Referido.NombreCompleto,
Referido.Email,
referido.Celular,
Referido.UsuarioID,
referido.ProductoID,
Producto.EmpresaID,
Producto.TipoComision,
Referido.EstatusReferenciaID,
EstatusReferencia.Nombre as EstatusReferenciaDescripcion,
EstatusReferencia.EnumValue as EstatusReferenciaEnum,
empresa.RazonSocial as EmpresaRazonSocial,
Producto.Nombre as ProductoNombre,
isnull(ProductoComision.Nivel_1, 0) as ComisionCantidad,
isnull(ProductoComision.Nivel_1, 0) as ComisionPorcentaje,
Referido.FechaCreacion as FechaRegistro
from Referido
left join Producto on producto.ID = Referido.ProductoID
left join Empresa on Empresa.ID = Producto.EmpresaID
left join ProductoComision on ProductoComision.ProductoId = Producto.Id
left join EstatusReferencia on EstatusReferencia.ID = Referido.EstatusReferenciaID
where Referido.UsuarioID = {usuarioID} order by Referido.ID desc";

            List<ReferidoDTO> referidos = DataAccess.fromQueryListOf<ReferidoDTO>(q);

            foreach (ReferidoDTO r in referidos)
            {
                if (r.ComisionCantidad > 0)
                {
                    r.Comision = r.ComisionCantidad;
                    r.ComisionTexto = "$" + r.ComisionCantidad.ToString("N2") + "MXN";
                }
                else
                {
                    r.ComisionTexto = r.ComisionCantidad.ToString("N0") + "%";
                }
            }

            return Ok(referidos);
        }

        [HttpGet("GetReferidosUsuarioPaginated")]
        [SwaggerResponse(200, "Objeto de respuesta", typeof(PaginationModelDTO<List<ReferidoDTO>>))]
        public async Task<IActionResult> GetReferidosUsuarioPaginated([FromQuery] int page, [FromQuery] int size, [FromQuery] string sortBy = "", [FromQuery] string sortDir = "", [FromQuery] string searchQuery = "")
        {
            if (!long.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var UsuarioID))
            {
                return BadRequest(GenericResponseDTO<string>.Fail("No se pudo obtener el ID de usuario"));
            }


            var result = await _referidoSercvice.GetReferidosPaginated(UsuarioID, page, size, sortBy, sortDir, searchQuery);

            if (_referidoSercvice.HasError == false)
            {
                if (result.Items == null || result.Total == 0)
                {
                    return NotFound(GenericResponseDTO<string>.Fail("No se encontraron registros en catalogo de referidos.", false));
                }
                return Ok(GenericResponseDTO<PaginationModelDTO<List<ReferidoDTO>>>.Ok(result, "Consulta exitosa"));
            }
            else
            {
                return BadRequest(GenericResponseDTO<string>.Fail(_referidoSercvice.LastError));
            }
        }


        [HttpPost("Save")]
        [SwaggerResponse(200, "Objeto de respuesta", typeof(bool))]
        public async Task<IActionResult> Save([FromBody] ReferidoDTO dto)
        {
            if (!long.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var UsuarioID))
                return BadRequest(GenericResponseDTO<string>.Fail("No se pudo obtener el ID de usuario"));

            var estatusReferencia = await _referidoSercvice
                .GetEstatusReferenciaByEnum(EBEnums.EstatusReferenciaEnum.Creado);

            if (estatusReferencia == null)
                return NotFound(GenericResponseDTO<string>.Fail("No se encontro el estatus de referencia en catalogo."));

            // normaliza opcionales
            var email = string.IsNullOrWhiteSpace(dto.Email) ? "" : dto.Email.Trim();
            var celular = string.IsNullOrWhiteSpace(dto.Celular) ? "" : dto.Celular.Trim();

            var model = new Referido
            {
                Celular = celular,
                Email = email, // <-- vacío si no se envió
                EstatusReferenciaID = estatusReferencia.ID,
                NombreCompleto = dto.NombreCompleto?.Trim(),
                ProductoID = dto.ProductoID,
                UsuarioID = UsuarioID,
            };

            var result = await _referidoSercvice.Save(model);
            return _referidoSercvice.HasError
                ? BadRequest(GenericResponseDTO<string>.Fail(_referidoSercvice.LastError))
                : Ok(GenericResponseDTO<bool>.Ok(result, "Guardado"));

        }

        [HttpPost("UpdateStatus")]
        [SwaggerResponse(200, "Objeto de respuesta", typeof(bool))]
        public async Task<IActionResult> UpdateStatus([FromBody] EstatusReferidoDTO dto)
        {
            if (dto == null || dto.ID <= 0)
                return BadRequest(GenericResponseDTO<string>.Fail("Payload inválido"));

            var nuevoEstatus = (int)dto.EstatusReferenciaEnum; // 1=Creado, 2=Seguimiento, 3=Cerrado

            using var cn = new SqlConnection(DataAccess.ConnectionString());
            await cn.OpenAsync();
            using var tx = cn.BeginTransaction();

            // 1) Actualiza el estatus
            var rows = await cn.ExecuteAsync(
                @"UPDATE dbo.Referido 
            SET EstatusReferenciaID = @Estatus 
          WHERE ID = @Id;",
                new { Estatus = nuevoEstatus, Id = dto.ID }, tx);

            if (rows == 0)
            {
                tx.Rollback();
                return NotFound(GenericResponseDTO<string>.Fail("Referido no encontrado"));
            }

            // 1) lee el estatus actual ANTES del update
            var anterior = await cn.ExecuteScalarAsync<int>(
                "SELECT EstatusReferenciaID FROM dbo.Referido WHERE ID = @Id",
                new { Id = dto.ID }, tx);

            // 2) si no cambió, no inserta nada extra (opcional)
            if (anterior == nuevoEstatus)
            {
                tx.Commit();
                return Ok(GenericResponseDTO<bool>.Ok(true, "Sin cambios"));
            }

            // 3) arma el mensaje del sistema
            string Nom(int s) => s switch
            {
                1 => "Creado",
                2 => "Seguimiento",
                3 => "Cerrado",
                _ => $"Estatus {s}"
            };
            var comentarioSistema = $"Estatus actualizado: {Nom(anterior)} → {Nom(nuevoEstatus)}";

            // si el front mandó un comentario, úsalo; si no, usa el del sistema
            var comentario = string.IsNullOrWhiteSpace(dto.Comentario)
                ? comentarioSistema
                : dto.Comentario.Trim();

            // 4) inserta el seguimiento con texto
            await cn.ExecuteAsync(@"
    INSERT INTO dbo.SeguimientoReferido
        (ReferidoID, FechaSeguimiento, Comentario, FechaCreacion, Eliminado)
    VALUES
        (@Id, GETDATE(), @Comentario, GETDATE(), 0);",
                new { Id = dto.ID, Comentario = comentario }, tx);

            await cn.ExecuteAsync(@"
        INSERT INTO dbo.SeguimientoReferido
            (ReferidoID, FechaSeguimiento, Comentario, FechaCreacion, Eliminado)
        VALUES
            (@Id, GETDATE(), NULLIF(@Comentario,''), GETDATE(), 0);",
                new { Id = dto.ID, Comentario = comentario }, tx);

            tx.Commit();
            return Ok(GenericResponseDTO<bool>.Ok(true, "Guardado"));
        }


        [HttpGet("GetQR")]
        [SwaggerResponse(200, "Objeto de respuesta", typeof(string))]
        public async Task<IActionResult> GetQR([FromQuery] string codigo)
        {

            var result = await _referidoSercvice.LeerQRBase64(codigo);

            if (_referidoSercvice.HasError == false)
            {
                return Ok(GenericResponseDTO<string>.Ok(result!, "Consulta exitosa"));
            }
            else
            {
                return BadRequest(GenericResponseDTO<string>.Fail(_referidoSercvice.LastError));
            }
        }

        [HttpPost("GetUltimosSeguimientos")]
        [AllowAnonymous] 
        public async Task<IActionResult> GetUltimosSeguimientos([FromBody] IdsRequest req)
        {
            try
            {
                if (req?.ids == null || req.ids.Count == 0)
                    return Ok(Array.Empty<UltimoSeguimientoDTO>());

                // limpiamos ids inválidos
                var ids = req.ids.Where(x => x > 0).Distinct().ToArray();
                if (ids.Length == 0)
                    return Ok(Array.Empty<UltimoSeguimientoDTO>());

                var cs = DataAccess.ConnectionString(); // usas esto en tu controller
                using var con = new SqlConnection(cs);
                await con.OpenAsync();

                // Tabla real: dbo.SeguimientoReferido
                // Campos: ID, FechaSeguimiento, Comentario, FechaCreacion, Eliminado, FechaEliminacion, ReferidoID
                var sql = @"
                    WITH ult AS (
                        SELECT
                            s.ReferidoID AS ReferidoId,
                            ISNULL(
                                NULLIF(
                                    LTRIM(RTRIM(CAST(s.Comentario AS nvarchar(max)))),
                                    ''
                                ),
                                N'(sin comentario)'
                            ) AS Texto,
                            COALESCE(s.FechaSeguimiento, s.FechaCreacion) AS Fecha,
                            ROW_NUMBER() OVER (
                                PARTITION BY s.ReferidoID
                                ORDER BY COALESCE(s.FechaSeguimiento, s.FechaCreacion) DESC, s.ID DESC
                            ) AS rn
                        FROM dbo.SeguimientoReferido s
                        WHERE ISNULL(s.Eliminado, 0) = 0
                          AND s.ReferidoID IN @Ids
                    )
                    SELECT ReferidoId, Texto, Fecha
                    FROM ult
                    WHERE rn = 1;";
                var data = await con.QueryAsync<UltimoSeguimientoDTO>(sql, new { Ids = ids });
                return Ok(data);
            }
            catch (Exception ex)
            {
                // loguea para ver el motivo exacto del 500
                return StatusCode(500, GenericResponseDTO<string>.Fail($"Error al obtener últimos seguimientos: {ex.Message}"));
            }
        }

        [HttpGet("GetAllReferidosPaginated")]
        public async Task<IActionResult> GetAllReferidosPaginated(
           [FromQuery] int page,
           [FromQuery] int size,
           [FromQuery] string sortBy = "",
           [FromQuery] string sortDir = "",
           [FromQuery] string searchQuery = "",
           [FromQuery] long empresaID = 0,
           [FromQuery] long usuarioID = 0,
           EstatusReferenciaEnum? estatus = null)
        {
            var buscarSafe = (searchQuery ?? string.Empty).Replace("'", "''");
            var estatusSql = estatus.HasValue ? ((int)estatus.Value).ToString() : "NULL";
            var paginaSql = page < 1 ? 1 : page;          // tu SP es 1-based

            var sql = $@"
            EXEC ObtenerReferidosPaginado
                @Pagina       = {paginaSql},
                @TamanoPagina = {size},
                @EmpresaID    = {empresaID},
                @EmbajadorID  = {usuarioID},
                @Buscar       = N'{buscarSafe}',
                @Estatus      = {estatusSql};";

            var dtReferidos = DataAccess.fromQueryListOf<ReferidoCatalogoDTO>(sql);

            // Ya no hace falta recalcular ProductoVigente aquí, el SP lo manda.
            foreach (var r in dtReferidos)
            {
                r.Email ??= string.Empty;
                r.Celular ??= string.Empty;
                r.Empresa ??= string.Empty;
                r.Embajador ??= string.Empty;
            }

            var p = new PaginationModelDTO<List<ReferidoCatalogoDTO>>
            {
                Total = dtReferidos.Count, // si más adelante quieres el total real, lo agregamos como 2º resultset del SP
                Items = dtReferidos
            };

            return Ok(GenericResponseDTO<PaginationModelDTO<List<ReferidoCatalogoDTO>>>.Ok(p, "Consulta exitosa"));
        }




        [HttpGet("GetReferidosByEmpresaPaginated")]
        [SwaggerResponse(200, "Objeto de respuesta", typeof(PaginationModelDTO<List<ReferidoCatalogoDTO>>))]
        public async Task<IActionResult> GetReferidosByEmpresaPaginated(
             [FromQuery] long empresaID,
             [FromQuery] int page,
             [FromQuery] int size,
             [FromQuery] string sortBy = "",
             [FromQuery] string sortDir = "",
             [FromQuery] string searchQuery = "",
             EstatusReferenciaEnum? estatus = null,
             long? usuarioID = 0)
        {
            var buscarSafe = (searchQuery ?? string.Empty).Replace("'", "''");
            var estatusSql = estatus.HasValue ? ((int)estatus.Value).ToString() : "NULL";
            var paginaSql = page < 1 ? 1 : page;              // SP es 1-based
            var usuarioSql = (usuarioID ?? 0);

            var sql = $@"
            EXEC ObtenerReferidosPaginado
                @Pagina       = {paginaSql},
                @TamanoPagina = {size},
                @EmpresaID    = {empresaID},
                @EmbajadorID  = {usuarioSql},
                @Buscar       = N'{buscarSafe}',
                @Estatus      = {estatusSql};";

            var items = DataAccess.fromQueryListOf<ReferidoCatalogoDTO>(sql);


            foreach (var r in items)
            {
                r.Email ??= string.Empty;
                r.Celular ??= string.Empty;
                r.Empresa ??= string.Empty;
                r.Embajador ??= string.Empty;
      
            }

            var payload = new PaginationModelDTO<List<ReferidoCatalogoDTO>>
            {
                Total = items.Count,  
                Items = items
            };

            return Ok(GenericResponseDTO<PaginationModelDTO<List<ReferidoCatalogoDTO>>>.Ok(payload, "Consulta exitosa"));
        }


        [HttpGet("GetSeguimientoReferido")]
        [SwaggerResponse(200, "Objeto de respuesta", typeof(List<SeguimientoReferido>))]
        public async Task<IActionResult> GetSeguimientoReferido([FromQuery] long referidoID)
        {
            var cs = DataAccess.ConnectionString();
            using var con = new SqlConnection(cs);

            const string sql = @"
        SELECT 
            s.ID,
            s.ReferidoID,
            COALESCE(s.FechaSeguimiento, s.FechaCreacion) AS FechaSeguimiento,
            ISNULL(NULLIF(LTRIM(RTRIM(CAST(s.Comentario AS nvarchar(max)))), N''), N'(sin comentario)') AS Comentario,
            s.FechaCreacion,
            ISNULL(s.UsuarioID, 0) AS UsuarioID,
            ISNULL(s.Eliminado, 0) AS Eliminado,
            s.FechaEliminacion
        FROM dbo.SeguimientoReferido s
        WHERE ISNULL(s.Eliminado, 0) = 0
          AND s.ReferidoID = @Id
        ORDER BY COALESCE(s.FechaSeguimiento, s.FechaCreacion) DESC, s.ID DESC;";

            var rows = (await con.QueryAsync<SeguimientoReferido>(sql, new { Id = referidoID })).ToList();

            // 200 con lista vacía; no disparamos el interceptor de errores
            return Ok(GenericResponseDTO<List<SeguimientoReferido>>.Ok(rows, rows.Count == 0 ? "Sin seguimientos" : "Consulta exitosa"));
        }

    }
}
