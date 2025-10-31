using EBEntities.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EBEntities
{
    public class FuenteOrigen : Base, ISoftDeletable
    {
        public required string Nombre { get; set; } 
        public string? Descripcion { get; set; }    
    }
}
