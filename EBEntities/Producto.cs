using EBEntities.Common;
using EBEnums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EBEntities
{
    public class Producto : Base, ISoftDeletable
    {
        public required long EmpresaID {  get; set; }
        public required string Nombre { get; set; }
        public string? Descripcion { get; set; }
        public TipoComisionEnum? TipoComision { get; set;}
        public decimal? Precio {  get; set; }   
        public DateTime? FechaCaducidad {  get; set; }   

        public Empresa? Empresa { get; set; }
    }
}
