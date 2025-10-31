using EBDTOs;
using EBEntities;
using EBEnums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EBServices.Interfaces
{
    public interface IReferidoService : IBaseService
    {
        Task<PaginationModelDTO<List<ReferidoDTO>>> GetReferidosPaginated(long usuarioID, int page, int size, string sortBy, string sortDir, string searchQuery);
        Task<EstatusReferencia?> GetEstatusReferenciaByEnum(EstatusReferenciaEnum enume);
        Task<bool> Save(Referido model);
        Task<List<ReferidoDTO>?> GetReferidosByUsuario(long usuarioID);
        Task<PaginationModelDTO<List<ReferidoCatalogoDTO>>> GetAllReferidosPaginated(int page, int size, string sortBy, string sortDir, string searchQuery, long? grupoId, long? empresaId, EstatusReferenciaEnum? estatusEnum, long? usuarioID);
        Task<PaginationModelDTO<List<ReferidoCatalogoDTO>>> GetReferidosByEmpresaPaginated(long empresaID, int page, int size, string sortBy, string sortDir, string searchQuery, EstatusReferenciaEnum? estatusEnum, long? usuarioID);      
        Task<ReferidoDTO?> GetReferidoByID(long id);
        Task<string> LeerQRBase64(string codigo, string folderDefault = "C:\\tmp\\qr\\");
        Task<bool> UpdateEstatus(EstatusReferidoDTO model);
    }
}
