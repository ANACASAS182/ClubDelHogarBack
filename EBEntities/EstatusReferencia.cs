using EBEntities.Common;
using EBEnums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EBEntities
{
    public class EstatusReferencia : Base, ISoftDeletable
    {
        public EstatusReferenciaEnum EnumValue { get; set; }
        public required string Nombre { get; set; }
        public string? Descripcion { get; set; }
    }
}
