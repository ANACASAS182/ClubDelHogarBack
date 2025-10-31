using EBEntities;
using EBDTOs;

namespace EBServices.Interfaces
{
    public interface ICatalogosService : IBaseService
    {
        Task<List<CatalogoEstado>?> GetCatalogoEstadosMexicanos();
        Task<List<CatalogoPais>?> GetCatalogoPais();

        Task<List<CatBancoDto>> GetCatalogoBancos();
    }
}
