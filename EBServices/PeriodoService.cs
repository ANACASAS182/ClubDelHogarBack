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
    public class PeriodoService : BaseService, IPeriodoService
    {
        public IRepository<Periodo> _repositoryPeriodo;

        public PeriodoService(IRepository<Periodo> repositoryPeriodo)
        {
            _repositoryPeriodo = repositoryPeriodo;
        }

        public async Task<PeriodoDTO?> GetByID(long id)
        {
            ResetError();
            try
            {
                var model = await _repositoryPeriodo.GetQueryable().Select(t => new PeriodoDTO
                {
                    Anio = t.Anio,
                    FechaFin = t.FechaFin,
                    FechaInicio = t.FechaInicio,
                    FechaPagoEmbajadores = t.FechaPagoEmbajadores,
                    FechaPagoEmpresas = t.FechaPagoEmpresas,
                    Mes = t.Mes,
                    ID = t.ID,
                }).FirstOrDefaultAsync(t => t.ID == id);

                return model;
            }
            catch (Exception ex)
            {
                ExceptionToMessage(ex, "Ocurrio un problema al obtener su grupo.");
                return null;
            }
        }

        public async Task<PaginationModelDTO<List<PeriodoDTO>>> GetPeriodosPaginated(int page, int size, string sortBy, string sortDir, string searchQuery)
        {

            ResetError();
            PaginationModelDTO<List<PeriodoDTO>> model = new();
            sortDir = sortDir == "" ? "asc" : sortDir;
            sortBy = sortBy == "undefined" ? string.Empty : sortBy;

            try
            {
                var query = _repositoryPeriodo.GetQueryable().AsQueryable();

                //ordenamiento 
                if (!string.IsNullOrEmpty(sortBy))
                {
                    switch (sortBy)
                    {
                        case "FechaInicio":
                            query = (sortDir == "asc") ? query.OrderBy(t => t.FechaInicio) : query.OrderByDescending(t => t.FechaInicio);
                            break;
                        case "FechaFin":
                            query = (sortDir == "asc") ? query.OrderBy(t => t.FechaFin) : query.OrderByDescending(t => t.FechaFin);
                            break;
                        case "Anio":
                            query = (sortDir == "asc") ? query.OrderBy(t => t.Anio) : query.OrderByDescending(t => t.Anio);
                            break;
                        case "Mes":
                            query = (sortDir == "asc") ? query.OrderBy(t => t.Mes) : query.OrderByDescending(t => t.Mes);
                            break;
                        case "FechaPagoEmpresas":
                            query = (sortDir == "asc") ? query.OrderBy(t => t.FechaPagoEmpresas) : query.OrderByDescending(t => t.FechaPagoEmpresas);
                            break;
                        case "FechaPagoEmbajadores":
                            query = (sortDir == "asc") ? query.OrderBy(t => t.FechaPagoEmbajadores) : query.OrderByDescending(t => t.FechaPagoEmbajadores);
                            break;
                        default:
                            break;
                    }
                }


                if (!string.IsNullOrEmpty(searchQuery))
                {
                   
                    var isNumeric = int.TryParse(searchQuery, out int numeroAño);

                    if (isNumeric)
                        query = query.Where(t => t.Anio == numeroAño);
                    else
                    {
                        string[] meses = ["enero", "febrero", "marzo", "abril", "mayo", "junio", "julio", "agosto", "septiembre", "octubre", "noviembre", "diciembre"];

                        int numeroMes = 0;

                        for (int i = 0; i < meses.Length; i++)
                        {
                            if (meses[i].Contains(searchQuery.ToLower()))
                            {
                                numeroMes = i + 1;
                                break;
                            }
                        }

                        query = query.Where(t => t.Mes == numeroMes);

                    }
                }

                var total = query.Count();

                var items = await query
                    .Skip(page * size)
                    .Take(size)
                    .Select(t => new PeriodoDTO
                    {
                        Anio = t.Anio,
                        Mes = t.Mes,
                        FechaFin = t.FechaFin,
                        FechaInicio = t.FechaInicio,
                        FechaPagoEmbajadores = t.FechaPagoEmbajadores,
                        FechaPagoEmpresas = t.FechaPagoEmpresas,
                        ID = t.ID
                    })
                    .ToListAsync();

                model.Items = items;
                model.Total = total;

                return model;
            }
            catch (Exception ex)
            {
                ExceptionToMessage(ex, "Error al obtener periodos");
                HasError = true;
                return model;
            }
        }

        public async Task<bool> Save(PeriodoDTO dto)
        {
            ResetError();
            if (dto == null)
            {
                LastError = "modelo nulo";
                return false;
            }

            try
            {
                Periodo? model;

                if (dto.ID > 0)
                {
                    model = await _repositoryPeriodo.GetQueryable().FirstOrDefaultAsync(t => t.ID == dto.ID);
                    if (model == null)
                    {
                        LastError = "no se encontro periodo a modificar";
                        return false;
                    }
                }
                else
                {
                    model = new Periodo();
                }

                model.Mes = dto.Mes;
                model.FechaPagoEmpresas = dto.FechaPagoEmpresas;
                model.FechaPagoEmbajadores = dto.FechaPagoEmbajadores;
                model.FechaInicio = dto.FechaInicio;
                model.Anio = dto.Anio;
                model.FechaFin = dto.FechaFin;


                if (model.ID > 0)
                {
                    _repositoryPeriodo.Update(model);
                    await _repositoryPeriodo.SaveAsync();
                }
                else
                {
                    await _repositoryPeriodo.AddAsync(model);
                    await _repositoryPeriodo.SaveAsync();
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
