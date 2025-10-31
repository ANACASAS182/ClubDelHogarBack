using EBDTOs;
using EBEntities;
using EBServices.Interfaces;
using EmbassyBusinessBack.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Org.BouncyCastle.Asn1;
using Swashbuckle.AspNetCore.Annotations;
using System.Data;
using System.Diagnostics;

namespace EmbassyBusinessBack.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class PeriodoController : ControllerBase
    {
        private readonly IPeriodoService _periodoService;

        public PeriodoController(IPeriodoService periodoService)
        {
            _periodoService = periodoService;
        }


        [HttpGet("GetPeriodosPaginated")]
        [SwaggerResponse(200, "Objeto de respuesta", typeof(PaginationModelDTO<List<PeriodoDTO>>))]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetGruposPaginated([FromQuery] int page, [FromQuery] int size, [FromQuery] string sortBy = "", [FromQuery] string sortDir = "", [FromQuery] string searchQuery = "")
        {

            var result = await _periodoService.GetPeriodosPaginated(page, size, sortBy, sortDir, searchQuery);

            if (_periodoService.HasError == false)
            {
                if (result.Items == null || result.Total == 0)
                {
                    return NotFound(GenericResponseDTO<string>.Fail("No se encontraron registros en catalogo de periodos.", false));
                }
                return Ok(GenericResponseDTO<PaginationModelDTO<List<PeriodoDTO>>>.Ok(result, "Consulta exitosa"));
            }
            else
            {
                return BadRequest(GenericResponseDTO<string>.Fail(_periodoService.LastError));
            }
        }

        [HttpGet("getPeriodos")]
        public async Task<IActionResult> getPeriodos()
        {
            List<PeriodoDTO> periodos = DataAccess.fromQueryListOf<PeriodoDTO>("select top 12 * from periodo order by fechaFin desc");
            foreach (PeriodoDTO p in periodos)
            {
                p.MesLetra = numeroMes(p.Mes);
            }
            return Ok(periodos);
        }

        [HttpGet("getDetalleEmbajadorMes")]
        public async Task<IActionResult> getDetalleEmbajadorMes(int embajadorId, int periodoId)
        {
            DetalleEmbajadorCorteMensual d = new DetalleEmbajadorCorteMensual();
            d.embajadorId = embajadorId;
            d.periodoId = periodoId;

            d.nombreEmbajador = "n/a";
            d.fechaRegistro = DateTime.Now;
            d.contactoEmbajador = "n/a";
            d.datosBancarios = "n/a";

            DataTable dtDatosEmbajador = DataAccess.performQuery($"select * from Usuario where ID = {embajadorId}");
            if (dtDatosEmbajador.Rows.Count > 0) {
                d.nombreEmbajador = dtDatosEmbajador.Rows[0]["nombres"].ToString() + ' ' + dtDatosEmbajador.Rows[0]["apellidos"].ToString();
                d.fechaRegistro = DateTime.Parse(dtDatosEmbajador.Rows[0]["FechaCreacion"].ToString());
            }


            

            d.referencias = new List<DetalleEmbajadorCorteMensualReferencia>();

            string qReferenciasMes = $@"select 
                Referido.ID,
                Referido.FechaEfectiva,
                Referido.ProductoID,
                Referido.EstatusReferenciaID,
                Referido.nombreCompleto as nombreReferido,
                isnull(Referido.PrecioBasePorcentaje, 0) as PrecioBasePorcentaje,
                isnull(Producto.TipoComision, 0) as TipoComision,
                Producto.Nombre as productoNombre,
                Empresa.ID,
                Empresa.RazonSocial,
                ProductoComision.nivel_1,
                UsuarioNivel1.ID as UsuarioNivel_1,
                ProductoComision.nivel_2,
                isnull(UsuarioNivel2.ID, 0) as UsuarioNivel_2,
                ProductoComision.nivel_3,
                isnull(UsuarioNivel3.ID, 0) as UsuarioNivel_3,
                ProductoComision.nivel_4,
                isnull(UsuarioNivel4.ID, 0) as UsuarioNivel_4,
                ProductoComision.nivel_base,
                isnull(Empresa.embajadorId, 0) as UsuarioInvita,
                ProductoComision.nivel_master
                from 
                Referido
                left join Periodo on Referido.FechaEfectiva between Periodo.FechaInicio and Periodo.FechaFin
                left join Producto on Producto.ID = Referido.ProductoID
                left join Empresa on Empresa.id	 = Producto.EmpresaID
                left join ProductoComision on ProductoComision.ProductoID = Producto.ID
                left join Usuario as UsuarioNivel1 on UsuarioNivel1.ID = Referido.UsuarioID
                left join Usuario as UsuarioNivel2 on UsuarioNivel2.ID = UsuarioNivel1.UsuarioParent
                left join Usuario as UsuarioNivel3 on UsuarioNivel3.ID = UsuarioNivel2.UsuarioParent 
                left join Usuario as UsuarioNivel4 on UsuarioNivel4.ID = UsuarioNivel3.UsuarioParent 
                where Referido.EstatusReferenciaId = 3
                and (
                    UsuarioNivel1.ID = {embajadorId} or 
                    UsuarioNivel2.ID = {embajadorId} or 
                    UsuarioNivel3.ID = {embajadorId} or 
                    UsuarioNivel4.ID = {embajadorId} or
                    Empresa.embajadorId = {embajadorId}
                )
                and empresa.Grupo = 1 and periodo.ID = {periodoId}
                order by Referido.FechaEfectiva desc";

            DataTable dtReferenciasMes = DataAccess.performQuery(qReferenciasMes);
            foreach (DataRow dr in dtReferenciasMes.Rows)
            {

                DetalleEmbajadorCorteMensualReferencia rf = new DetalleEmbajadorCorteMensualReferencia();


                decimal ImporteDirectoSumar = 0;
                decimal ImporteIndirectoSumar = 0;

                int ID = Int32.Parse(dr["ID"].ToString());
                rf.referenciaId = ID;

                rf.fechaEfectiva = DateTime.Parse(dr["FechaEfectiva"].ToString());

                rf.nombreReferido = dr["nombreReferido"].ToString();
                rf.producto = dr["productoNombre"].ToString();

                int EstatusReferenciaId = Int32.Parse(dr["EstatusReferenciaId"].ToString());
                int TipoComision = Int32.Parse(dr["TipoComision"].ToString());
                decimal PrecioBasePorcentaje = decimal.Parse(dr["PrecioBasePorcentaje"].ToString());
                //referencias Directas
                decimal nivel1 = decimal.Parse(dr["nivel_1"].ToString());
                decimal nivel2 = decimal.Parse(dr["nivel_2"].ToString());
                decimal nivel3 = decimal.Parse(dr["nivel_3"].ToString());
                decimal nivel4 = decimal.Parse(dr["nivel_4"].ToString());
                decimal nivel_invitacion = decimal.Parse(dr["nivel_base"].ToString());
                decimal nivel_master = decimal.Parse(dr["nivel_master"].ToString());

                rf.tipoComision = "$";

                string suf = "% de $" + PrecioBasePorcentaje.ToString() + "MXN";

                string nivel1Porcentaje = nivel1.ToString("N2") + suf;
                string nivel2Porcentaje = nivel2.ToString("N2") + suf;
                string nivel3Porcentaje = nivel3.ToString("N2") + suf;
                string nivel4Porcentaje = nivel4.ToString("N2") + suf;
                string nivelInvitacionPorcentaje = nivel_invitacion.ToString("N2") + suf;
                string nivelMasterPorcentaje = nivel_master.ToString("N2") + suf;



                if (TipoComision == 1)
                {
                    rf.tipoComision = "%";
                    

                    nivel1 = (nivel1 / 100) * PrecioBasePorcentaje;
                    nivel2 = (nivel2 / 100) * PrecioBasePorcentaje;
                    nivel3 = (nivel3 / 100) * PrecioBasePorcentaje;
                    nivel4 = (nivel4 / 100) * PrecioBasePorcentaje;
                    nivel_invitacion = (nivel_invitacion / 100) * PrecioBasePorcentaje;
                    nivel_master = (nivel_master / 100) * PrecioBasePorcentaje;
                }

                int usuarionivel1 = Int32.Parse(dr["UsuarioNivel_1"].ToString());
                int usuarionivel2 = Int32.Parse(dr["UsuarioNivel_2"].ToString());
                int usuarionivel3 = Int32.Parse(dr["UsuarioNivel_3"].ToString());
                int usuarionivel4 = Int32.Parse(dr["UsuarioNivel_4"].ToString());
                int usuarioInvita = Int32.Parse(dr["UsuarioInvita"].ToString());


                if (embajadorId == usuarionivel1)
                {
                    rf.nivel = "1";
                    ImporteDirectoSumar = nivel1;
                    rf.detallePorcentaje = nivel1Porcentaje;
                }
                if (embajadorId == usuarionivel2)
                {
                    rf.nivel = "2";
                    ImporteIndirectoSumar = nivel2;
                    rf.detallePorcentaje = nivel2Porcentaje;
                }
                if (embajadorId == usuarionivel3)
                {
                    rf.nivel = "3";
                    ImporteIndirectoSumar = nivel3;
                    rf.detallePorcentaje = nivel3Porcentaje;
                }
                if (embajadorId == usuarionivel4)
                {
                    rf.nivel = "4";
                    ImporteIndirectoSumar = nivel4;
                    rf.detallePorcentaje = nivel4Porcentaje;
                }
                if (embajadorId == usuarioInvita)
                {
                    rf.nivel = "INV";
                    ImporteIndirectoSumar = nivel_invitacion;
                    rf.detallePorcentaje = nivelInvitacionPorcentaje;
                }



                if (EstatusReferenciaId != 3)
                {
                    ImporteDirectoSumar = 0;
                    ImporteIndirectoSumar = 0;
                }


                rf.importeDirecto += ImporteDirectoSumar;
                rf.importeIndirecto += ImporteIndirectoSumar;

                d.referencias.Add(rf);
            }

            return Ok(d);
        }


        private CorteMensual GenerarCorteMensual(int periodoId)
        {
            CorteMensual c = new CorteMensual();


            string qReferenciasMes = $@"select 
                Referido.ID,
                Referido.FechaEfectiva,
                Referido.ProductoID,
                isnull(Referido.PrecioBasePorcentaje, 0) as PrecioBasePorcentaje,
                isnull(Producto.TipoComision, 0) as TipoComision,
                Producto.Nombre,
                Empresa.ID,
                Empresa.RazonSocial,
                ProductoComision.nivel_1,
                UsuarioNivel1.ID as UsuarioNivel_1,
                ProductoComision.nivel_2,
                isnull(UsuarioNivel2.ID, 0) as UsuarioNivel_2,
                ProductoComision.nivel_3,
                isnull(UsuarioNivel3.ID, 0) as UsuarioNivel_3,
                ProductoComision.nivel_4,
                isnull(UsuarioNivel4.ID, 0) as UsuarioNivel_4,
                ProductoComision.nivel_base,
                isnull(Empresa.embajadorId, 0) as UsuarioInvita,
                ProductoComision.nivel_master
                from 
                Referido
                left join Periodo on Referido.FechaEfectiva between Periodo.FechaInicio and Periodo.FechaFin
                left join Producto on Producto.ID = Referido.ProductoID
                left join Empresa on Empresa.id	 = Producto.EmpresaID
                left join ProductoComision on ProductoComision.ProductoID = Producto.ID
                left join Usuario as UsuarioNivel1 on UsuarioNivel1.ID = Referido.UsuarioID
                left join Usuario as UsuarioNivel2 on UsuarioNivel2.ID = UsuarioNivel1.UsuarioParent
                left join Usuario as UsuarioNivel3 on UsuarioNivel3.ID = UsuarioNivel2.UsuarioParent 
                left join Usuario as UsuarioNivel4 on UsuarioNivel4.ID = UsuarioNivel3.UsuarioParent 
                where 1 = 1 and EstatusReferenciaId = 3 and empresa.Grupo = 1 and periodo.ID = {periodoId}
                order by Referido.FechaEfectiva asc";

            DataTable dtReferenciasMes = DataAccess.performQuery(qReferenciasMes);


            string queryComisionDirecta = $@"select 
Usuario.id, 
usuario.Nombres + ' ' + usuario.Apellidos as nombre
from Usuario
left join Periodo on usuario.FechaCreacion <= Periodo.FechaFin
where RolesID = 3 and GrupoID = 1 and periodo.id = {periodoId}";


            c.embajadores = DataAccess.fromQueryListOf<CorteMensualEmbajador>(queryComisionDirecta);

            c.embajadoresMes = c.embajadores.Count;

            decimal GananciasEmbassy = 0;
            decimal MovimientosTotal = 0;

            foreach (DataRow dr in dtReferenciasMes.Rows)
            {
                bool asignado = true;

                int TipoComision = Int32.Parse(dr["TipoComision"].ToString());
                decimal PrecioBasePorcentaje = decimal.Parse(dr["PrecioBasePorcentaje"].ToString());
                //referencias Directas
                decimal nivel1 = decimal.Parse(dr["nivel_1"].ToString());
                decimal nivel2 = decimal.Parse(dr["nivel_2"].ToString());
                decimal nivel3 = decimal.Parse(dr["nivel_3"].ToString());
                decimal nivel4 = decimal.Parse(dr["nivel_4"].ToString());
                decimal nivel_invitacion = decimal.Parse(dr["nivel_base"].ToString());
                decimal nivel_master = decimal.Parse(dr["nivel_master"].ToString());


                
                if (TipoComision == 1)
                {
                    nivel1 = (nivel1 / 100) * PrecioBasePorcentaje;
                    nivel2 = (nivel2 / 100) * PrecioBasePorcentaje;
                    nivel3 = (nivel3 / 100) * PrecioBasePorcentaje;
                    nivel4 = (nivel4 / 100) * PrecioBasePorcentaje;
                    nivel_invitacion = (nivel_invitacion / 100) * PrecioBasePorcentaje;
                    nivel_master = (nivel_master / 100) * PrecioBasePorcentaje;
                }

                int usuarionivel1 = Int32.Parse(dr["UsuarioNivel_1"].ToString());
                int usuarionivel2 = Int32.Parse(dr["UsuarioNivel_2"].ToString());
                int usuarionivel3 = Int32.Parse(dr["UsuarioNivel_3"].ToString());
                int usuarionivel4 = Int32.Parse(dr["UsuarioNivel_4"].ToString());
                int UsuarioInvita = Int32.Parse(dr["UsuarioInvita"].ToString());

                CorteMensualEmbajador directo = c.embajadores.Find(c => c.id == usuarionivel1);
                if (directo != null)
                {
                    directo.referenciasDirectas++;
                    directo.importeDirecto += nivel1;
                }

                CorteMensualEmbajador indirecto_nivel_2 = c.embajadores.Find(c => c.id == usuarionivel2);
                if (indirecto_nivel_2 != null)
                {
                    indirecto_nivel_2.referenciasIndirectas++;
                    indirecto_nivel_2.importeIndirecto += nivel2;
                }

                CorteMensualEmbajador indirecto_nivel_3 = c.embajadores.Find(c => c.id == usuarionivel3);
                if (indirecto_nivel_3 != null)
                {
                    indirecto_nivel_3.referenciasIndirectas++;
                    indirecto_nivel_3.importeIndirecto += nivel3;
                }

                CorteMensualEmbajador indirecto_nivel_4 = c.embajadores.Find(c => c.id == usuarionivel4);
                if (indirecto_nivel_4 != null)
                {
                    indirecto_nivel_4.referenciasIndirectas++;
                    indirecto_nivel_4.importeIndirecto += nivel4;
                }

                CorteMensualEmbajador indirecto_invitacion = c.embajadores.Find(c => c.id == UsuarioInvita);
                if (indirecto_invitacion != null)
                {
                    indirecto_invitacion.referenciasIndirectas++;
                    indirecto_invitacion.importeIndirecto += nivel_invitacion;
                }

                //Referencias indirectas


                //referencia master
                GananciasEmbassy += (nivel_master);
                MovimientosTotal += nivel1 + nivel2 + nivel3 + nivel4 + nivel_invitacion + nivel_master;

            }

            c.importeEmbassy = GananciasEmbassy;
            c.importeTotal = MovimientosTotal;

            return c;
        }


        [HttpGet("getCorteMensual")]
        public async Task<IActionResult> getCorteMensual(int periodoId)
        {
            CorteMensual c = GenerarCorteMensual(periodoId);
            return Ok(c);

        }


        private string numeroMes(int numero)
        {
            string mes = numero.ToString();

            switch (numero)
            {
                case 1: mes = "Enero"; break;
                case 2: mes = "Febrero"; break;
                case 3: mes = "Marzo"; break;
                case 4: mes = "Abril"; break;
                case 5: mes = "Mayo"; break;
                case 6: mes = "Junio"; break;
                case 7: mes = "Julio"; break;
                case 8: mes = "Agosto"; break;
                case 9: mes = "Septiembre"; break;
                case 10: mes = "Octubre"; break;
                case 11: mes = "Noviembre"; break;
                case 12: mes = "Diciembre"; break;
                default: mes = "Mes inválido"; break;
            }

            return mes;
        }



        [HttpGet("GetByID")]
        [SwaggerResponse(200, "Objeto de respuesta", typeof(PeriodoDTO))]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetByID([FromQuery] long id)
        {
            var result = await _periodoService.GetByID(id);

            if (_periodoService.HasError == false)
            {
                if (result == null)
                {
                    return NotFound(GenericResponseDTO<string>.Fail("No se encontro grupo."));
                }
                return Ok(GenericResponseDTO<PeriodoDTO>.Ok(result, "Consulta exitosa"));
            }
            else
            {
                return BadRequest(GenericResponseDTO<string>.Fail(_periodoService.LastError));
            }
        }


        [HttpPost("Save")]
        [SwaggerResponse(200, "Objeto de respuesta", typeof(bool))]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Save([FromBody] PeriodoDTO dto)
        {
            var result = await _periodoService.Save(dto);

            if (_periodoService.HasError == false)
            {
                return Ok(GenericResponseDTO<bool>.Ok(result, "Guardado"));
            }
            else
            {
                return BadRequest(GenericResponseDTO<string>.Fail(_periodoService.LastError));
            }
        }
    }


    public class CorteMensual
    {

        public List<CorteMensualEmbajador> embajadores { get; set; }

        public decimal importeEmbassy { get; set; }
        public decimal importeTotal { get; set; }
        public int referenciasValidas { get; set; }

        public int embajadoresMes { get; set; }
    }


    public class CorteMensualEmbajador
    {
        public int id { get; set; }
        public string nombre { get; set; }
        public int referenciasDirectas { get; set; }
        public decimal importeDirecto { get; set; }
        public int referenciasIndirectas { get; set; }
        public decimal importeIndirecto { get; set; }
    }

    public class DetalleEmbajadorCorteMensual
    {
        public int embajadorId { get; set; }
        public int periodoId { get; set; }
        public string nombreEmbajador { get; set; }
        public string contactoEmbajador { get; set; }
        public DateTime fechaRegistro { get; set; }
        public string datosBancarios { get; set; }
        public double totalImporte { get; set; }
        public List<DetalleEmbajadorCorteMensualReferencia> referencias { get; set; }
    }

    public class DetalleEmbajadorCorteMensualReferencia
    {
        public int referenciaId { get; set; }
        public string embajadorEfectivo { get; set; }
        public DateTime fechaEfectiva { get; set; }
        public string tipoComision { get; set; }
        public string detallePorcentaje { get; set; }
        public string nivel { get; set; }
        public decimal importeDirecto { get; set; }
        public decimal importeIndirecto { get; set; }
        public string producto { get; set; }
        public string nombreReferido { get; set; }
    }

}
