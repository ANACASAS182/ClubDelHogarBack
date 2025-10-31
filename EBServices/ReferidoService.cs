using EBDTOs;
using EBEntities;
using EBEnums;
using EBRepositories.Interfaces;
using EBServices.Interfaces;
using Microsoft.EntityFrameworkCore;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;
//using System.Linq.Dynamic.Core;

namespace EBServices
{
    public class ReferidoService : BaseService, IReferidoService
    {
        public IRepository<Referido> _repositoryReferido;
        public IRepository<EstatusReferencia> _repositoryEstatusReferencia;
        public IRepository<EmpresaGrupo> _repositoryEmpresaGrupo;
        public IRepository<Empresa> _repositoryEmpresa;
        public IRepository<Producto> _repositoryProducto;
        public IRepository<Grupo> _repositoryGrupo;
        public IRepository<Usuario> _repositoryUsuario;
        public IRepository<Cupones> _repositoryCupones;

        public ReferidoService(IRepository<Referido> repositoryReferido, IRepository<EstatusReferencia> repositoryEstatusReferencia, IRepository<Cupones> repositoryCupones,
            IRepository<EmpresaGrupo> repositoryEmpresaGrupo, IRepository<Empresa> repositoryEmpresa, IRepository<Producto> repositoryProducto, IRepository<Grupo> repositoryGrupo, IRepository<Usuario> repositoryUsuario)
        {
            _repositoryReferido = repositoryReferido;
            _repositoryEstatusReferencia = repositoryEstatusReferencia;
            _repositoryEmpresaGrupo = repositoryEmpresaGrupo;
            _repositoryEmpresa = repositoryEmpresa;
            _repositoryProducto = repositoryProducto;
            _repositoryGrupo = repositoryGrupo;
            _repositoryUsuario = repositoryUsuario;
            _repositoryCupones = repositoryCupones;

        }

        public async Task<PaginationModelDTO<List<ReferidoDTO>>> GetReferidosPaginated(long usuarioID, int page, int size, string sortBy, string sortDir, string searchQuery)
        {

            ResetError();
            PaginationModelDTO<List<ReferidoDTO>> model = new();
            sortDir = sortDir == "" ? "asc" : sortDir;
            sortBy = sortBy == "undefined" ? string.Empty : sortBy;

            try
            {
                var query = _repositoryReferido.GetQueryable().AsQueryable().Where(t => !t.Eliminado && t.UsuarioID == usuarioID);

                //ordenamiento 
                if (!string.IsNullOrEmpty(sortBy))
                {
                    switch (sortBy)
                    {
                        case "Nombre":
                            query = (sortDir == "asc") ? query.OrderBy(t => t.NombreCompleto) : query.OrderByDescending(t => t.NombreCompleto);
                            break;
                        case "Celular":
                            query = (sortDir == "asc") ? query.OrderBy(t => t.Celular) : query.OrderByDescending(t => t.Celular);
                            break;
                        case "Empresa":
                            query = (sortDir == "asc") ? query.OrderBy(t => t.Producto!.Empresa!.RazonSocial) : query.OrderByDescending(t => t.Producto!.Empresa!.RazonSocial);
                            break;
                        case "Producto":
                            query = (sortDir == "asc") ? query.OrderBy(t => t.Producto!.Nombre) : query.OrderByDescending(t => t.Producto!.Nombre);
                            break;
                        case "Estatus":
                            query = (sortDir == "asc") ? query.OrderBy(t => t.EstatusReferencia!.Nombre) : query.OrderByDescending(t => t.EstatusReferencia!.Nombre);
                            break;
                        default:
                            break;
                    }
                    //query = query.OrderBy($"{sortBy} {sortDir}");
                }

                if (!string.IsNullOrEmpty(searchQuery))
                {
                    query = query.Where(t => t.NombreCompleto.Contains(searchQuery) || t.Celular.Contains(searchQuery) || t.Producto!.Empresa!.RazonSocial.Contains(searchQuery)
                    || t.Producto!.Nombre.Contains(searchQuery) || t.EstatusReferencia!.Nombre.Contains(searchQuery));
                }

                var total = query.Count();

                var items = await query
                    .Skip(page * size)
                    .Take(size)
                    .Select(t => new ReferidoDTO
                    {
                        ID = t.ID,
                        Celular = t.Celular,
                        Email = t.Email,
                        NombreCompleto = t.NombreCompleto,
                        UsuarioID = t.UsuarioID,
                        EmpresaID = t.Producto!.EmpresaID,
                        ProductoID = t.ProductoID,
                        EmpresaRazonSocial = t.Producto!.Empresa!.RazonSocial,
                        ProductoNombre = t.Producto!.Nombre,
                        EstatusReferenciaDescripcion = t.EstatusReferencia!.Nombre,
                        EstatusReferenciaID = t.EstatusReferenciaID,
                        EstatusReferenciaEnum = t.EstatusReferencia!.EnumValue,
                        Comision = 0,
                        FechaRegistro = t.FechaCreacion
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


        public async Task<EstatusReferencia?> GetEstatusReferenciaByEnum(EstatusReferenciaEnum enume)
        {
            ResetError();

            try
            {
                return await _repositoryEstatusReferencia.GetQueryable().FirstOrDefaultAsync(t => t.EnumValue == enume);
            }
            catch (Exception ex)
            {
                HasError = true;
                ExceptionToMessage(ex, "Ocurrio un problema al obtener estatus de referencia.");
                return null;
            }
        }

        public async Task<bool> Save(Referido model)
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
                    _repositoryReferido.Update(model);
                    await _repositoryReferido.SaveAsync();
                }
                else
                {
                    await _repositoryReferido.AddAsync(model);
                    await _repositoryReferido.SaveAsync();
                }
                return true;
            }
            catch (Exception ex)
            {
                ExceptionToMessage(ex, "Ocurrio un problema al guardar referido. ");
                HasError = true;
                return false;
            }
        }

        public async Task<List<ReferidoDTO>?> GetReferidosByUsuario(long usuarioID)
        {
            ResetError();
            try
            {
                var referidos = await _repositoryReferido.GetQueryable().Where(t => !t.Eliminado && t.UsuarioID == usuarioID)
                    .Select(t => new ReferidoDTO
                    {
                        ID = t.ID,
                        Celular = t.Celular,
                        Email = t.Email,
                        EstatusReferenciaID = t.EstatusReferenciaID,
                        NombreCompleto = t.NombreCompleto,
                        ProductoID = t.ProductoID,
                        UsuarioID = t.UsuarioID,
                        EmpresaID = t.Producto != null ? t.Producto.EmpresaID : null,
                        EstatusReferenciaDescripcion = t.EstatusReferencia != null ? t.EstatusReferencia.Descripcion : ""
                    }).ToListAsync();


                return referidos;
            }
            catch (Exception ex)
            {
                ExceptionToMessage(ex, "Error al obtener referidos");
                HasError = true;
                return null;
            }
        }

        public async Task<ReferidoDTO?> GetReferidoByID(long id)
        {
            ResetError();
            try
            {
                var referido = await _repositoryReferido.GetQueryable().Include(t => t.EstatusReferencia).Include(t => t.Producto).Include(t => t.Usuario)
                    .Select(t => new ReferidoDTO
                    {
                        ID = t.ID,
                        Celular = t.Celular,
                        Email = t.Email,
                        EstatusReferenciaID = t.EstatusReferenciaID,
                        EstatusReferenciaEnum = t.EstatusReferencia!.EnumValue,
                        NombreCompleto = t.NombreCompleto,
                        ProductoID = t.ProductoID,
                        ProductoNombre = t.Producto.Nombre,
                        UsuarioID = t.UsuarioID,
                        EmpresaID = t.Producto != null ? t.Producto.EmpresaID : null,
                        EstatusReferenciaDescripcion = t.EstatusReferencia != null ? t.EstatusReferencia.Nombre : "",
                        UsuarioNombre = t.Usuario.Nombres,
                        UsuarioApellido = t.Usuario.Apellidos,
                        UsuarioNombreCompleto = t.Usuario.Nombres + " " + t.Usuario.Apellidos,
                        FechaVencimiento = t.Producto.FechaCaducidad
                    }).FirstOrDefaultAsync(t => t.ID == id);

                return referido;
            }
            catch (Exception ex)
            {
                ExceptionToMessage(ex, "Error al obtener referidos");
                HasError = true;
                return null;
            }
        }


        public async Task<PaginationModelDTO<List<ReferidoCatalogoDTO>>> GetAllReferidosPaginated(int page, int size, string sortBy, string sortDir, string searchQuery, long? grupoId, long? empresaId, EstatusReferenciaEnum? estatusEnum, long? usuarioID)
        {
            ResetError();
            PaginationModelDTO<List<ReferidoCatalogoDTO>> model = new();
            sortDir = string.IsNullOrEmpty(sortDir) ? "asc" : sortDir;
            sortBy = sortBy == "undefined" ? string.Empty : sortBy;

            try
            {
                var baseQuery = from referido in _repositoryReferido.GetQueryable()
                                join embajador in _repositoryUsuario.GetQueryable() on referido.UsuarioID equals embajador.ID
                                join estatus in _repositoryEstatusReferencia.GetQueryable() on referido.EstatusReferenciaID equals estatus.ID
                                join producto in _repositoryProducto.GetQueryable() on referido.ProductoID equals producto.ID
                                join empresa in _repositoryEmpresa.GetQueryable() on producto.EmpresaID equals empresa.ID
                                select new
                                {
                                    Referido = referido,
                                    EstatusReferencia = estatus,
                                    Producto = producto,
                                    Empresa = empresa,
                                    Embajador = embajador
                                };

                if (!string.IsNullOrEmpty(searchQuery))
                {
                    baseQuery = baseQuery.Where(x =>
                        x.Referido.NombreCompleto.Contains(searchQuery) ||
                        x.Referido.Celular.Contains(searchQuery) ||
                        x.Referido.Email.Contains(searchQuery) ||
                        x.EstatusReferencia.Nombre.Contains(searchQuery) ||
                        x.Producto.Nombre.Contains(searchQuery) ||
                        x.Empresa.RazonSocial.Contains(searchQuery) 
                    );
                }

                
                if (empresaId.HasValue && empresaId > 0)
                {
                    baseQuery = baseQuery.Where(x => x.Empresa != null && x.Empresa.ID == empresaId.Value);
                }

                if (estatusEnum.HasValue)
                {
                    baseQuery = baseQuery.Where(x => x.EstatusReferencia != null && x.EstatusReferencia.EnumValue == estatusEnum);
                }

                if (usuarioID.HasValue && usuarioID > 0)
                {
                    baseQuery = baseQuery.Where(x => x.Embajador != null && x.Embajador.ID == usuarioID);
                }


                var total = await baseQuery.Select(x => x.Producto.ID).Distinct().CountAsync();

                // Ordenamiento
                baseQuery = sortBy switch
                {
                    "Nombre" => (sortDir == "asc") ? baseQuery.OrderBy(x => x.Referido.NombreCompleto) : baseQuery.OrderByDescending(x => x.Referido.NombreCompleto),
                    "Email" => (sortDir == "asc") ? baseQuery.OrderBy(x => x.Referido.Email) : baseQuery.OrderByDescending(x => x.Referido.Email),
                    "Celular" => (sortDir == "asc") ? baseQuery.OrderBy(x => x.Referido.Celular) : baseQuery.OrderByDescending(x => x.Referido.Celular),
                    "Producto" => (sortDir == "asc") ? baseQuery.OrderBy(x => x.Producto.Nombre) : baseQuery.OrderByDescending(x => x.Producto.Nombre),
                    "EstatusReferencia" => (sortDir == "asc") ? baseQuery.OrderBy(x => x.EstatusReferencia.Nombre) : baseQuery.OrderByDescending(x => x.EstatusReferencia.Nombre),
                    "Empresa" => (sortDir == "asc") ? baseQuery.OrderBy(x => x.Empresa.RazonSocial) : baseQuery.OrderByDescending(x => x.Empresa.RazonSocial),
                    "Embajador" => (sortDir == "asc") ? baseQuery.OrderBy(x => x.Embajador!.Nombres + " " + x.Embajador.Apellidos) : baseQuery.OrderByDescending(x => x.Embajador!.Nombres + " " + x.Embajador.Apellidos),
                    _ => baseQuery.OrderBy(x => x.Referido.NombreCompleto)
                };

                var rawData = await baseQuery
                    .Skip(page * size)
                    .Take(size)
                    .Select(x => new
                    {
                        ReferidoID = x.Referido.ID,
                        x.Referido.NombreCompleto,
                        x.Referido.Email,
                        x.Referido.Celular,
                        Producto = x.Producto.Nombre,
                        EmbajadorNombre = x.Embajador.Nombres + " " + x.Embajador.Apellidos,
                        ProductoVigente = x.Producto.FechaCaducidad.Value >= DateTime.Now ? true : false,
                        EstatusReferencia = x.EstatusReferencia.Nombre,
                        EmpresaRazonSocial = x.Empresa.RazonSocial
                    })
                    .ToListAsync();

                var items = rawData
                    .GroupBy(x => x.ReferidoID)
                    .Select(g => new ReferidoCatalogoDTO
                    {
                        ID = g.Key,
                        Nombre = g.First().NombreCompleto,
                        Celular = g.First().Celular,
                        Email = g.First().Email,
                        EstatusRerefencia = g.First().EstatusReferencia,
                        Producto = g.First().Producto,
                        Empresa = g.First().EmpresaRazonSocial,
                        ProductoVigente = g.First().ProductoVigente,
                        Embajador = g.First().EmbajadorNombre
                    })
                    .ToList();

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

        public async Task<PaginationModelDTO<List<ReferidoCatalogoDTO>>> GetReferidosByEmpresaPaginated(long empresaID, int page, int size, string sortBy, string sortDir, string searchQuery, EstatusReferenciaEnum? estatusEnum, long? usuarioID)
        {

            ResetError();
            PaginationModelDTO<List<ReferidoCatalogoDTO>> model = new();
            sortDir = sortDir == "" ? "asc" : sortDir;
            sortBy = sortBy == "undefined" ? string.Empty : sortBy;

            try
            {

                var query =
                            from referido in _repositoryReferido.GetQueryable()
                                .Include(t => t.Producto)
                                .Include(t => t.EstatusReferencia)
                                .Include(t => t.Usuario)
                            where !referido.Eliminado && referido.Producto!.EmpresaID == empresaID
                            join cupon in _repositoryCupones.GetQueryable()
                                on referido.ID equals cupon.referidoID into referidoCupones
                            from cupon in referidoCupones.DefaultIfEmpty()
                            select new { referido, cupon };


                //var query = _repositoryReferido.GetQueryable().Include(t => t.Producto).Include(t => t.EstatusReferencia).Include(t => t.Usuario).AsQueryable().Where(t => !t.Eliminado && t.Producto!.EmpresaID == empresaID);

                //ordenamiento
                if (!string.IsNullOrEmpty(sortBy))
                {
                    switch (sortBy)
                    {
                        case "Nombre":
                            query = (sortDir == "asc") ? query.OrderBy(t => t.referido.NombreCompleto) : query.OrderByDescending(t => t.referido.NombreCompleto);
                            break;
                        case "Email":
                            query = (sortDir == "asc") ? query.OrderBy(t => t.referido.Email) : query.OrderByDescending(t => t.referido.Email);
                            break;
                        case "Celular":
                            query = (sortDir == "asc") ? query.OrderBy(t => t.referido.Celular) : query.OrderByDescending(t => t.referido.Celular);
                            break;
                        case "Producto":
                            query = (sortDir == "asc") ? query.OrderBy(t => t.referido.Producto!.Nombre) : query.OrderByDescending(t => t.referido.Producto!.Nombre);
                            break;
                        case "Embajador":
                            query = (sortDir == "asc")
                                ? query.OrderBy(t => t.referido.Usuario!.Nombres + " " + t.referido.Usuario.Apellidos)
                                : query.OrderByDescending(t => t.referido.Usuario!.Nombres + " " + t.referido.Usuario.Apellidos);
                            break;
                        case "EstatusReferencia":
                            query = (sortDir == "asc") ? query.OrderBy(t => t.referido.EstatusReferencia!.Nombre) : query.OrderByDescending(t => t.referido.EstatusReferencia!.Nombre);
                            break;
                        default:
                            break;
                    }
                }

                if (!string.IsNullOrEmpty(searchQuery))
                {
                    query = query.Where(t => t.referido.NombreCompleto.Contains(searchQuery) ||
                    t.referido.Email.Contains(searchQuery) ||
                     t.referido.Celular.Contains(searchQuery) ||
                      t.referido.Producto!.Nombre.Contains(searchQuery) ||
                       t.referido.EstatusReferencia!.Nombre.Contains(searchQuery)
                    );
                }

                if (estatusEnum.HasValue)
                {
                    query = query.Where(x => x.referido.EstatusReferencia != null && x.referido.EstatusReferencia.EnumValue == estatusEnum);
                }


                if (usuarioID.HasValue && usuarioID > 0)
                {
                    query = query.Where(x => x.referido.UsuarioID == usuarioID);
                }

                var total = query.Count();

                var items = await query
                    .Skip(page * size)
                    .Take(size).Select(t => new ReferidoCatalogoDTO
                    {
                        ID = t.referido.ID,
                        Nombre = t.referido.NombreCompleto,
                        Celular = t.referido.Celular,
                        Email = t.referido.Email,
                        EstatusRerefencia = t.referido.EstatusReferencia!.Nombre,
                        Producto = t.referido.Producto!.Nombre,
                        CodigoCupon = t.cupon == null ? "" : t.cupon.codigo,
                        ProductoVigente = t.referido.Producto.FechaCaducidad == null ? true : (t.referido.Producto.FechaCaducidad.Value >= DateTime.Now ? true : false),
                        Embajador = t.referido.Usuario.Nombres + " " + t.referido.Usuario.Apellidos
                    })
                    .ToListAsync();

                model.Items = items;
                model.Total = total;

                return model;
            }
            catch (Exception ex)
            {
                ExceptionToMessage(ex, "Error al obtener");
                HasError = true;
                return model;
            }
        }


        public async Task<bool> UpdateEstatus(EBDTOs.EstatusReferidoDTO model)
        {
            ResetError();
            if (model == null)
            {
                LastError = "modelo nulo";
                return false;
            }

            try
            {
                var referido = await _repositoryReferido.GetQueryable()
                    .FirstOrDefaultAsync(t => t.ID == model.ID);
                if (referido == null)
                {
                    LastError = "No se pudo obtener el referido";
                    HasError = true;
                    return false;
                }

                var estatusReferencia = await _repositoryEstatusReferencia.GetQueryable()
                    .FirstOrDefaultAsync(t => t.EnumValue == (EBEnums.EstatusReferenciaEnum)model.EstatusReferenciaEnum);
                // o: ((int)t.EnumValue) == model.EstatusReferenciaEnum

                if (estatusReferencia == null)
                {
                    LastError = "No se pudo obtener el estatus";
                    HasError = true;
                    return false;
                }

                referido.EstatusReferenciaID = estatusReferencia.ID;

                _repositoryReferido.Update(referido);
                await _repositoryReferido.SaveAsync();

                return true;
            }
            catch (Exception ex)
            {
                ExceptionToMessage(ex, "Ocurrio un problema al guardar referido. ");
                HasError = true;
                return false;
            }
        }

        public async Task<string> LeerQRBase64(string codigo, string folderDefault = "C:\\tmp\\qr\\")
        {
            string pathQR = Path.Combine(folderDefault, codigo + ".png");

            try
            {
                if (File.Exists(pathQR))
                {
                    byte[] imageBytes = await File.ReadAllBytesAsync(pathQR);
                    string base64String = Convert.ToBase64String(imageBytes);

                    return "data:image/png;base64," + base64String;
                }
                else
                {
                    throw new FileNotFoundException("El archivo QR no existe en la ruta especificada.");
                }
            }
            catch (Exception ex)
            {
                HasError = true;
                ExceptionToMessage(ex, "Ocurrio un problema al obtener QR.");
                return string.Empty;
            }
        }

    }
}
