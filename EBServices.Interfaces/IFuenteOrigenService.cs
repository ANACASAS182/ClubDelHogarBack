using EBDTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EBServices.Interfaces
{
    public interface IFuenteOrigenService : IBaseService
    {
        Task<List<FuenteOrigenDTO>?> GetFuentesDeOrigen();
    }
}
