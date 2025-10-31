using EBDTOs;
using EBEntities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EBServices.Interfaces
{
    public interface IPeriodoService : IBaseService
    {
        Task<PeriodoDTO?> GetByID(long id);
        Task<PaginationModelDTO<List<PeriodoDTO>>> GetPeriodosPaginated(int page, int size, string sortBy, string sortDir, string searchQuery);
        Task<bool> Save(PeriodoDTO model);
    }
}
