using EBEntities;
using EBRepositories.Interfaces;
using EBServices.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace EBServices
{
    public class PasswordRecoveryService : BaseService, IPasswordRecoveryService
    {
        private readonly IRepository<PasswordRecovery> _passwordRecoveryRepository;

        public PasswordRecoveryService(IRepository<PasswordRecovery> passwordRecoveryRepository)
        {
            _passwordRecoveryRepository = passwordRecoveryRepository;
        }

        public async Task<PasswordRecovery?> AnySolicitudActivaUsuario(long usuarioID)
        {
            ResetError();
            try
            {
                var nowUtc = DateTime.UtcNow;

                // ACTIVA = no usada, no eliminada, no vencida
                var q = _passwordRecoveryRepository.GetQueryable()
                    .Where(t => t.UsuarioID == usuarioID
                             && (t.Usado == null || t.Usado == false)
                             && (t.Eliminado == null || t.Eliminado == false)
                             && t.FechaVencimiento > nowUtc)
                    .OrderByDescending(t => t.FechaCreacion);

                return await q.FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                ExceptionToMessage(ex, "Error al comprobar solicitud de cambio de contraseña.");
                HasError = true;
                return null;
            }
        }

        public async Task<PasswordRecovery?> GetSolicitudByToken(string token)
        {
            ResetError();
            try
            {
                var tk = (token ?? string.Empty).Trim();
                if (string.IsNullOrEmpty(tk)) return null;

                // NO filtramos Eliminado/Usado: el controlador decide si responde 410
                return await _passwordRecoveryRepository.GetQueryable()
                    .AsNoTracking()
                    .FirstOrDefaultAsync(t => t.Token == tk);
            }
            catch (Exception ex)
            {
                ExceptionToMessage(ex, "Error al obtener solicitud de cambio de contraseña.");
                HasError = true;
                return null;
            }
        }

        public async Task<bool> ExistToken(string token)
        {
            ResetError();
            try
            {
                var tk = (token ?? string.Empty).Trim();
                if (string.IsNullOrEmpty(tk)) return false;

                return await _passwordRecoveryRepository.GetQueryable()
                    .AnyAsync(t => t.Token == tk);
            }
            catch (Exception ex)
            {
                HasError = true;
                ExceptionToMessage(ex, "Error al verificar token de cambio de contraseña.");
                return false;
            }
        }

        public async Task<bool> Save(PasswordRecovery model)
        {
            ResetError();
            try
            {
                if (model.ID > 0)
                    _passwordRecoveryRepository.Update(model);
                else
                    await _passwordRecoveryRepository.AddAsync(model);

                await _passwordRecoveryRepository.SaveAsync();
                return true;
            }
            catch (Exception ex)
            {
                HasError = true;
                ExceptionToMessage(ex, "Error al guardar petición de recuperación de contraseña.");
                return false;
            }
        }

        public async Task<bool> DeleteSolicitud(PasswordRecovery model)
        {
            ResetError();
            try
            {
                // Soft delete (marca Eliminado/FechaEliminacion adentro del repo)
                await _passwordRecoveryRepository.SoftDeleteAsync(model.ID);
                await _passwordRecoveryRepository.SaveAsync();
                return true;
            }
            catch (Exception ex)
            {
                HasError = true;
                ExceptionToMessage(ex, "Error al borrar solicitud de cambio de contraseña anterior.");
                return false;
            }
        }
    }
}
