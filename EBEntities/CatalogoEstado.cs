using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EBEntities
{
    public class CatalogoEstado
    {
        public long ID { get; set; }    
        public required string Codigo { get; set; }  
        public required string CodigoPais { get; set; }
        public required string Nombre { get; set; }
    }
}
