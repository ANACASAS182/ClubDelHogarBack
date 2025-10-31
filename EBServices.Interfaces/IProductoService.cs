using EBDTOs;
using EBEntities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EBServices.Interfaces
{
    public interface IProductoService : IBaseService
    {
        Task<PaginationModelDTO<List<ProductoCatalogoDTO>>> GetAllProductosPaginated(int page, int size, string sortBy, string sortDir, string searchQuery, long? grupoId, long? empresaID, int vigenciaFilter);
        Task<PaginationModelDTO<List<ProductoCatalogoDTO>>> GetProductosByEmpresaPaginated(long empresaID, int page, int size, string sortBy, string sortDir, string searchQuery, int vigenciaFilter);
        Task<PaginationModelDTO<List<Producto>>> GetProductosPaginated(long empresaID, int page, int size, string sortBy, string sortDir, string searchQuery);
        Task<List<Producto>?> GetProductosByEmpresa(long empresaID);
        Task<List<ProductoCatalogoDTO>> GetProductosByEmpresaCatalogo(long empresaID);
        Task<bool> Save(ProductoCreateDTO model);
    }
}
