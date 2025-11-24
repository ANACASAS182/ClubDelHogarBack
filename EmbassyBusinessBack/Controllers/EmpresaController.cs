using EBDTOs;
using EBEntities;
using EBServices;
using EBServices.Interfaces;
using EmbassyBusinessBack.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using System.Collections.Generic;
using System.Linq;                 // FirstOrDefault
using System.Security.Claims;      // Claims

namespace EmbassyBusinessBack.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class EmpresaController : ControllerBase
    {
        private readonly IEmpresaService _empresaService;
        private readonly IUsuarioService _usuarioService;

        public EmpresaController(
            IEmpresaService empresaService,
            IUsuarioService usuarioService
        )
        {
            _empresaService = empresaService;
            _usuarioService = usuarioService;
        }

        [HttpGet("GetAllEmpresas")]
        [SwaggerResponse(200, "Objeto de respuesta", typeof(List<Empresa>))]
        public async Task<IActionResult> GetAllEmpresas()
        {
            var result = await _empresaService.GetAllEmpresas();

            if (_empresaService.HasError == false)
            {
                if (result == null || result.Count == 0)
                    return NotFound(GenericResponseDTO<string>.Fail("No se encontraron registros en catalogo de empresas."));

                return Ok(GenericResponseDTO<List<Empresa>>.Ok(result, "Consulta exitosa"));
            }

            return BadRequest(GenericResponseDTO<string>.Fail(_empresaService.LastError));
        }

        [AllowAnonymous]
        [HttpGet("GetAllEmpresasByUsuarioId")]
        [SwaggerResponse(200, "Objeto de respuesta", typeof(List<EmpresaDTO>))]
        public async Task<IActionResult> GetAllEmpresasByUsuarioId([FromQuery] int? usuarioId)
        {
            int uid = usuarioId.GetValueOrDefault(15319);

            string q = $@"
SELECT 
    e.ID                AS id,
    e.RFC               AS rfc,
    e.RazonSocial       AS razonSocial,
    e.NombreComercial   AS nombreComercial,
    e.Grupo             AS GrupoID,
    CASE 
        WHEN ISNULL(e.LogotipoBase64,'') = '' THEN ''
        WHEN e.LogotipoBase64 LIKE 'data:%' THEN e.LogotipoBase64
        ELSE 'data:image/png;base64,' + 
             REPLACE(REPLACE(REPLACE(e.LogotipoBase64, CHAR(13), ''), CHAR(10), ''), ' ', '')
    END AS logotipoBase64
FROM Empresa e
LEFT JOIN Usuario u         ON u.ID = {uid}
LEFT JOIN UsuarioEmpresa ue ON ue.UsuarioID = u.ID AND ue.EmpresaID = e.ID
WHERE (ISNULL(u.GrupoID,0) > 0 AND e.Grupo = u.GrupoID)
   OR (ue.EmpresaID IS NOT NULL);";

            var empresasUsuario = DataAccess.fromQueryListOf<EmpresaDTO>(q);

            return Ok(GenericResponseDTO<List<EmpresaDTO>>.Ok(empresasUsuario, "Consulta exitosa"));
        }


        [HttpGet("GetEmpresasPaginated")]
        [SwaggerResponse(200, "Objeto de respuesta", typeof(PaginationModelDTO<List<EmpresaCatalogoDTO>>))]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetEmpresasPaginated(
            [FromQuery] int page,
            [FromQuery] int size,
            [FromQuery] string sortBy = "",
            [FromQuery] string sortDir = "",
            [FromQuery] string searchQuery = "",
            [FromQuery] long grupoID = 0)
        {
            var result = await _empresaService.GetEmpresasPaginated(page, size, sortBy, sortDir, searchQuery, grupoID);

            if (_empresaService.HasError == false)
            {
                if (result.Items == null || result.Total == 0)
                    return NotFound(GenericResponseDTO<string>.Fail("No se encontraron registros en catalogo de grupos.", false));

                return Ok(GenericResponseDTO<PaginationModelDTO<List<EmpresaCatalogoDTO>>>.Ok(result, "Consulta exitosa"));
            }

            return BadRequest(GenericResponseDTO<string>.Fail(_empresaService.LastError));
        }

        [HttpPost("Save")]
        [SwaggerResponse(200, "Objeto de respuesta", typeof(bool))]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Save([FromBody] EmpresaCreateDTO model)
        {
            var result = await _empresaService.Save(model);

            if (_empresaService.HasError == false)
                return Ok(GenericResponseDTO<bool>.Ok(result, "Guardado"));

            return BadRequest(GenericResponseDTO<string>.Fail(_empresaService.LastError));
        }

        [HttpGet("GetEmpresaByID")]
        [SwaggerResponse(200, "Objeto de respuesta", typeof(Empresa))]
        public async Task<IActionResult> GetEmpresaByID([FromQuery] long empresaID)
        {
            var result = await _empresaService.GetEmpresaByID(empresaID);

            if (result.embajadorId > 0)
            {
                var u = DataAccess.fromQueryObject<UsuarioBasico>(
                    $"select id, Nombres + ' ' + apellidos as nombre, email from Usuario where id = {result.embajadorId}");
                result.embajadorNombre = u.nombre + "(" + u.email + ")";
            }

            if (_empresaService.HasError == false)
            {
                if (result == null)
                    return NotFound(GenericResponseDTO<string>.Fail("No se encontro la empresa."));

                return Ok(GenericResponseDTO<Empresa>.Ok(result, "Consulta exitosa"));
            }

            return BadRequest(GenericResponseDTO<string>.Fail(_empresaService.LastError));
        }
    }
}
