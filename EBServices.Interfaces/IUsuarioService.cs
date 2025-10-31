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
    public interface IUsuarioService : IBaseService
    {
        Task<bool> ExisteEmail(long userID, string email);
        Task<bool> Update(Usuario usuario);
        Task<Usuario?> GetUsuario(long usuarioID);
        Task<bool> RegistroUsuario(Usuario usuario);
        Task<(bool Success, string Token)> Login(string email, string password);
        string EncryptPassword(string password);
        Task<bool> ResetPassword(string token, string newPassword);
        Task<bool> PasswordRecoveryPeticion(string email);
        Task<(bool Success, string Token)> LoginPanelAdministrador(string email, string password);
        Task<PaginationModelDTO<List<UsuarioCatalogoDTO>>> GetUsuariosPaginated(int page, int size, string sortBy, string sortDir, string searchQuery, int? rolesId = null);
        Task<List<UsuarioCatalogoDTO>?> GetUsuarioBusqueda(string searchQuery);
        Task<UsuarioEditDTO?> GetUsuarioByID(long id);
        Task<Roles?> GetRolEnum(RolesEnum enumValue);
        Task<bool> Save(Usuario usuario, long? empresaID);
        Task<Empresa?> GetEmpresaByUsuario(long usuarioID);
    }
}
