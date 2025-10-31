using EBDTOs;
using EBEmail;
using EBEntities;
using EBEnums;
using EBRepositories.Interfaces;
using EBServices.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.IO; // <-- para File/Path/AppContext
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;
using System.Globalization;
using System.Security.Cryptography;

namespace EBServices
{
    public class UsuarioService : BaseService, IUsuarioService
    {
        public IRepository<Usuario> _repositoryUsuario;
        public IRepository<UsuarioEmpresa> _repositoryUsuarioEmpresa;
        public IRepository<Empresa> _repositoryEmpresa;
        public IRepository<Roles> _repositoryRoles;
        public IPasswordRecoveryService _passwordRecoveryService;
        public IEmailService _emailService;
        private readonly IConfiguration _config;

        public UsuarioService(
            IRepository<Usuario> repositoryUsuario,
            IRepository<UsuarioEmpresa> repositoryUsuarioEmpresa,
            IRepository<Empresa> repositoryEmpresa,
            IRepository<Roles> repositoryRoles,
            IConfiguration config,
            IEmailService emailService,
            IPasswordRecoveryService passwordRecoveryService)
        {
            _repositoryUsuario = repositoryUsuario;
            _repositoryUsuarioEmpresa = repositoryUsuarioEmpresa;
            _repositoryEmpresa = repositoryEmpresa;
            _repositoryRoles = repositoryRoles;

            _config = config;
            _emailService = emailService;
            _passwordRecoveryService = passwordRecoveryService;
        }

        public async Task<Roles?> GetRolEnum(RolesEnum enumValue)
        {
            ResetError();
            try
            {
                var rol = await _repositoryRoles.GetQueryable()
                    .FirstOrDefaultAsync(t => t.EnumValue == enumValue);
                return rol;
            }
            catch (Exception ex)
            {
                ExceptionToMessage(ex, "Ocurrio un error al registrar usuario");
                HasError = true;
                return null;
            }
        }

        public async Task<bool> ExisteEmail(long userID, string email)
        {
            ResetError();
            try
            {
                var exists = await _repositoryUsuario.GetQueryable()
                    .AnyAsync(t => t.ID != userID && t.Email == email);
                return exists;
            }
            catch (Exception ex)
            {
                ExceptionToMessage(ex, "Ocurrio un error al registrar usuario");
                HasError = true;
                return false;
            }
        }

        public async Task<Usuario?> GetUsuario(long usuarioID)
        {
            ResetError();
            try
            {
                return await _repositoryUsuario.GetQueryable()
                    .Include(t => t.Roles)
                    .FirstOrDefaultAsync(t => t.ID == usuarioID);
            }
            catch (Exception ex)
            {
                ExceptionToMessage(ex, "Ocurrio un error al registrar usuario");
                HasError = true;
                return null;
            }
        }

        public async Task<bool> Update(Usuario usuario)
        {
            ResetError();
            try
            {
                _repositoryUsuario.Update(usuario);
                await _repositoryUsuario.SaveAsync();
                return true;
            }
            catch (Exception ex)
            {
                ExceptionToMessage(ex, "Ocurrio un error al guardar usuario");
                return false;
            }
        }

        public async Task<bool> RegistroUsuario(Usuario usuario)
        {
            ResetError();
            try
            {
                var existeUsuario = await _repositoryUsuario.GetQueryable()
                    .AsNoTracking()
                    .FirstOrDefaultAsync(t => t.Email == usuario.Email);

                if (existeUsuario != null)
                {
                    LastError = "Ya existe un usuario con ese correo electrónico.";
                    return false;
                }

                await _repositoryUsuario.AddAsync(usuario);
                await _repositoryUsuario.SaveAsync();
                return true;
            }
            catch (Exception ex)
            {
                ExceptionToMessage(ex, "Ocurrio un error al registrar usuario");
                return false;
            }
        }

        public async Task<(bool Success, string Token)> Login(string email, string password)
        {
            ResetError();
            try
            {
                var emailLower = (email ?? string.Empty).Trim().ToLowerInvariant();

                var user = await _repositoryUsuario
                    .GetQueryable()
                    .Where(u => !u.Eliminado && u.Email.ToLower() == emailLower)
                    .Select(u => new UserLoginRow
                    {
                        ID = u.ID,
                        Email = u.Email,
                        Password = u.Password,
                        RolesID = u.RolesID
                    })
                    .FirstOrDefaultAsync();

                if (user is null)
                {
                    LastError = "No podemos encontrar una cuenta con esta dirección de email. Reinténtalo o crea una cuenta nueva.";
                    return (false, string.Empty);
                }

                var stored = user.Password ?? string.Empty;
                var passOk = !string.IsNullOrEmpty(stored) && BCrypt.Net.BCrypt.Verify(password, stored);

                if (!passOk)
                {
                    LastError = "La contraseña no es correcta";
                    return (false, string.Empty);
                }

                var token = GenerateJwtToken(user.Email ?? string.Empty, user.ID);
                return (true, token);
            }
            catch (Exception ex)
            {
                ExceptionToMessage(ex, "Ocurrió un error al iniciar sesión");
                return (false, string.Empty);
            }
        }

        private sealed class UserLoginRow
        {
            public long ID { get; set; }
            public string? Email { get; set; }
            public string? Password { get; set; }
            public long? RolesID { get; set; }
        }

        public async Task<(bool Success, string Token)> LoginPanelAdministrador(string email, string password)
        {
            ResetError();
            try
            {
                var usuario = await _repositoryUsuario.GetQueryable()
                    .Include(t => t.Roles)
                    .FirstOrDefaultAsync(t => t.Email == email);

                if (usuario == null)
                {
                    LastError = "No podemos encontrar una cuenta con esta dirección de email. Reinténtalo o crea una cuenta nueva.";
                    return (false, string.Empty);
                }

                if (usuario.Roles == null || usuario.Roles.EnumValue == EBEnums.RolesEnum.Embajador)
                {
                    LastError = "Este panel es solo para administración del sistema, si eres embajador, por favor utiliza la aplicación";
                    return (false, string.Empty);
                }

                if (!BCrypt.Net.BCrypt.Verify(password, usuario.Password))
                {
                    LastError = "La contraseña no es correcta";
                    return (false, string.Empty);
                }

                var rol = usuario.Roles.EnumValue switch
                {
                    EBEnums.RolesEnum.Admin => "Admin",
                    EBEnums.RolesEnum.Socio => "Socio",
                    EBEnums.RolesEnum.Embajador => "Embajador",
                    _ => "Embajador"
                };

                var token = GenerateJwtToken(usuario.Email, usuario.ID, rol);
                return (true, token);
            }
            catch (Exception ex)
            {
                ExceptionToMessage(ex, "Ocurrio un error al iniciar sesión");
                return (false, string.Empty);
            }
        }

        public async Task<bool> ResetPassword(string token, string newPassword)
        {
            ResetError();
            try
            {
                var peticion = await _passwordRecoveryService.GetSolicitudByToken(token);
                if (peticion == null)
                {
                    LastError = "No se encontro una solicitud con tu token.";
                    if (_passwordRecoveryService.HasError) LastError += _passwordRecoveryService.LastError;
                    return false;
                }

                if (DateTime.Now > peticion.FechaVencimiento)
                {
                    await _passwordRecoveryService.DeleteSolicitud(peticion);
                    LastError = "Tu peticion vencio. Solicita un nuevo cambio de contraseña.";
                    return false;
                }

                var user = await _repositoryUsuario.GetByIdAsync(peticion.UsuarioID);
                if (user == null)
                {
                    LastError = "No se encontro tu usuario";
                    return false;
                }

                user.Password = EncryptPassword(newPassword);
                _repositoryUsuario.Update(user);
                await _repositoryUsuario.SaveAsync();

                peticion.Usado = true;
                peticion.Eliminado = true;
                peticion.FechaEliminacion = DateTime.Now;
                await _passwordRecoveryService.Save(peticion);

                return true;
            }
            catch (Exception ex)
            {
                ExceptionToMessage(ex, "Ocurrio un error al cambiar la contraseña. Intentelo de nuevo.");
                return false;
            }
        }

        public async Task<bool> PasswordRecoveryPeticion(string email)
        {
            ResetError();
            try
            {
                // 1) Buscar usuario (normalizado), sin tracking y async
                var emailLower = (email ?? string.Empty).Trim().ToLowerInvariant();

                var usuario = await _repositoryUsuario
                    .GetQueryable()
                    .AsNoTracking()
                    .FirstOrDefaultAsync(u => !u.Eliminado && u.Email.ToLower() == emailLower);

                if (usuario is null)
                {
                    LastError = "No se encontró una cuenta para este email.";
                    return false;
                }

                // 2) Eliminar solicitud activa previa (si existe)
                var activa = await _passwordRecoveryService.AnySolicitudActivaUsuario(usuario.ID);
                if (activa != null)
                    await _passwordRecoveryService.DeleteSolicitud(activa);

                // 3) Generar token único (máx. 5 intentos)
                static string NewToken() =>
                    Guid.NewGuid().ToString("N") + RandomNumberGenerator.GetInt32(int.MaxValue).ToString(CultureInfo.InvariantCulture);

                var token = NewToken();
                var intentos = 5;
                while (intentos-- > 0 && await _passwordRecoveryService.ExistToken(token))
                    token = NewToken();

                // Si, aun así, existe, aborta
                if (await _passwordRecoveryService.ExistToken(token))
                {
                    LastError = "No se pudo generar un token único.";
                    return false;
                }

                // 4) Alta de la solicitud con expiración (UTC)
                var horasStr = _config["RecoveryPasswordConfig:HoursToExpire"] ?? "2";
                var expHours = Convert.ToDouble(horasStr, CultureInfo.InvariantCulture);

                var nowUtc = DateTime.UtcNow;

                var peticion = new PasswordRecovery
                {
                    Token = token,
                    FechaCreacion = nowUtc,
                    FechaVencimiento = nowUtc.AddHours(expHours),
                    Usado = false,
                    UsuarioID = usuario.ID
                };

                if (!await _passwordRecoveryService.Save(peticion))
                {
                    LastError = _passwordRecoveryService.LastError;
                    return false;
                }

                // 5) Construir LINK hacia el ADMIN (fallback si no está PasswordReset:BaseUrl)
                var baseUrl = _config["PasswordReset:BaseUrl"]
                           ?? _config["RecoveryPasswordConfig:PageFrontDomain"];

                if (string.IsNullOrWhiteSpace(baseUrl))
                {
                    LastError = "No está configurada la URL del front para restablecer la contraseña.";
                    return false;
                }

                baseUrl = baseUrl.TrimEnd('/');
                // Tu Admin enruta así: /password-reset/:token
                var link = $"{baseUrl}/password/reset/{token}";


                // 6) Cargar plantilla desde el bin (copiada por el .csproj)
                var templatePath = Path.Combine(AppContext.BaseDirectory, "Data", "Templates", "PasswordRecoveryEmailTemplate.html");

                string html = File.Exists(templatePath)
                    ? await File.ReadAllTextAsync(templatePath)
                    : DefaultTemplate();

                // 7) Reemplazar placeholders EXACTOS de tu HTML
                html = html
                    .Replace("{{RecoveryURL}}", link)
                    .Replace("{{recoveryUrl}}", link)
                    .Replace("{{ExpireHours}}", expHours.ToString("0", CultureInfo.InvariantCulture))
                    .Replace("{{expireHours}}", expHours.ToString("0", CultureInfo.InvariantCulture));


                // 8) Enviar correo
                var mail = await _emailService.SendEmailAsync(
                    usuario.Email,
                    "Recupera tu contraseña",
                    html,
                    "Embassy"
                );

                if (!mail.Success)
                {
                    LastError = mail.ErrorMessage ?? "No se pudo enviar el correo.";
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                ExceptionToMessage(ex, "Ocurrió un error al recuperar contraseña");
                return false;
            }
        }


        public async Task<PaginationModelDTO<List<UsuarioCatalogoDTO>>> GetUsuariosPaginated(
            int page, int size, string sortBy, string sortDir, string searchQuery, int? rolesId = null)
        {
            ResetError();
            var model = new PaginationModelDTO<List<UsuarioCatalogoDTO>>();
            sortDir = string.IsNullOrWhiteSpace(sortDir) ? "asc" : sortDir.ToLowerInvariant();
            sortBy = sortBy == "undefined" ? string.Empty : (sortBy ?? string.Empty);

            try
            {
                var query = _repositoryUsuario
                    .GetQueryable()
                    .Include(t => t.Roles)
                    .Where(t => !t.Eliminado)
                    .AsQueryable();

                if (rolesId.HasValue)
                    query = query.Where(t => t.RolesID == rolesId.Value);

                if (!string.IsNullOrWhiteSpace(searchQuery))
                {
                    var sq = searchQuery.Trim();
                    query = query.Where(t =>
                           ((t.Nombres + " " + t.Apellidos).Contains(sq))
                        || ((t.Roles != null ? t.Roles.Nombre : "").Contains(sq))
                        || t.Nombres.Contains(sq)
                        || t.Apellidos.Contains(sq)
                        || t.Email.Contains(sq)
                        || t.Celular.Contains(sq));
                }

                if (!string.IsNullOrEmpty(sortBy))
                {
                    query = sortBy switch
                    {
                        "NombreCompleto" => (sortDir == "asc")
                            ? query.OrderBy(t => t.Nombres + " " + t.Apellidos)
                            : query.OrderByDescending(t => t.Nombres + " " + t.Apellidos),
                        "Nombres" => (sortDir == "asc") ? query.OrderBy(t => t.Nombres) : query.OrderByDescending(t => t.Nombres),
                        "Apellidos" => (sortDir == "asc") ? query.OrderBy(t => t.Apellidos) : query.OrderByDescending(t => t.Apellidos),
                        "Email" => (sortDir == "asc") ? query.OrderBy(t => t.Email) : query.OrderByDescending(t => t.Email),
                        "Celular" => (sortDir == "asc") ? query.OrderBy(t => t.Celular) : query.OrderByDescending(t => t.Celular),
                        "Rol" => (sortDir == "asc")
                            ? query.OrderBy(t => t.Roles != null ? t.Roles.Nombre : "")
                            : query.OrderByDescending(t => t.Roles != null ? t.Roles.Nombre : ""),
                        _ => query
                    };
                }
                else
                {
                    query = query.OrderBy(t => t.ID);
                }

                var total = await query.CountAsync();

                var items = await query
                    .Skip(page * size)
                    .Take(size)
                    .Select(t => new UsuarioCatalogoDTO
                    {
                        ID = t.ID,
                        Nombres = t.Nombres,
                        Apellidos = t.Apellidos,
                        NombreCompleto = t.Nombres + " " + t.Apellidos,
                        Celular = t.Celular,
                        Email = t.Email,
                        Rol = t.Roles == null ? "" : t.Roles.Nombre
                    })
                    .ToListAsync();

                model.Items = items;
                model.Total = total;

                return model;
            }
            catch (Exception ex)
            {
                ExceptionToMessage(ex, "Error al obtener usuarios");
                HasError = true;
                return model;
            }
        }

        public async Task<List<UsuarioCatalogoDTO>?> GetUsuarioBusqueda(string searchQuery)
        {
            try
            {
                var query = _repositoryUsuario.GetQueryable().Where(t => !t.Eliminado).AsQueryable();

                if (!string.IsNullOrEmpty(searchQuery))
                {
                    query = query.Where(t =>
                        (t.Nombres + " " + t.Apellidos).Contains(searchQuery) ||
                        t.Nombres.Contains(searchQuery) ||
                        t.Apellidos.Contains(searchQuery));
                }

                var items = await query
                    .Take(30)
                    .Select(t => new UsuarioCatalogoDTO
                    {
                        ID = t.ID,
                        Nombres = t.Nombres,
                        Apellidos = t.Apellidos,
                        NombreCompleto = t.Nombres + " " + t.Apellidos,
                        Celular = t.Celular,
                        Email = t.Email
                    })
                    .ToListAsync();

                return items;
            }
            catch (Exception ex)
            {
                ExceptionToMessage(ex, "Error al obtener usuarios");
                HasError = true;
                return null;
            }
        }

        public async Task<UsuarioEditDTO?> GetUsuarioByID(long id)
        {
            try
            {
                var userData = await (
                    from u in _repositoryUsuario.GetQueryable()
                    where u.ID == id
                    join ro in _repositoryRoles.GetQueryable() on u.RolesID equals ro.ID into rolesJoin
                    from rol in rolesJoin.DefaultIfEmpty()
                    join p in _repositoryUsuario.GetQueryable() on u.UsuarioParent equals p.ID into parentJoin
                    from parent in parentJoin.DefaultIfEmpty()
                    join eu in _repositoryUsuarioEmpresa.GetQueryable() on u.ID equals eu.UsuarioID into empresaUsuarioJoin
                    from empresaUsuario in empresaUsuarioJoin.DefaultIfEmpty()
                    join e in _repositoryEmpresa.GetQueryable() on empresaUsuario.EmpresaID equals e.ID into empresaJoin
                    from empresa in empresaJoin.DefaultIfEmpty()
                    select new UsuarioEditDTO
                    {
                        ID = u.ID,
                        Nombres = u.Nombres,
                        Apellidos = u.Apellidos,
                        Email = u.Email,
                        Celular = u.Celular,
                        CatalogoPaisID = u.CatalogoPaisID,
                        CatalogoEstadoID = u.CatalogoEstadoID,
                        EstadoTexto = u.EstadoTexto,
                        Ciudad = u.Ciudad,
                        FuenteOrigenID = u.FuenteOrigenID,
                        UsuarioParentID = u.UsuarioParent,
                        UsuarioParentNombreCompleto = parent != null ? parent.Nombres + " " + parent.Apellidos : null,
                        CodigoInvitacion = u.CodigoInvitacion,
                        RolesID = u.RolesID,
                        RolesEnum = rol != null ? rol.EnumValue : null,
                        GrupoID = u.GrupoID,
                        EmpresaID = empresa != null ? empresa.ID : (long?)null
                    }
                ).FirstOrDefaultAsync();

                return userData;
            }
            catch (Exception ex)
            {
                ExceptionToMessage(ex, "Error al obtener usuario");
                HasError = true;
                return null;
            }
        }

        public async Task<bool> Save(Usuario usuario, long? empresaID)
        {
            ResetError();
            try
            {
                using var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);

                if (usuario.ID > 0) _repositoryUsuario.Update(usuario);
                else await _repositoryUsuario.AddAsync(usuario);
                await _repositoryUsuario.SaveAsync();

                var rel = await _repositoryUsuarioEmpresa.GetQueryable()
                    .FirstOrDefaultAsync(t => t.UsuarioID == usuario.ID);

                if (empresaID != null && empresaID > 0)
                {
                    var agregar = false;

                    if (rel != null)
                    {
                        if (rel.EmpresaID != empresaID)
                        {
                            _repositoryUsuarioEmpresa.Delete(rel);
                            agregar = true;
                        }
                    }
                    else agregar = true;

                    if (agregar)
                    {
                        var ue = new UsuarioEmpresa { EmpresaID = empresaID.Value, UsuarioID = usuario.ID };
                        await _repositoryUsuarioEmpresa.AddAsync(ue);
                        await _repositoryUsuarioEmpresa.SaveAsync();
                    }
                }
                else if (rel != null)
                {
                    _repositoryUsuarioEmpresa.Delete(rel);
                    await _repositoryUsuarioEmpresa.SaveAsync();
                }

                scope.Complete();
                return true;
            }
            catch (Exception ex)
            {
                ExceptionToMessage(ex, "Ocurrio un error al guardar usuario");
                return false;
            }
        }

        public async Task<Empresa?> GetEmpresaByUsuario(long usuarioID)
        {
            ResetError();
            try
            {
                var rel = await _repositoryUsuarioEmpresa.GetQueryable()
                    .AsNoTracking()
                    .FirstOrDefaultAsync(t => t.UsuarioID == usuarioID);

                Empresa? empresa = null;

                if (rel != null)
                {
                    empresa = await _repositoryEmpresa.GetQueryable()
                        .AsNoTracking()
                        .FirstOrDefaultAsync(e => e.ID == rel.EmpresaID && !e.Eliminado);
                }

                if (empresa == null)
                {
                    empresa = await _repositoryEmpresa.GetQueryable()
                        .AsNoTracking()
                        .FirstOrDefaultAsync(e => e.embajadorId == (int)usuarioID && !e.Eliminado);
                }

                return empresa;
            }
            catch (Exception ex)
            {
                ExceptionToMessage(ex, "Ocurrió un error al obtener empresa de usuario");
                HasError = true;
                return null;
            }
        }

        private string GenerateJwtToken(string username, long id, string rol = "Embajador")
        {
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, id.ToString()),
                new Claim(ClaimTypes.Name, username),
                new Claim(ClaimTypes.Role, rol),
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var expiration = DateTime.UtcNow.AddMinutes(Convert.ToDouble(_config["Jwt:ExpirationMinutes"]));

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                claims: claims,
                expires: expiration,
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public string EncryptPassword(string password)
            => BCrypt.Net.BCrypt.HashPassword(password);

        private string DefaultTemplate() => @"
<!DOCTYPE html>
<html lang='es'><head><meta charset='UTF-8'><title>Recuperar contraseña</title></head>
<body style='font-family:Arial,sans-serif;background:#f6f7fb;padding:24px;color:#333'>
  <div style='max-width:600px;margin:auto;background:#fff;padding:30px;border-radius:8px;box-shadow:0 4px 16px rgba(0,0,0,.08)'>
    <h2 style='color:#0b5;margin-top:0;'>Recuperación de contraseña</h2>
    <p>Recibimos una solicitud para restablecer tu contraseña.</p>
    <p style='text-align:center;margin:28px 0;'>
      <a href='{{RecoveryURL}}' style='background:#2563eb;color:#fff;padding:12px 22px;border-radius:8px;text-decoration:none;display:inline-block'>
        Restablecer contraseña
      </a>
    </p>
    <p style='color:#666;font-size:12px'>El enlace expira en {{ExpireHours}} horas.</p>
  </div>
</body></html>";

    }
}
