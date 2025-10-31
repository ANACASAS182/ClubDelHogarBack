using EBEnums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EBDTOs
{
    public class ProductoCreateDTO
    {
        public long? Id { get; set; }
        public long EmpresaID { get; set; }
        public string Nombre { get; set; }
        public string? Descripcion { get; set; }
        public TipoComisionEnum? TipoComision { get; set; }
        public decimal? ComisionPorcentajeCantidad { get; set; }
        public DateTime? FechaCaducidad { get; set; }
        public decimal? Nivel1 { get; set; }
        public decimal? Nivel2 { get; set; }
        public decimal? Nivel3 { get; set; }
        public decimal? Nivel4 { get; set; }
        public decimal? NivelInvitacion { get; set; }
        public decimal? NivelMaster { get; set; }

    }
}
