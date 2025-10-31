using EBDTOs;
using EBEntities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EBServices.Interfaces
{
    public interface IGrupoService : IBaseService
    {
        Task<PaginationModelDTO<List<GrupoDTO>>> GetGruposPaginated(int page, int size, string sortBy, string sortDir, string searchQuery);
        Task<bool> Save(Grupo model);
        Task<GrupoDTO?> GetByID(long id);
        Task<List<GrupoDTO>?> GetAllGrupos();
    }
}
