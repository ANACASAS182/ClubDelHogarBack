using EBDTOs;
using EBEntities;
using EBServices;
using EBServices.Interfaces;
using EmbassyBusinessBack.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using System.Security;

namespace EmbassyBusinessBack.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ProductoController : ControllerBase
    {
        private readonly IProductoService _productoService;

        public ProductoController(IProductoService productoService)
        {
            _productoService = productoService;
        }



        [HttpGet("getProductoById")]
        [SwaggerResponse(200, "Objeto de respuesta", typeof(PaginationModelDTO<List<ProductoCatalogoDTO>>))]
        public async Task<IActionResult> getProductoById([FromQuery] int productoId)
        {

            string q = $@"select producto.id, producto.nombre, producto.Descripcion, Producto.FechaCaducidad as vigencia, Producto.FechaCreacion as creacion,
Producto.tipoComision,
ProductoComision.nivel_1 as nivel1,
ProductoComision.nivel_2 as nivel2,
ProductoComision.nivel_3 as nivel3,
ProductoComision.nivel_4 as nivel4,
ProductoComision.nivel_base as nivelInvitacion,
ProductoComision.nivel_master as nivelMaster
from producto left join ProductoComision on ProductoComision.ProductoID= Producto.ID  where Producto.id = {productoId}";

            ProductoDisplay p = DataAccess.fromQueryObject<ProductoDisplay>(q);
            p.vigenciaLetra = FechaHora(p.vigencia);
            p.creacionLetra= FechaHora(p.creacion);

            p.totalComision = p.nivel1 + p.nivel2 + p.nivel3 + p.nivel4 + p.nivelInvitacion + p.nivelMaster;

            return Ok(p);
        }
        private string FechaHora(DateTime dt)
        {
            string fechaHoraLetra = dt.ToString("dd 'de' MMMM 'de' yyyy",
                new System.Globalization.CultureInfo("es-ES"));
            return fechaHoraLetra;
        }



        [HttpGet("GetAllProductoPaginated")]
        [SwaggerResponse(200, "Objeto de respuesta", typeof(PaginationModelDTO<List<ProductoCatalogoDTO>>))]
        public async Task<IActionResult> GetAllProductoPaginated([FromQuery] int page, [FromQuery] int size, [FromQuery] string sortBy = "", [FromQuery] string sortDir = "", [FromQuery] string searchQuery = "", long? grupoID = null, long? empresaID = null, int vigenciaFilter = 0)
        {

            var result = await _productoService.GetAllProductosPaginated(page, size, sortBy, sortDir, searchQuery, grupoID, empresaID, vigenciaFilter);

            if (_productoService.HasError == false)
            {
                if (result.Items == null || result.Total == 0)
                {
                    return NotFound(GenericResponseDTO<string>.Fail("No se encontraron registros en catalogo de productos.", false));
                }
                return Ok(GenericResponseDTO<PaginationModelDTO<List<ProductoCatalogoDTO>>>.Ok(result, "Consulta exitosa"));
            }
            else
            {
                return BadRequest(GenericResponseDTO<string>.Fail(_productoService.LastError));
            }
        }


        [HttpGet("GetProductoByEmpresaPaginated")]
        public async Task<IActionResult> GetProductoByEmpresaPaginated([FromQuery] long empresaID, [FromQuery] int page, [FromQuery] int size, [FromQuery] string sortBy = "", [FromQuery] string sortDir = "", [FromQuery] string searchQuery = "", int vigenciaFilter = 0)
        {
            var result = await _productoService.GetProductosByEmpresaPaginated(empresaID, page, size, sortBy, sortDir, searchQuery, vigenciaFilter);

            if (_productoService.HasError)
                return BadRequest(GenericResponseDTO<string>.Fail(_productoService.LastError));

            // ✅ SIEMPRE 200, aunque esté vacío
            return Ok(GenericResponseDTO<PaginationModelDTO<List<ProductoCatalogoDTO>>>.Ok(result, "Consulta exitosa"));
        }



        [HttpGet("GetProductoPaginated")]
        [SwaggerResponse(200, "Objeto de respuesta", typeof(PaginationModelDTO<List<Producto>>))]
        public async Task<IActionResult> GetProductoPaginated([FromQuery] long empresaID, [FromQuery] int page, [FromQuery] int size, [FromQuery] string sortBy = "", [FromQuery] string sortDir= "", [FromQuery] string searchQuery = "")
        {

            var result = await _productoService.GetProductosPaginated(empresaID, page, size, sortBy, sortDir, searchQuery);

            if (_productoService.HasError == false)
            {
                if (result.Items == null || result.Total == 0)
                {
                    return NotFound(GenericResponseDTO<string>.Fail("No se encontraron registros en catalogo de productos.", false));
                }
                return Ok(GenericResponseDTO<PaginationModelDTO<List<Producto>>>.Ok(result, "Consulta exitosa"));
            }
            else
            {
                return BadRequest(GenericResponseDTO<string>.Fail(_productoService.LastError));
            }
        }

        [HttpGet("GetProductosEmpresa")]
        [SwaggerResponse(200, "Objeto de respuesta", typeof(List<ProductoCatalogoDTO>))]
        public async Task<IActionResult> GetProductosEmpresa([FromQuery] long empresaID)
        {
            var result = await _productoService.GetProductosByEmpresaCatalogo(empresaID); // <- AQUÍ

            if (_productoService.HasError == false)
            {
                if (result == null || result.Count == 0)
                    return NotFound(GenericResponseDTO<string>.Fail("No se encontraron registros en catalogo de productos.", false));

                return Ok(GenericResponseDTO<List<ProductoCatalogoDTO>>.Ok(result, "Consulta exitosa"));
            }
            else
            {
                return BadRequest(GenericResponseDTO<string>.Fail(_productoService.LastError));
            }
        }



        [HttpPost("Save")]
        [SwaggerResponse(200, "Objeto de respuesta", typeof(bool))]
        public async Task<IActionResult> Save([FromBody] ProductoCreateDTO model)
        {

            var result = await _productoService.Save(model);

            if (_productoService.HasError == false)
            {
                return Ok(GenericResponseDTO<bool>.Ok(result, "Guardado"));
            }
            else
            {
                return BadRequest(GenericResponseDTO<string>.Fail(_productoService.LastError));
            }
        }





    }


    public class ProductoDisplay { 
        public int id { get; set; }

        public string nombre { get; set; }
        public string descripcion { get; set; }
        public DateTime vigencia { get; set; }
        public string vigenciaLetra { get; set; }

        public DateTime creacion { get; set; }
        public string creacionLetra { get; set; }

        public int tipoComision { get; set; }
        public decimal nivel1 { get; set; }
        public decimal nivel2 { get; set; }
        public decimal nivel3 { get; set; }
        public decimal nivel4 { get; set; }
        public decimal nivelInvitacion { get; set; }
        public decimal nivelMaster { get; set; }
        public decimal totalComision { get; set; }

    }
}



