using EBDTOs;
using EBEntities;
using EBRepositories;
using EBRepositories.Interfaces;
using EBServices.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Transactions;

namespace EBServices
{
    public class EmpresaService : BaseService, IEmpresaService
    {

        public IRepository<Empresa> _repositoryEmpresa;
        public IRepository<EmpresaGrupo> _repositoryEmpresaGrupo;
        public IRepository<Grupo> _repositoryGrupo;

        public EmpresaService(IRepository<Empresa> repositoryEmpresa, IRepository<EmpresaGrupo> repositoryEmpresaGrupo, IRepository<Grupo> repositoryGrupo)
        {
            _repositoryEmpresa = repositoryEmpresa;
            _repositoryEmpresaGrupo = repositoryEmpresaGrupo;
            _repositoryGrupo = repositoryGrupo;
        }



        public async Task<bool> Save(EmpresaCreateDTO model)
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
                    Empresa? empresaModel;

                    var query = _repositoryEmpresa.GetQueryable().Where(t => t.RFC == model.rfc);
                    if (model.id > 0)
                    {
                        query = query.Where(t => t.ID != model.id);
                    }

                    var existRFC = await query.FirstOrDefaultAsync();
                    if (existRFC != null) {
                        HasError = true;
                        LastError = "Este RFC " + model.rfc + " ya existe para otra empresa.";
                        return false;
                    }



                    if (model.id > 0)
                    {
                        empresaModel = await _repositoryEmpresa.GetQueryable().FirstOrDefaultAsync(t => t.ID == model.id);
                        if(empresaModel == null)
                        {
                            HasError = true;
                            LastError = "No se encontro empresa a modificar";
                            return false;
                        }
                    }
                    else
                    {
                        empresaModel = new Empresa() { NombreComercial = "", RazonSocial = "", RFC = "" };
                    }

                    empresaModel.RFC = model.rfc!;
                    empresaModel.NombreComercial = model.nombreComercial!;
                    empresaModel.RazonSocial = model.razonSocial!;
                    empresaModel.Descripcion = model.descripcion;
                    empresaModel.Giro = model.giro;
                    empresaModel.Grupo = model.grupo;
                    empresaModel.LogotipoBase64 = model.logotipoBase64;

                    empresaModel.embajadorId = model.embajadorId;

                    if (empresaModel.ID > 0)
                    {
                        _repositoryEmpresa.Update(empresaModel);
                    }
                    else
                    {
                        await _repositoryEmpresa.AddAsync(empresaModel);
                    }

                    await _repositoryEmpresa.SaveAsync();

                    scope.Complete();
                }

                return true;
            }
            catch (Exception ex)
            {
                ExceptionToMessage(ex, "Ocurrio un problema al guardar empresa. ");
                HasError = true;
                return false;
            }
        }

        public async Task<bool> Save(Empresa model)
        {
            ResetError();
            if (model == null)
            {
                LastError = "modelo nulo";
                return false;
            }

            try
            {
                if (model.ID > 0)
                {
                    _repositoryEmpresa.Update(model);
                    await _repositoryEmpresa.SaveAsync();
                }
                else
                {
                    await _repositoryEmpresa.AddAsync(model);
                    await _repositoryEmpresa.SaveAsync();
                }
                return true;
            }
            catch (Exception ex)
            {
                ExceptionToMessage(ex, "Ocurrio un problema al guardar empresa. ");
                HasError = true;
                return false;
            }
        }

        public async Task<List<Empresa>?> GetAllEmpresas()
        {
            ResetError();
            try
            {
                var empresas = await _repositoryEmpresa.GetQueryable().Where(t => !t.Eliminado).ToListAsync();
                return empresas;
            }
            catch (Exception ex)
            {
                ExceptionToMessage(ex, "Error al obtener empresas");
                HasError = true;
                return null;
            }
        }


        public async Task<PaginationModelDTO<List<EmpresaCatalogoDTO>>> GetEmpresasPaginated(int page, int size, string sortBy, string sortDir, string searchQuery, long grupoID)
        {

            ResetError();
            PaginationModelDTO<List<EmpresaCatalogoDTO>> model = new();
            sortDir = sortDir == "" ? "asc" : sortDir;
            sortBy = sortBy == "undefined" ? string.Empty : sortBy;

            try
            {
                //var query = _repositoryEmpresa.GetQueryable().Where(t => !t.Eliminado).AsQueryable();
                //var query = from e in _repositoryEmpresa.GetQueryable()
                //            where !e.Eliminado
                //            join eg in _repositoryEmpresaGrupo.GetQueryable() on e.ID equals eg.EmpresaID into egJoin
                //            from eg in egJoin.DefaultIfEmpty()
                //            join g in _repositoryGrupo.GetQueryable() on eg.GrupoID equals g.ID into gJoin
                //            from g in gJoin.DefaultIfEmpty()
                //            where grupoID == 0 || g.ID == grupoID
                //            group g by new
                //            {
                //                e.ID,
                //                e.RFC,
                //                e.RazonSocial,
                //                e.NombreComercial
                //            } into grp
                //            select new EmpresaCatalogoDTO
                //            {
                //                ID = grp.Key.ID,
                //                RFC = grp.Key.RFC,
                //                NombreComercial = grp.Key.NombreComercial,
                //                RazonSocial = grp.Key.RazonSocial,
                //                Grupos = grp
                //                    .Where(g => g != null)
                //                    .Select(g => new GrupoDTO
                //                    {
                //                        id = g.ID,
                //                        nombre = g.Nombre
                //                    }).ToList(),
                //                //Descripcion = _repositoryEmpresa.GetQueryable()
                //                //                .Where(x => x.ID == grp.Key.ID)
                //                //                .Select(x => x.Descripcion)
                //                //                .FirstOrDefault()
                //            };

                var query = from e in _repositoryEmpresa.GetQueryable()
                            where !e.Eliminado
                            join g in _repositoryGrupo.GetQueryable() on e.Grupo equals g.ID into gJoin
                            from g in gJoin.DefaultIfEmpty()
                            where grupoID == 0 || g.ID == grupoID
                            group g by new
                            {
                                e.ID,
                                e.RFC,
                                e.RazonSocial,
                                e.NombreComercial
                            } into grp
                            select new EmpresaCatalogoDTO
                            {
                                ID = grp.Key.ID,
                                RFC = grp.Key.RFC,
                                NombreComercial = grp.Key.NombreComercial,
                                RazonSocial = grp.Key.RazonSocial,
                                Grupos = grp
                                    .Where(g => g != null)
                                    .Select(g => new GrupoDTO
                                    {
                                        id = g.ID,
                                        nombre = g.Nombre
                                    }).ToList(),
                                // Si necesitas traer la descripción
                                // Descripcion = _repositoryEmpresa.GetQueryable()
                                //                .Where(x => x.ID == grp.Key.ID)
                                //                .Select(x => x.Descripcion)
                                //                .FirstOrDefault()
                            };


                //ordenamiento 
                if (!string.IsNullOrEmpty(sortBy))
                {
                    switch (sortBy)
                    {
                        case "RFC":
                            query = (sortDir == "asc") ? query.OrderBy(t => t.RFC) : query.OrderByDescending(t => t.RFC);
                            break;
                        case "RazonSocial":
                            query = (sortDir == "asc") ? query.OrderBy(t => t.RazonSocial) : query.OrderByDescending(t => t.RazonSocial);
                            break;
                        case "NombreComercial":
                            query = (sortDir == "asc") ? query.OrderBy(t => t.NombreComercial) : query.OrderByDescending(t => t.NombreComercial);
                            break;
                        default:
                            break;
                    }
                }
                else {
                    query = query.OrderBy(t => t.NombreComercial);
                }

                if (!string.IsNullOrEmpty(searchQuery))
                {
                    query = query.Where(t => t.RFC!.Contains(searchQuery) || t.RazonSocial!.Contains(searchQuery) || t.NombreComercial!.Contains(searchQuery));
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
                ExceptionToMessage(ex, "Error al obtener empresas");
                HasError = true;
                return model;
            }
        }


        public async Task<Empresa?> GetEmpresaByID(long id)
        {
            ResetError();
            try
            {
                var empresa = await _repositoryEmpresa.GetQueryable().FirstOrDefaultAsync(t => t.ID == id);
                return empresa;
            }
            catch (Exception ex)
            {
                ExceptionToMessage(ex, "Error al obtener empresas");
                HasError = true;
                return null;
            }
        }



    }
}
