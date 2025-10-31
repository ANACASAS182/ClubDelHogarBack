using EBDTOs;
using EBEntities;
using EBServices;
using EBServices.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using System.Security.Claims;

namespace EmbassyBusinessBack.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class GrupoController : ControllerBase
    {
        private readonly IGrupoService _grupoService;

        public GrupoController(IGrupoService grupoService)
        {
            _grupoService = grupoService;
        }

        [HttpGet("GetAllGrupos")]
        [SwaggerResponse(200, "Objeto de respuesta", typeof(List<GrupoDTO>))]
        //[Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAllGrupos()
        {

            var result = await _grupoService.GetAllGrupos();

            if (_grupoService.HasError == false)
            {
                if (result == null)
                {
                    return NotFound(GenericResponseDTO<string>.Fail("No se encontraron registros en catalogo de grupos.", false));
                }
                return Ok(GenericResponseDTO<List<GrupoDTO>>.Ok(result, "Consulta exitosa"));
            }
            else
            {
                return BadRequest(GenericResponseDTO<string>.Fail(_grupoService.LastError));
            }
        }

        [HttpGet("GetGruposPaginated")]
        [SwaggerResponse(200, "Objeto de respuesta", typeof(PaginationModelDTO<List<GrupoDTO>>))]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetGruposPaginated([FromQuery] int page, [FromQuery] int size, [FromQuery] string sortBy = "", [FromQuery] string sortDir = "", [FromQuery] string searchQuery = "")
        {

            var result = await _grupoService.GetGruposPaginated(page, size, sortBy, sortDir, searchQuery);

            if (_grupoService.HasError == false)
            {
                if (result.Items == null || result.Total == 0)
                {
                    return NotFound(GenericResponseDTO<string>.Fail("No se encontraron registros en catalogo de grupos.", false));
                }
                return Ok(GenericResponseDTO<PaginationModelDTO<List<GrupoDTO>>>.Ok(result, "Consulta exitosa"));
            }
            else
            {
                return BadRequest(GenericResponseDTO<string>.Fail(_grupoService.LastError));
            }
        }

        [HttpGet("GetByID")]
        [SwaggerResponse(200, "Objeto de respuesta", typeof(GrupoDTO))]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetByID([FromQuery] long id)
        {
            var result = await _grupoService.GetByID(id);

            if (_grupoService.HasError == false)
            {
                if (result == null)
                {
                    return NotFound(GenericResponseDTO<string>.Fail("No se encontro grupo."));
                }
                return Ok(GenericResponseDTO<GrupoDTO>.Ok(result, "Consulta exitosa"));
            }
            else
            {
                return BadRequest(GenericResponseDTO<string>.Fail(_grupoService.LastError));
            }
        }


        [HttpPost("Save")]
        [SwaggerResponse(200, "Objeto de respuesta", typeof(bool))]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Save([FromBody] GrupoDTO dto)
        {

            Grupo model = new Grupo()
            {
                ID = dto.id ?? 0,
                Nombre = dto.nombre
            };

            var result = await _grupoService.Save(model);

            if (_grupoService.HasError == false)
            {
                return Ok(GenericResponseDTO<bool>.Ok(result, "Guardado"));
            }
            else
            {
                return BadRequest(GenericResponseDTO<string>.Fail(_grupoService.LastError));
            }
        }

    }
}