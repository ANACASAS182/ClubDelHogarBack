using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EBEntities
{
    public class CatalogoPais
    {
        public long ID { get; set; }
        public required string Codigo {  get; set; }
        public required string Descripcion { get; set; }
    }
}
