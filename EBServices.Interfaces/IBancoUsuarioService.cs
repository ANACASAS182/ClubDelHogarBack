using EBEntities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EBServices.Interfaces
{
    public interface IBancoUsuarioService : IBaseService
    {
        Task<BancoUsuario?> GetBancoByID(long id, long usuarioId);
        Task<List<BancoUsuario>?> GetBancosUsuario(long usuarioId);
        Task<bool> Save(BancoUsuario model);          
        Task<bool> Save(BancoUsuario model, long usuarioId); 
        Task<bool> Delete(long id);
    }

}
