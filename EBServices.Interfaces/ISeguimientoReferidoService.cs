using EBEntities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EBServices.Interfaces
{
    public interface ISeguimientoReferidoService : IBaseService
    {
        Task<List<SeguimientoReferido>?> GetSeguimietosReferido(long referidoID);
        Task<bool> Save(SeguimientoReferido model);
    }
}
