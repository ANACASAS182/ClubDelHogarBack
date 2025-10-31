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
using System.Transactions;

namespace EBServices
{
    public class SeguimientoReferidoService :BaseService, ISeguimientoReferidoService
    {
        public IRepository<SeguimientoReferido> _repositorySeguimientoReferido;

        public SeguimientoReferidoService(IRepository<SeguimientoReferido> repositorySeguimientoReferido)
        {
            _repositorySeguimientoReferido = repositorySeguimientoReferido;
        }

        public async Task<List<SeguimientoReferido>?> GetSeguimietosReferido(long referidoID)
        {
            try
            {
                return await _repositorySeguimientoReferido.GetQueryable().Where(t => !t.Eliminado && t.ReferidoID == referidoID).OrderByDescending(t=>t.FechaSeguimiento).ToListAsync();
            }
            catch (Exception ex)
            {
                HasError = false;
                ExceptionToMessage(ex, "Ocurrio un problema al obtener seguimientos");
                return null;
            }
        }

        public async Task<bool> Save(SeguimientoReferido model)
        {
            ResetError();
            if (model == null)
            {
                LastError = "modelo nulo";
                return false;
            }

            try
            {
                model.FechaSeguimiento = DateTime.Now;

                await _repositorySeguimientoReferido.AddAsync(model);
                await _repositorySeguimientoReferido.SaveAsync();

                return true;
            }
            catch (Exception ex)
            {
                ExceptionToMessage(ex, "Ocurrio un problema al guardar producto. ");
                HasError = true;
                return false;
            }
        }


    }
}
