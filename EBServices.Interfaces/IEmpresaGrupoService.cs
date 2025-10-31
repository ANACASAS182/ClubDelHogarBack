using EBDTOs;
using EBEntities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EBServices.Interfaces
{
    public interface IEmpresaGrupoService : IBaseService
    {
        Task<bool> Delete(EmpresaGrupo model);
        Task<bool> Save(EmpresaGrupo model);
        Task<List<EmpresaGrupoDTO>?> GetAllGruposByEmpresa(long empresaID);
    }
}
