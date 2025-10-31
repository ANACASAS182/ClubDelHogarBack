using EBEnums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EBDTOs
{
    public class ProductoCatalogoDTO
    {
        public long ID { get; set; }
        public long EmpresaID { get; set; }
        public string? Nombre { get; set; }
        public string? Descripcion { get; set; }
        public TipoComisionEnum? TipoComision { get; set; }   // 0 = Monto, 1 = %
        public decimal? ComisionCantidad { get; set; }        // MXN
        public decimal? ComisionPorcentaje { get; set; }      // %
        public decimal? ComisionPorcentajeCantidad { get; set; }
        public decimal? Precio { get; set; }
        public DateTime? FechaCaducidad { get; set; }
        public string? EmpresaRazonSocial { get; set; }
        public List<GrupoDTO> Grupos { get; set; } = new();

        // texto listo para el front
        public string? Comision { get; set; }
    }

}
