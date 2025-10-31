using EBEntities;
using EBEntities.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EBEntities
{
    public class SeguimientoReferido : Base, ISoftDeletable
    {
        public DateTime FechaSeguimiento { get; set; }
        public string? Comentario { get; set; }
        public long ReferidoID { get; set; }
        public Referido? Referido { get; set; }

    }
}
