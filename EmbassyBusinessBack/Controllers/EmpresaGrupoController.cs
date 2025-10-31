using EBDTOs;
using EBEntities;
using EBServices;
using EBServices.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace EmbassyBusinessBack.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EmpresaGrupoController : ControllerBase
    {
        private readonly IEmpresaGrupoService _empresaGrupoService;

        public EmpresaGrupoController(IEmpresaGrupoService empresaGrupoService)
        {
            _empresaGrupoService = empresaGrupoService;
        }

        [HttpGet("GetAllGruposByEmpresa")]
        [SwaggerResponse(200, "Objeto de respuesta", typeof(List<EmpresaGrupoDTO>))]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAllGruposByEmpresa([FromQuery] long id)
        {

            var result = await _empresaGrupoService.GetAllGruposByEmpresa(id);

            if (_empresaGrupoService.HasError == false)
            {
                if (result == null || result.Count == 0)
                {
                    return NotFound(GenericResponseDTO<string>.Fail("No se encontraron registros en catalogo de grupos/empresa.",false));
                }
                return Ok(GenericResponseDTO<List<EmpresaGrupoDTO>>.Ok(result, "Consulta exitosa"));
            }
            else
            {
                return BadRequest(GenericResponseDTO<string>.Fail(_empresaGrupoService.LastError));
            }
        }

        [HttpPost("Save")]
        [SwaggerResponse(200, "Objeto de respuesta", typeof(bool))]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Save([FromBody] EmpresaGrupo model)
        {

            var result = await _empresaGrupoService.Save(model);

            if (_empresaGrupoService.HasError == false)
            {
                return Ok(GenericResponseDTO<bool>.Ok(result, "Guardado"));
            }
            else
            {
                return BadRequest(GenericResponseDTO<string>.Fail(_empresaGrupoService.LastError));
            }
        }

        [HttpPost("Delete")]
        [SwaggerResponse(200, "Objeto de respuesta", typeof(bool))]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete([FromBody] EmpresaGrupo model)
        {

            var result = await _empresaGrupoService.Save(model);

            if (_empresaGrupoService.HasError == false)
            {
                return Ok(GenericResponseDTO<bool>.Ok(result, "Guardado"));
            }
            else
            {
                return BadRequest(GenericResponseDTO<string>.Fail(_empresaGrupoService.LastError));
            }
        }


    }
}
