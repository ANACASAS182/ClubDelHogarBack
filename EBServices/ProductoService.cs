using EBDTOs;
using EBEntities;
using EBEnums;
using EBRepositories.Interfaces;
using EBServices.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;
//using System.Linq.Dynamic.Core;

namespace EBServices
{
    public class ProductoService : BaseService, IProductoService
    {
        public IRepository<Producto> _repositoryProducto;
        public IRepository<Empresa> _repositoryEmpresa;
        public IRepository<EmpresaGrupo> _repositoryEmpresaGrupo;
        public IRepository<Grupo> _repositoryGrupo;
        public IRepository<ProductoComision> _repositoryProductoComision;

        public ProductoService(IRepository<Producto> repositoryProducto, IRepository<Empresa> repositoryEmpresa, IRepository<EmpresaGrupo> repositoryEmpresaGrupo, IRepository<Grupo> repositoryGrupo, IRepository<ProductoComision> repositoryProductoComision)
        {
            _repositoryProducto = repositoryProducto;
            _repositoryEmpresa = repositoryEmpresa;
            _repositoryEmpresaGrupo = repositoryEmpresaGrupo;
            _repositoryGrupo = repositoryGrupo;
            _repositoryProductoComision = repositoryProductoComision;
        }


        public async Task<PaginationModelDTO<List<ProductoCatalogoDTO>>> GetAllProductosPaginated(
    int page, int size, string sortBy, string sortDir, string searchQuery, long? grupoId, long? empresaID, int vigenciaFilter)
        {
            ResetError();
            PaginationModelDTO<List<ProductoCatalogoDTO>> model = new();
            sortDir = string.IsNullOrEmpty(sortDir) ? "asc" : sortDir;
            sortBy = sortBy == "undefined" ? string.Empty : sortBy;

            try
            {
                var baseQuery =
                    from producto in _repositoryProducto.GetQueryable()
                    join empresa in _repositoryEmpresa.GetQueryable() on producto.EmpresaID equals empresa.ID
                    join pc in _repositoryProductoComision.GetQueryable() on producto.ID equals pc.ProductoID into pcJoin
                    from pc in pcJoin.DefaultIfEmpty()
                    join eg in _repositoryEmpresaGrupo.GetQueryable() on empresa.ID equals eg.EmpresaID into egJoin
                    from eg in egJoin.DefaultIfEmpty()
                    join grupo in _repositoryGrupo.GetQueryable() on eg.GrupoID equals grupo.ID into grupoJoin
                    from grupo in grupoJoin.DefaultIfEmpty()
                    where !producto.Eliminado
                    select new { Producto = producto, Empresa = empresa, Grupo = grupo, PC = pc };

                if (!string.IsNullOrEmpty(searchQuery))
                {
                    baseQuery = baseQuery.Where(x =>
                        x.Producto.Nombre.Contains(searchQuery) ||
                        x.Empresa.RazonSocial.Contains(searchQuery) ||
                        (x.Grupo != null && x.Grupo.Nombre.Contains(searchQuery))
                    );
                }

                if (grupoId.HasValue && grupoId > 0)
                    baseQuery = baseQuery.Where(x => x.Grupo != null && x.Grupo.ID == grupoId.Value);

                if (empresaID.HasValue && empresaID > 0)
                    baseQuery = baseQuery.Where(x => x.Empresa != null && x.Empresa.ID == empresaID.Value);

                if (vigenciaFilter > 0)
                {
                    var now = DateTime.Now;
                    if (vigenciaFilter == 1) // vigentes
                        baseQuery = baseQuery.Where(x => x.Producto.FechaCaducidad == null || x.Producto.FechaCaducidad >= now);
                    else // no vigentes
                        baseQuery = baseQuery.Where(x => x.Producto.FechaCaducidad < now);
                }

                var total = await baseQuery.CountAsync();

                var data = await baseQuery
                    .Skip(page * size)
                    .Take(size)
                    .ToListAsync();

                var items = data.Select(x => new ProductoCatalogoDTO
                {
                    ID = x.Producto.ID,
                    Nombre = x.Producto.Nombre,
                    Descripcion = x.Producto.Descripcion,
                    FechaCaducidad = x.Producto.FechaCaducidad,
                    Precio = x.Producto.Precio,
                    TipoComision = x.Producto.TipoComision,
                    EmpresaID = x.Empresa.ID,
                    EmpresaRazonSocial = x.Empresa.RazonSocial,
                    Grupos = x.Grupo != null
                        ? new List<GrupoDTO> { new GrupoDTO { id = x.Grupo.ID, nombre = x.Grupo.Nombre } }
                        : new List<GrupoDTO>(),
                    Comision = (x.Producto.TipoComision == EBEnums.TipoComisionEnum.Porcentaje)
                        ? $"{(x.PC?.nivel_1 ?? 0):0.##}%"
                        : $"${(x.PC?.nivel_1 ?? 0):0.##} MXN"
                }).ToList();

                model.Items = items;
                model.Total = total;

                return model;
            }
            catch (Exception ex)
            {
                ExceptionToMessage(ex, "Error al obtener productos");
                HasError = true;
                return model;
            }
        }

        public async Task<PaginationModelDTO<List<ProductoCatalogoDTO>>> GetProductosByEmpresaPaginated(
    long empresaID, int page, int size, string sortBy, string sortDir, string searchQuery, int vigenciaFilter)
        {
            ResetError();
            PaginationModelDTO<List<ProductoCatalogoDTO>> model = new();
            sortDir = string.IsNullOrEmpty(sortDir) ? "asc" : sortDir;
            sortBy = sortBy == "undefined" ? string.Empty : sortBy;

            try
            {
                var query =
    from p in _repositoryProducto.GetQueryable()
    join e in _repositoryEmpresa.GetQueryable() on p.EmpresaID equals e.ID
    join pc_ in _repositoryProductoComision.GetQueryable() on p.ID equals pc_.ProductoID into pcJoin
    from pc in pcJoin.DefaultIfEmpty()
    where !p.Eliminado && p.EmpresaID == empresaID
    select new { P = p, E = e, PC = pc };

                var total = await query.CountAsync();

                var items = await query
                    .Skip(page * size)
                    .Take(size)
                    .Select(x => new ProductoCatalogoDTO
                    {
                        ID = x.P.ID,
                        EmpresaID = x.P.EmpresaID,
                        Nombre = x.P.Nombre,
                        Descripcion = x.P.Descripcion,
                        FechaCaducidad = x.P.FechaCaducidad,
                        Precio = x.P.Precio,
                        EmpresaRazonSocial = x.E.RazonSocial,
                        TipoComision = x.P.TipoComision,

                        ComisionCantidad = ((int)(x.P.TipoComision ?? 0) == 0)
                            ? (x.PC != null ? x.PC.nivel_1 : 0)
                            : 0,

                        ComisionPorcentaje = ((int)(x.P.TipoComision ?? 0) == 1)
                            ? (x.PC != null ? x.PC.nivel_1 : 0)
                            : 0,

                        // Aquí formateamos para el front
                        Comision = (x.P.TipoComision == TipoComisionEnum.Porcentaje)
                            ? $"{(x.PC != null ? x.PC.nivel_1 : 0):0.##}%"
                            : $"${(x.PC != null ? x.PC.nivel_1 : 0):0.##} MXN"
                    })
                    .ToListAsync();

                model.Items = items;
                model.Total = total;


                return model;
            }
            catch (Exception ex)
            {
                ExceptionToMessage(ex, "Error al obtener productos");
                HasError = true;
                return model;
            }
        }



        public async Task<PaginationModelDTO<List<Producto>>> GetProductosPaginated(long empresaID, int page, int size, string sortBy, string sortDir, string searchQuery)
        {

            ResetError();
            PaginationModelDTO<List<Producto>> model = new();
            sortDir = sortDir == "" ? "asc" : sortDir;
            sortBy = sortBy == "undefined" ? string.Empty : sortBy;

            try
            {
                var query = _repositoryProducto.GetQueryable().AsQueryable().Where(t => !t.Eliminado && t.EmpresaID == empresaID);

                //ordenamiento
                if (!string.IsNullOrEmpty(sortBy))
                {
                    switch (sortBy)
                    {
                        case "Nombre":
                            query = (sortDir == "asc") ? query.OrderBy(t => t.Nombre) : query.OrderByDescending(t => t.Nombre);
                            break;
                        case "FechaCaducidad":
                            query = (sortDir == "asc") ? query.OrderBy(t => t.FechaCaducidad) : query.OrderByDescending(t => t.FechaCaducidad);
                            break;
                        default:
                            break;
                            //query = query.OrderBy($"{sortBy} {sortDir}");
                    }
                }
                var total = query.Count();

                var items = await query
                    .Skip(page * size)
                    .Take(size)
                    .ToListAsync();

                model.Items = items;
                model.Total = total;

                return model;
            }
            catch (Exception ex)
            {
                ExceptionToMessage(ex, "Error al obtener productos");
                HasError = true;
                return model;
            }
        }

        public async Task<List<Producto>?> GetProductosByEmpresa(long empresaID)
        {
            ResetError();
            try
            {
                var productos = await _repositoryProducto.GetQueryable().Where(t => !t.Eliminado && t.EmpresaID == empresaID).ToListAsync();
                return productos;
            }
            catch (Exception ex)
            {
                ExceptionToMessage(ex, "Error al obtener productos");
                HasError = true;
                return null;
            }
        }


        public async Task<bool> Save(ProductoCreateDTO model)
        {
            ResetError();
            if (model == null)
            {
                LastError = "modelo nulo";
                return false;
            }

            try
            {
                using (var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
                {
                    // === UPDATE ===
                    if ((model.Id ?? 0) > 0)                               // 👈 antes: model.Id > 0
                    {
                        var producto = await _repositoryProducto
                            .GetQueryable()
                            .FirstOrDefaultAsync(p => p.ID == model.Id.Value && !p.Eliminado);  // 👈 usa .Value

                        if (producto == null)
                        {
                            LastError = "Producto no encontrado.";
                            return false;
                        }

                        // Campos editables
                        producto.Nombre = model.Nombre;
                        producto.Descripcion = model.Descripcion;
                        producto.FechaCaducidad = model.FechaCaducidad ?? producto.FechaCaducidad;
                        producto.TipoComision = model.TipoComision ?? producto.TipoComision;

                        await _repositoryProducto.SaveAsync();

                        // Comisión (crea si no existe)
                        var comision = await _repositoryProductoComision
                            .GetQueryable()
                            .FirstOrDefaultAsync(c => c.ProductoID == producto.ID);

                        if (comision == null)
                        {
                            comision = new ProductoComision { ProductoID = producto.ID };
                            await _repositoryProductoComision.AddAsync(comision);
                        }

                        comision.nivel_1 = model.Nivel1 ?? comision.nivel_1;
                        comision.nivel_2 = model.Nivel2 ?? comision.nivel_2;
                        comision.nivel_3 = model.Nivel3 ?? comision.nivel_3;
                        comision.nivel_4 = model.Nivel4 ?? comision.nivel_4;
                        comision.nivel_base = model.NivelInvitacion ?? comision.nivel_base;
                        comision.nivel_master = model.NivelMaster ?? comision.nivel_master;

                        await _repositoryProductoComision.SaveAsync();

                        scope.Complete();
                        return true;
                    }

                    // === INSERT ===
                    if (model.EmpresaID <= 0 ||
                        !await _repositoryEmpresa.GetQueryable().AnyAsync(e => e.ID == model.EmpresaID))
                    {
                        LastError = "Empresa no válida para el producto.";
                        return false;
                    }

                    var nuevo = new Producto
                    {
                        EmpresaID = model.EmpresaID,
                        Nombre = model.Nombre,
                        Descripcion = model.Descripcion,
                        FechaCaducidad = model.FechaCaducidad,
                        Precio = 0,
                        TipoComision = model.TipoComision,
                    };
                    await _repositoryProducto.AddAsync(nuevo);
                    await _repositoryProducto.SaveAsync();

                    var nuevaComision = new ProductoComision
                    {
                        ProductoID = nuevo.ID,
                        nivel_1 = model.Nivel1 ?? 0,
                        nivel_2 = model.Nivel2 ?? 0,
                        nivel_3 = model.Nivel3 ?? 0,
                        nivel_4 = model.Nivel4 ?? 0,
                        nivel_base = model.NivelInvitacion ?? 0,
                        nivel_master = model.NivelMaster ?? 0,
                    };
                    await _repositoryProductoComision.AddAsync(nuevaComision);
                    await _repositoryProductoComision.SaveAsync();

                    scope.Complete();
                    return true;
                }

            }
            catch (Exception ex)
            {
                ExceptionToMessage(ex, "Ocurrio un problema al guardar producto. ");
                HasError = true;
                return false;
            }
        }


        public async Task<List<ProductoCatalogoDTO>> GetProductosByEmpresaCatalogo(long empresaID)
        {
            ResetError();
            try
            {
                var query =
                    from p in _repositoryProducto.GetQueryable()
                    join e in _repositoryEmpresa.GetQueryable() on p.EmpresaID equals e.ID
                    join pc_ in _repositoryProductoComision.GetQueryable() on p.ID equals pc_.ProductoID into pcJoin
                    from pc in pcJoin.DefaultIfEmpty()
                    where !p.Eliminado && p.EmpresaID == empresaID
                    select new { P = p, E = e, PC = pc };

                var list = await query
                    .Select(x => new ProductoCatalogoDTO
                    {
                        ID = x.P.ID,
                        EmpresaID = x.P.EmpresaID,
                        Nombre = x.P.Nombre,
                        Descripcion = x.P.Descripcion,
                        FechaCaducidad = x.P.FechaCaducidad,
                        Precio = x.P.Precio,
                        EmpresaRazonSocial = x.E.RazonSocial,
                        TipoComision = x.P.TipoComision,

                        // 0 = MXN
                        ComisionCantidad = ((int)(x.P.TipoComision ?? 0) == 0)
                            ? (x.P.Precio ?? 0)
                            : 0,

                        // 1 = %
                        ComisionPorcentaje = ((int)(x.P.TipoComision ?? 0) == 1)
                            ? (
                                (x.PC != null
                                  ? (x.PC.nivel_1 + x.PC.nivel_2 + x.PC.nivel_3 + x.PC.nivel_4 + x.PC.nivel_base + x.PC.nivel_master)
                                  : 0
                                )
                              )
                            : 0
                    })
                    .ToListAsync();

                return list;
            }
            catch (Exception ex)
            {
                ExceptionToMessage(ex, "Error al obtener productos catálogo por empresa");
                HasError = true;
                return new List<ProductoCatalogoDTO>();
            }
        }

    }
}
