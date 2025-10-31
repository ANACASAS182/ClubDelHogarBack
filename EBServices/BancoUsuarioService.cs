using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using EBEntities;
using EBRepositories.Interfaces;
using EBServices.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EBServices
{
    public class BancoUsuarioService : BaseService, IBancoUsuarioService
    {
        private readonly IRepository<BancoUsuario> _repositoryBancoUsuario;
        private readonly IRepository<CatBanco> _repositoryCatBanco;

        public BancoUsuarioService(
            IRepository<BancoUsuario> repositoryBancoUsuario,
            IRepository<CatBanco> repositoryCatBanco)
        {
            _repositoryBancoUsuario = repositoryBancoUsuario;
            _repositoryCatBanco = repositoryCatBanco;
        }

        public async Task<BancoUsuario?> GetBancoByID(long id, long usuarioId)
        {
            ResetError();
            try
            {
                return await _repositoryBancoUsuario
                    .GetQueryable()
                    .FirstOrDefaultAsync(t => t.UsuarioId == usuarioId && t.Id == id); // ← Id
            }
            catch (Exception ex)
            {
                ExceptionToMessage(ex, "Ocurrió un problema al obtener su banco.");
                return null;
            }
        }

        public async Task<List<BancoUsuario>?> GetBancosUsuario(long usuarioId)
        {
            ResetError();
            try
            {
                return await _repositoryBancoUsuario
                    .GetQueryable()
                    .Where(t => t.UsuarioId == usuarioId && !t.Eliminado)
                    .OrderByDescending(t => t.FechaCreacion)
                    .ToListAsync();
            }
            catch (System.Exception ex)
            {
                ExceptionToMessage(ex, "Ocurrió un problema al obtener sus bancos.");
                return null;
            }
        }

        public async Task<bool> Save(BancoUsuario model)
        {
            ResetError();
            try
            {
                if (model.Id > 0) _repositoryBancoUsuario.Update(model); // ← Id
                else
                {
                    model.FechaCreacion = DateTime.UtcNow;
                    await _repositoryBancoUsuario.AddAsync(model);
                }
                await _repositoryBancoUsuario.SaveAsync();
                return true;
            }
            catch (Exception ex)
            {
                ExceptionToMessage(ex, "Ocurrió un problema al guardar su banco.");
                return false;
            }
        }

        public async Task<bool> Save(BancoUsuario model, long usuarioId)
        {
            ResetError();
            try
            {
                model.UsuarioId = usuarioId;

                string? nombreBanco = null;
                if (model.CatBancoId.HasValue)
                {
                    nombreBanco = await _repositoryCatBanco
                        .GetQueryable()
                        .Where(b => b.Id == model.CatBancoId && b.Activo)
                        .Select(b => b.Nombre)
                        .FirstOrDefaultAsync();
                }
                if (string.IsNullOrWhiteSpace(nombreBanco))
                    nombreBanco = (model.BancoOtro ?? model.NombreBanco)?.Trim();

                if (string.IsNullOrWhiteSpace(nombreBanco))
                    throw new System.ArgumentException("Nombre de banco inválido.");

                model.NombreBanco = nombreBanco;

                if (model.TipoCuenta != 0 && model.TipoCuenta != 1)
                    model.TipoCuenta = 0;

                model.NumeroCuenta = (model.NumeroCuenta ?? "").Trim();

                // 👇 Usa model.ID para decidir alta/edición
                if (model.ID > 0)
                    _repositoryBancoUsuario.Update(model);
                else
                {
                    model.FechaCreacion = System.DateTime.UtcNow;
                    await _repositoryBancoUsuario.AddAsync(model);
                }

                await _repositoryBancoUsuario.SaveAsync();
                return true;
            }
            catch (System.Exception ex)
            {
                ExceptionToMessage(ex, "Ocurrió un problema al guardar su banco.");
                return false;
            }
        }

        public async Task<bool> Delete(long id)
        {
            ResetError();
            try
            {
                await _repositoryBancoUsuario.SoftDeleteAsync(id); // asume PK = ID
                await _repositoryBancoUsuario.SaveAsync();
                return true;
            }
            catch (System.Exception ex)
            {
                ExceptionToMessage(ex, "Ocurrió un error al borrar su banco.");
                return false;
            }
        }
    }
}
