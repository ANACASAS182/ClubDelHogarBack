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
    public class EmpresaGrupoService : BaseService, IEmpresaGrupoService
    {
        public IRepository<EmpresaGrupo> _repositoryEmpresaGrupo;
        public IRepository<Grupo> _repositoryGrupo;

        public EmpresaGrupoService(IRepository<EmpresaGrupo> repositoryEmpresaGrupo, IRepository<Grupo> repositoryGrupo)
        {
            _repositoryEmpresaGrupo = repositoryEmpresaGrupo;
            _repositoryGrupo = repositoryGrupo;
        }

        public async Task<bool> Delete(EmpresaGrupo model)
        {
            
            ResetError();
            if (model == null)
            {
                LastError = "modelo nulo";
                return false;
            }
            try
            {
                var exist = _repositoryEmpresaGrupo.GetQueryable().Where(t => t.EmpresaID == model.EmpresaID && t.GrupoID == model.GrupoID).FirstOrDefault();

                if (exist != null)
                {
                    _repositoryEmpresaGrupo.Delete(model);
                    await _repositoryEmpresaGrupo.SaveAsync();
                }
                return true;
            }
            catch (Exception ex)
            {
                ExceptionToMessage(ex, "Ocurrio un problema al eliminar EmpresaGrupo. ");
                HasError = true;
                return false;
            }

        }

        public async Task<bool> Save(EmpresaGrupo model)
        {
            ResetError();
            if (model == null)
            {
                LastError = "modelo nulo";
                return false;
            }
            try
            {
                var exist = _repositoryEmpresaGrupo.GetQueryable().Where(t => t.EmpresaID == model.EmpresaID && t.GrupoID == model.GrupoID).Any();

                if (exist)
                {
                    HasError = true;
                    LastError = "Este grupo ya esta relacionado con su empresa. ";
                }
                else
                {
                    await _repositoryEmpresaGrupo.AddAsync(model);
                    await _repositoryEmpresaGrupo.SaveAsync();
                }
                return true;
            }
            catch (Exception ex)
            {
                ExceptionToMessage(ex, "Ocurrio un problema al guardar EmpresaGrupo. ");
                HasError = true;
                return false;
            }
        }

        public async Task<List<EmpresaGrupoDTO>?> GetAllGruposByEmpresa(long empresaID)
        {
            ResetError();
            try
            {
                var empresaGrupos = await (
                                            from eg in _repositoryEmpresaGrupo.GetQueryable()
                                            join g in _repositoryGrupo.GetQueryable() on eg.GrupoID equals g.ID
                                            where eg.EmpresaID == empresaID
                                            select new EmpresaGrupoDTO
                                            {
                                                ID = eg.ID,
                                                EmpresaID = eg.EmpresaID,
                                                GrupoID = eg.GrupoID,
                                                NombreGrupo = g.Nombre
                                            }
                                        ).ToListAsync();
                return empresaGrupos;
            }
            catch (Exception ex)
            {
                ExceptionToMessage(ex, "Error al obtener grupos de empresas");
                HasError = true;
                return null;
            }
        }


    }
}
