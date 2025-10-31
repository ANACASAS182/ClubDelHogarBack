using EBDTOs;
using EBEntities;
using EBEnums;
using EBServices;
using EBServices.Interfaces;
using EmbassyBusinessBack.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using Dapper;

namespace EmbassyBusinessBack.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class SeguimientoReferidoController : ControllerBase
    {
        private readonly ISeguimientoReferidoService _seguimientoReferidoService;

        public SeguimientoReferidoController(ISeguimientoReferidoService seguimientoReferidoService)
        {
            _seguimientoReferidoService = seguimientoReferidoService;
        }

        [HttpGet("GetSeguimientoReferido")]
        [SwaggerResponse(200, "Objeto de respuesta", typeof(List<SeguimientoReferido>))]
        public async Task<IActionResult> GetSeguimientoReferido([FromQuery] long referidoID) 
        {

            var result = await _seguimientoReferidoService.GetSeguimietosReferido(referidoID);

            if (_seguimientoReferidoService.HasError == false)
            {
                if (result == null || result.Count == 0)
                {
                    return NotFound(GenericResponseDTO<string>.Fail("No se encontraron registros en catalogo de seguimientos.", false));
                }
                return Ok(GenericResponseDTO<List<SeguimientoReferido>>.Ok(result, "Consulta exitosa"));
            }
            else
            {
                return BadRequest(GenericResponseDTO<string>.Fail(_seguimientoReferidoService.LastError));
            }
        }

        [HttpPost("Save")]
        [SwaggerResponse(200, "Objeto de respuesta", typeof(bool))]
        [Authorize(Roles = "Admin,Socio")]
        public async Task<IActionResult> Save([FromBody] SeguimientoReferido model)
        {

            var result = await _seguimientoReferidoService.Save(model);

            if (_seguimientoReferidoService.HasError == false)
            {
                return Ok(GenericResponseDTO<bool>.Ok(result, "Guardado"));
            }
            else
            {
                return BadRequest(GenericResponseDTO<string>.Fail(_seguimientoReferidoService.LastError));
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

            // Normaliza strings por si el mapper no los pone en ""
            foreach (var r in items)
            {
                r.Email ??= string.Empty;
                r.Celular ??= string.Empty;
                r.Empresa ??= string.Empty;
                r.Embajador ??= string.Empty;
                // ProductoVigente ya viene calculado en el SP
            }

            var payload = new PaginationModelDTO<List<ReferidoCatalogoDTO>>
            {
                Total = items.Count,   // si luego quieres total real, agregamos 2º resultset en el SP
                Items = items
            };

            return Ok(GenericResponseDTO<PaginationModelDTO<List<ReferidoCatalogoDTO>>>.Ok(payload, "Consulta exitosa"));
        }

    }
}
