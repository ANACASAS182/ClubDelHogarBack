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
    public class FuenteOrigenService : BaseService, IFuenteOrigenService
    {
        public IRepository<FuenteOrigen> _repositoryFuenteOrigen;

        public FuenteOrigenService(IRepository<FuenteOrigen> repositoryFuenteOrigen)
        {
            _repositoryFuenteOrigen = repositoryFuenteOrigen;
        }

        public async Task<List<FuenteOrigenDTO>?> GetFuentesDeOrigen()
        {
            try
            {
                return await _repositoryFuenteOrigen
                                .GetQueryable()
                                .Where(t => !t.Eliminado)
                                .Select(t => new FuenteOrigenDTO
                                {
                                    ID = t.ID,
                                    Nombre = t.Nombre
                                })
                                .ToListAsync();
            }
            catch (Exception ex)
            {
                ExceptionToMessage(ex);
                this.HasError = true;
                return null;
            }
        }
    }
}
