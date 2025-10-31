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
    public class CatalogosController : ControllerBase
    {

        private readonly ICatalogosService _catalogosService;

        public CatalogosController(ICatalogosService catalogosService)
        {
            _catalogosService = catalogosService;
        }

        [HttpGet("GetCatalogoPaises")]
        [SwaggerResponse(200, "Objeto de respuesta", typeof(List<CatalogoPais>))]
        public async Task<IActionResult> GetCatalogoPaises()
        {

            var result = await _catalogosService.GetCatalogoPais();

            if (_catalogosService.HasError == false)
            {
                if (result == null || result.Count == 0) {
                    return NotFound(GenericResponseDTO<string>.Fail("No se encontraron registros en catalogo de pais."));
                }
                return Ok(GenericResponseDTO<List<CatalogoPais>>.Ok(result, "Consulta exitosa"));
            }
            else
            {
                return BadRequest(GenericResponseDTO<string>.Fail(_catalogosService.LastError));
            }
        }


        [HttpGet("GetCatalogoEstadosMexicanos")]
        [SwaggerResponse(200, "Objeto de respuesta", typeof(List<CatalogoEstado>))]
        public async Task<IActionResult> GetCatalogoEstadosMexicanos()
        {

            var result = await _catalogosService.GetCatalogoEstadosMexicanos();

            if (_catalogosService.HasError == false)
            {
                if (result == null)
                {
                    return NotFound(GenericResponseDTO<string>.Fail("No se encontraron registros en catalogo de estados."));
                }
                return Ok(GenericResponseDTO<List<CatalogoEstado>>.Ok(result, "Consulta exitosa"));
            }
            else
            {
                return BadRequest(GenericResponseDTO<string>.Fail(_catalogosService.LastError));
            }
        }


        [HttpGet("GetCatalogoBancos")]
        [SwaggerResponse(200, "Objeto de respuesta", typeof(List<CatBancoDto>))]
        public async Task<IActionResult> GetCatalogoBancos()
        {
            var result = await _catalogosService.GetCatalogoBancos();

            if (_catalogosService.HasError == false)
            {
                if (result == null || result.Count == 0)
                    return NotFound(GenericResponseDTO<string>.Fail("No se encontraron bancos activos."));

                return Ok(GenericResponseDTO<List<CatBancoDto>>.Ok(result, "Consulta exitosa"));
            }
            else
            {
                return BadRequest(GenericResponseDTO<string>.Fail(_catalogosService.LastError));
            }
        }
    }
}
