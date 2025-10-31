using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EBEntities.Common
{
    public interface ISoftDeletable
    {
        bool Eliminado { get; set; }
        DateTime? FechaEliminacion { get; set; }
    }
}
