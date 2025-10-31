using EBDTOs;
using EBEntities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EBServices.Interfaces
{
    public interface IEmpresaService : IBaseService
    {
        Task<List<Empresa>?> GetAllEmpresas();
        Task<Empresa?> GetEmpresaByID(long id);
        Task<PaginationModelDTO<List<EmpresaCatalogoDTO>>> GetEmpresasPaginated(int page, int size, string sortBy, string sortDir, string searchQuery, long grupoID);
        Task<bool> Save(Empresa model);
        Task<bool> Save(EmpresaCreateDTO model);
    }
}
