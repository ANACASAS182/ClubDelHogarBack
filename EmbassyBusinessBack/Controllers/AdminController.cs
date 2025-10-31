using EBDTOs;
using EmbassyBusinessBack.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EmbassyBusinessBack.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AdminController : ControllerBase
    {
        /*public IActionResult Index()
        {
            return View();
        }*/

        //---------------------------------


        [HttpGet("GetGrupos")]
        [AllowAnonymous]
        public IActionResult GetGrupos()
        {

            List<GrupoDTO> empresas = DataAccess.fromQueryListOf<GrupoDTO>($"select * from grupo");

            return Ok(empresas);
        }

        [HttpPost("AgregarEditarGrupo")]
        [AllowAnonymous]
        public IActionResult AgregarEditarGrupo([FromBody] GrupoDTO grupo)
        {

            if (grupo.id > 0)
            {
                DataAccess.performQuery($"update grupo set nombre = '{grupo.nombre}'  where id = '{grupo.id}'");
                
            }
            else {
                int id = DataAccess.fromQueryObject<int>($"insert into grupo (nombre) output inserted.id values ('{grupo.nombre}')");
                grupo.id = id;
            }

            return Ok(grupo);

        }

        //---------------------------------

        [HttpGet("GetEmpresas")]
        [AllowAnonymous]
        public IActionResult GetEmpresas()
        {
            string q = @"select empresa.ID, empresa.rfc, empresa.RazonSocial, empresa.NombreComercial, EmpresaGrupo.GrupoID from empresa
left join EmpresaGrupo on EmpresaGrupo.empresaID = empresa.ID";

            List<EmpresaDTO> empresas = DataAccess.fromQueryListOf<EmpresaDTO>(q);
           
            return Ok(empresas);
        }

        

        [HttpGet("GetEmbajadores")]
        [AllowAnonymous]
        public IActionResult GetEmbajadores()
        {

            List<EmbajadorDTO> embajadores = DataAccess.fromQueryListOf<EmbajadorDTO>($"select * from usuario");

            return Ok(embajadores);
        }
    }
}
