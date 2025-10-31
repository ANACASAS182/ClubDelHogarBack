using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using EBEntities;
using EBRepositories.Interfaces;
using EBServices.Interfaces;
using Microsoft.EntityFrameworkCore;
using EBDTOs;                           // ✅ NECESARIO para CatBancoDto

namespace EBServices
{
    public class CatalogosService : BaseService, ICatalogosService
    {
        private readonly IRepository<CatalogoEstado> _repositoryCatalogoEstado;
        private readonly IRepository<CatalogoPais> _repositoryCatalogoPais;
        private readonly IRepository<CatBanco> _repositoryCatBanco;   // ✅

        public CatalogosService(
            IRepository<CatalogoEstado> repositoryCatalogoEstado,
            IRepository<CatalogoPais> repositoryCatalogoPais,
            IRepository<CatBanco> repositoryCatBanco)                 // ✅
        {
            _repositoryCatalogoEstado = repositoryCatalogoEstado;
            _repositoryCatalogoPais = repositoryCatalogoPais;
            _repositoryCatBanco = repositoryCatBanco;                 // ✅
        }

        public async Task<List<CatalogoEstado>?> GetCatalogoEstadosMexicanos()
        {
            try
            {
                return await _repositoryCatalogoEstado
                    .GetQueryable()
                    .Where(t => t.CodigoPais == "MEX")
                    .ToListAsync();
            }
            catch (System.Exception ex)
            {
                ExceptionToMessage(ex);
                HasError = true;
                return null;
            }
        }

        public async Task<List<CatalogoPais>?> GetCatalogoPais()
        {
            try
            {
                return await _repositoryCatalogoPais.GetAllAsyncList();
            }
            catch (System.Exception ex)
            {
                ExceptionToMessage(ex);
                HasError = true;
                return null;
            }
        }

        // ====== Catálogo de bancos ======
        public async Task<List<CatBancoDto>> GetCatalogoBancos()
        {
            ResetError();
            try
            {
                return await _repositoryCatBanco
                    .GetQueryable()
                    .Where(b => b.Activo)
                    .OrderBy(b => b.Nombre)
                    .Select(b => new CatBancoDto(b.Id, b.Nombre))
                    .ToListAsync();
            }
            catch (System.Exception ex)
            {
                ExceptionToMessage(ex, "No se pudo obtener el catálogo de bancos.");
                HasError = true;
                return new List<CatBancoDto>();
            }
        }
    }
}