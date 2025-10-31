using EBDTOs;
using EBEntities;
using EBRepositories.Interfaces;
using EBServices.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EBServices
{
    public class GrupoService : BaseService, IGrupoService
    {

        public IRepository<Grupo> _repositoryGrupo;

        public GrupoService(IRepository<Grupo> repositoryGrupo)
        {
            _repositoryGrupo = repositoryGrupo;
        }


        public async Task<List<GrupoDTO>?> GetAllGrupos()
        {

            ResetError();

            try
            {
                var grupos = await _repositoryGrupo.GetQueryable().AsQueryable().Select(t => new GrupoDTO
                {
                    id = t.ID,
                    nombre = t.Nombre,
                }).ToListAsync();
               
                return grupos;
            }
            catch (Exception ex)
            {
                ExceptionToMessage(ex, "Error al obtener grupos");
                HasError = true;
                return null;
            }
        }


        public async Task<GrupoDTO?> GetByID(long id)
        {
            ResetError();
            try
            {
                var grupo = await _repositoryGrupo.GetQueryable().Select(t => new GrupoDTO
                {
                    id = t.ID,
                    nombre = t.Nombre,
                }).FirstOrDefaultAsync(t => t.id == id);

                return grupo;
            }
            catch (Exception ex)
            {
                ExceptionToMessage(ex, "Ocurrio un problema al obtener su grupo.");
                return null;
            }
        }

        public async Task<PaginationModelDTO<List<GrupoDTO>>> GetGruposPaginated(int page, int size, string sortBy, string sortDir, string searchQuery)
        {

            ResetError();
            PaginationModelDTO<List<GrupoDTO>> model = new();
            sortDir = sortDir == "" ? "asc" : sortDir;
            sortBy = sortBy == "undefined" ? string.Empty : sortBy;

            try
            {
                var query = _repositoryGrupo.GetQueryable().AsQueryable();

                //ordenamiento 
                if (!string.IsNullOrEmpty(sortBy))
                {
                    switch (sortBy)
                    {
                        case "Nombre":
                            query = (sortDir == "asc") ? query.OrderBy(t => t.Nombre) : query.OrderByDescending(t => t.Nombre);
                            break;

                        default:
                            break;
                    }
                }

                if (!string.IsNullOrEmpty(searchQuery))
                {
                    query = query.Where(t => t.Nombre.Contains(searchQuery));
                }

                var total = query.Count();

                var items = await query
                    .Skip(page * size)
                    .Take(size)
                    .Select(t => new GrupoDTO
                    {
                        id = t.ID,
                        nombre = t.Nombre,
                    })
                    .ToListAsync();

                model.Items = items;
                model.Total = total;

                return model;
            }
            catch (Exception ex)
            {
                ExceptionToMessage(ex, "Error al obtener grupos");
                HasError = true;
                return model;
            }
        }



        public async Task<bool> Save(Grupo model)
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
                    _repositoryGrupo.Update(model);
                    await _repositoryGrupo.SaveAsync();
                }
                else
                {
                    await _repositoryGrupo.AddAsync(model);
                    await _repositoryGrupo.SaveAsync();
                }
                return true;
            }
            catch (Exception ex)
            {
                ExceptionToMessage(ex, "Ocurrio un problema al guardar grupo. ");
                HasError = true;
                return false;
            }
        }



    }
}
