using EBDTOs;
using EBEntities;
using EBServices;
using EBServices.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace EmbassyBusinessBack.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FuenteOrigenController : ControllerBase
    {

        private readonly IFuenteOrigenService _fuenteOrigenService;

        public FuenteOrigenController(IFuenteOrigenService fuenteOrigenService)
        {
            _fuenteOrigenService = fuenteOrigenService;
        }

        [HttpGet("GetFuentesOrigen")]
        [SwaggerResponse(200, "Objeto de respuesta", typeof(List<FuenteOrigenDTO>))]
        public async Task<IActionResult> GetCatalogoPaises()
        {

            var result = await _fuenteOrigenService.GetFuentesDeOrigen();

            if (_fuenteOrigenService.HasError == false)
            {
                if (result == null || result.Count == 0)
                {
                    return NotFound(GenericResponseDTO<string>.Fail("No se encontraron registros en catalogo de fuente origen."));
                }
                return Ok(GenericResponseDTO<List<FuenteOrigenDTO>>.Ok(result, "Consulta exitosa"));
            }
            else
            {
                return BadRequest(GenericResponseDTO<string>.Fail(_fuenteOrigenService.LastError));
            }
        }


    }
}
