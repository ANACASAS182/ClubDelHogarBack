using EBEntities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EBServices.Interfaces
{
    public interface IPasswordRecoveryService : IBaseService
    {
        Task<bool> Save(PasswordRecovery model);
        Task<PasswordRecovery?> GetSolicitudByToken(string token);
        Task<bool> ExistToken(string token);
        Task<PasswordRecovery?> AnySolicitudActivaUsuario(long usuarioID);
        Task<bool> DeleteSolicitud(PasswordRecovery model);
    }
}
