using EBDTOs;
using EBEntities;
using EBServices;
using EBServices.Interfaces;
using EmbassyBusinessBack.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Swashbuckle.AspNetCore.Annotations;
using System.ComponentModel;
using System.Data;
using System.Security.Claims;

namespace EmbassyBusinessBack.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class UsuarioController : ControllerBase
    {

        private readonly IUsuarioService _usuarioService;

        private readonly IPasswordRecoveryService _passwordRecoveryService;

        public UsuarioController(
        IUsuarioService usuarioService,
        IPasswordRecoveryService passwordRecoveryService)               // 👈 nuevo
        {
            _usuarioService = usuarioService;
            _passwordRecoveryService = passwordRecoveryService;             // 👈 nuevo
        }


        //Angel Romero
        //En vez de allowAnonymous seria mejor hacer un personalizado, para que pida un api key. Por falta de tiempo se deja pendiente.

        [HttpPost("RegistroUsuario")]
        [SwaggerResponse(200, "Objeto de respuesta", typeof(GenericResponseDTO<bool>))]
        [AllowAnonymous]
        public async Task<IActionResult> RegistroUsuario([FromBody] UsuarioDTO dto)
        {

            Usuario user = new Usuario() {
                Apellidos = dto.Apellidos,
                Celular = dto.Celular,
                Email = dto.Email.ToLower(),
                Nombres = dto.Nombres,
                Password = _usuarioService.EncryptPassword(dto.Password!),
                CatalogoEstadoID = dto.CatalogoEstadoID,
                CatalogoPaisID = dto.CatalogoPaisID,
                Ciudad = dto.Ciudad,
                EstadoTexto = dto.EstadoTexto,
                FuenteOrigenID = dto.FuenteOrigenID!.Value,
                UsuarioParent = dto.UsuarioParent!.Value,
                CodigoInvitacion = dto.CodigoInvitacion,
                RolesID= 3
            };

            try
            {
                var result = await _usuarioService.RegistroUsuario(user);

                if (result)
                    return Ok(GenericResponseDTO<bool>.Ok(true, "Registro exitoso"));
                else
                    return BadRequest(GenericResponseDTO<bool>.Fail(_usuarioService.LastError));
            }
            catch (Exception ex)
            {
                return BadRequest(GenericResponseDTO<bool>.Fail(ex.InnerException?.Message ?? ex.Message));
            }

        }

        //Agregado por Alexander

        /* [HttpPost("RegistroUsuarioCodigoInvitacion")]
         [SwaggerResponse(200, "Objeto de respuesta", typeof(GenericResponseDTO<bool>))]
         [AllowAnonymous]
         public async Task<IActionResult> RegistroUsuarioCodigoInvitacion([FromBody] UsuarioRegistrarBasicoDTO dto)
         {
             // Validaciones mínimas
             if (dto == null ||
                 string.IsNullOrWhiteSpace(dto.email) ||
                 string.IsNullOrWhiteSpace(dto.password) ||
                 string.IsNullOrWhiteSpace(dto.codigoInvitacion))
             {
                 return BadRequest(GenericResponseDTO<bool>.Fail("Datos incompletos."));
             }

             var emailLower = SqlEsc(dto.email.Trim().ToLower());
             var codigoInv = SqlEsc(dto.codigoInvitacion.Trim());

             // Validar invitación y obtener parent/grupo
             var dtGrupo = DataAccess.performQuery($@"
                 SELECT TOP(1)
                     ISNULL(U.GrupoID, 0)              AS UsuarioGrupo,
                     ISNULL(E.Grupo, 0)                 AS EmpresaGrupo,
                     ISNULL(I.EmbajadorReferenteID, 0)  AS EmbajadorReferenteID
                 FROM Invitaciones I
                 LEFT JOIN Usuario U          ON U.ID  = I.EmbajadorReferenteID
                 LEFT JOIN UsuarioEmpresa UE  ON UE.UsuarioID = I.EmbajadorReferenteID
                 LEFT JOIN Empresa E          ON E.ID  = UE.EmpresaID
                 WHERE I.CodigoDeInvitacion = '{codigoInv}'
             ");

             if (dtGrupo.Rows.Count == 0)
                 return BadRequest(GenericResponseDTO<bool>.Fail("Código de invitación inválido."));

             var r = dtGrupo.Rows[0];
             int? parentId = Convert.ToInt32(r["EmbajadorReferenteID"]) > 0
                 ? Convert.ToInt32(r["EmbajadorReferenteID"])
                 : (int?)null;

             int usuarioGrupo = Convert.ToInt32(r["UsuarioGrupo"]);
             int empresaGrupo = Convert.ToInt32(r["EmpresaGrupo"]);
             int? grupoId = usuarioGrupo > 0 ? usuarioGrupo
                          : (empresaGrupo > 0 ? empresaGrupo : (int?)null);

             // Email unico
             var dtExists = DataAccess.performQuery($@"
                 SELECT TOP(1) 1 FROM Usuario
                 WHERE LOWER(Email) = '{emailLower}' AND ISNULL(Eliminado,0)=0
             ");
             if (dtExists.Rows.Count > 0)
                 return Conflict(GenericResponseDTO<bool>.Fail("El correo ya está registrado."));

             // Construir usuario con campos opcionales en NULL
             var user = new Usuario
             {
                 Nombres = null,
                 Apellidos = null,
                 Celular = null,
                 Email = dto.email.Trim().ToLower(),
                 Password = _usuarioService.EncryptPassword(dto.password),

                 CatalogoPaisID = null,
                 CatalogoEstadoID = null,
                 Ciudad = null,
                 EstadoTexto = null,

                 FuenteOrigenID = 2,
                 UsuarioParent = parentId,
                 CodigoInvitacion = dto.codigoInvitacion,
                 RolesID = 3,
                 mostrarOnboarding = true,
                 GrupoID = grupoId
             };

             try
             {
                 var ok = await _usuarioService.RegistroUsuario(user);
                 if (!ok) return BadRequest(GenericResponseDTO<bool>.Fail(_usuarioService.LastError));
             }
             catch (Exception ex)
             {
                 var root = ex.GetBaseException()?.Message ?? ex.Message;
                 return BadRequest(GenericResponseDTO<bool>.Fail("DB: " + root));
             }

             return Ok(GenericResponseDTO<bool>.Ok(true, "Registro exitoso"));
         }

         private static string SqlEsc(string? s) => (s ?? string.Empty).Replace("'", "''"); */




        //Inician los cambios hechos por Abraham
        // Modificado por Alexander


        [HttpPost("RegistroUsuarioCodigoInvitacion")]
        [AllowAnonymous]
        public async Task<IActionResult> RegistroUsuarioCodigoInvitacion([FromBody] UsuarioRegistrarBasicoDTO dto)
        {
            try
            {
                if (dto == null || string.IsNullOrWhiteSpace(dto.email) ||
                    string.IsNullOrWhiteSpace(dto.password) || string.IsNullOrWhiteSpace(dto.codigoInvitacion))
                {
                    return BadRequest(GenericResponseDTO<bool>.Fail("Datos incompletos."));
                }

                // --- Buscar grupo y referente a partir de la invitación
                var safeCodigo = (dto.codigoInvitacion ?? string.Empty).Replace("'", "''");
                var dtGrupo = DataAccess.performQuery($@"
            SELECT TOP(1)
                   ISNULL(U.GrupoID, 0)                   AS UsuarioGrupo,
                   ISNULL(E.Grupo, 0)                     AS EmpresaGrupo,
                   ISNULL(I.EmbajadorReferenteID, 0)      AS EmbajadorReferenteID
            FROM Invitaciones I
            LEFT JOIN Usuario        U  ON U.ID  = I.EmbajadorReferenteID
            LEFT JOIN UsuarioEmpresa UE ON UE.UsuarioID = I.EmbajadorReferenteID
            LEFT JOIN Empresa        E  ON E.ID  = UE.EmpresaID
            WHERE I.CodigoDeInvitacion = '{safeCodigo}';");

                int? grupoId = null;
                int? parentId = null;

                if (dtGrupo.Rows.Count > 0)
                {
                    var row = dtGrupo.Rows[0];
                    var usuarioGrupo = Convert.ToInt32(row["UsuarioGrupo"]);
                    var empresaGrupo = Convert.ToInt32(row["EmpresaGrupo"]);
                    var embajadorRef = Convert.ToInt32(row["EmbajadorReferenteID"]);

                    grupoId = usuarioGrupo > 0 ? usuarioGrupo : (empresaGrupo > 0 ? empresaGrupo : (int?)null);
                    parentId = embajadorRef > 0 ? embajadorRef : (int?)null;
                }

                // --- Construir el usuario SIN ceros en FKs (usa NULL cuando no haya valor)
                var user = new Usuario
                {
                    Email = dto.email.Trim().ToLower(),
                    Password = _usuarioService.EncryptPassword(dto.password),

                    // Campos opcionales: NULL mejor que ""/0 si tu esquema lo permite
                    Nombres = null,
                    Apellidos = null,
                    Celular = null,
                    Ciudad = null,
                    EstadoTexto = null,
                    CatalogoPaisID = null,          // ⚠ antes 0 -> puede romper FK
                    CatalogoEstadoID = null,          // ⚠ antes 0 -> puede romper FK

                    FuenteOrigenID = 2,             // este sí parece requerido y válido
                    UsuarioParent = parentId,      // NULL si no hay referente
                    CodigoInvitacion = (dto.codigoInvitacion.Length > 8)
                                        ? dto.codigoInvitacion.Substring(0, 8) // por si en BD cabe 8
                                        : dto.codigoInvitacion,
                    RolesID = 3,
                    mostrarOnboarding = true,
                    GrupoID = grupoId        // NULL si no hay grupo válido
                };

                var ok = await _usuarioService.RegistroUsuario(user);
                if (!ok)
                    return BadRequest(GenericResponseDTO<bool>.Fail(_usuarioService.LastError ?? "Registro rechazado."));

                return Ok(GenericResponseDTO<bool>.Ok(true, "Registro exitoso"));
            }
            catch (DbUpdateException ex)
            {
                // Muestra la causa real (FK, NOT NULL, truncado, etc.)
                return BadRequest(GenericResponseDTO<bool>.Fail("DB: " + ex.GetBaseException().Message));
            }
            catch (SqlException ex)
            {
                return BadRequest(GenericResponseDTO<bool>.Fail("SQL: " + ex.Message));
            }
            catch (Exception ex)
            {
                return BadRequest(GenericResponseDTO<bool>.Fail("Unhandled: " + ex.Message));
            }
        }



        [HttpGet("getEmbajadorPorCorreo")]
        [AllowAnonymous]
        public async Task<IActionResult> getEmbajadorPorCorreo(string correo)
        {

            UsuarioBasico u = new UsuarioBasico();

            string q = $"select id, Nombres + ' ' + Apellidos as nombre, email from Usuario where email = '{correo}';";
            q = q.Split(";")[0];

            u = DataAccess.fromQueryObject<UsuarioBasico>(q);

            return Ok(u);

        }

        [HttpPost("postOnboardingA")]
        [Authorize]
        public IActionResult PostOnboardingA([FromBody] UsuarioDTO dto)
        {
            if (!long.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var usuarioID))
                return Unauthorized(GenericResponseDTO<string>.Fail("No autorizado"));

            var nombres = (dto.Nombres ?? "").Replace("'", "''");
            var apellidos = (dto.Apellidos ?? "").Replace("'", "''");
            var celular = (dto.Celular ?? "").Replace("'", "''");

            DataAccess.performQuery($@"
        UPDATE Usuario
        SET Nombres   = '{nombres}',
            Apellidos = '{apellidos}',
            Celular   = '{celular}'
        WHERE ID = {usuarioID}");
            return Ok(true);
        }

        [HttpPost("postOnboardingB")]
        [Authorize]
        public IActionResult PostOnboardingB([FromBody] UsuarioDTO dto)
        {
            if (!long.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var usuarioID))
                return Unauthorized(GenericResponseDTO<string>.Fail("No autorizado"));

            var ciudad = (dto.Ciudad ?? "").Replace("'", "''");
            var estadoTexto = (dto.EstadoTexto ?? "").Replace("'", "''");
            var estadoIdSql = dto.CatalogoEstadoID.HasValue ? dto.CatalogoEstadoID.Value.ToString() : "NULL";

            DataAccess.performQuery($@"
        UPDATE Usuario
        SET CatalogoPaisId    = 151,
            CatalogoEstadoId  = {estadoIdSql},
            Ciudad            = '{ciudad}',
            EstadoTexto       = '{estadoTexto}',
            GrupoID           = 2,              -- valor fijo
            mostrarOnboarding = 0
        WHERE ID = {usuarioID}");

            return Ok(true);
        }



        /// TERMINAN LOS CAMBIOS HECHOS POR ABRAHAM

        [HttpPost("Login")]
        [SwaggerResponse(200, "Objeto de respuesta", typeof(GenericResponseDTO<string>))]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] LoginUsuarioDTO dto)
        {
            if (dto is null || string.IsNullOrWhiteSpace(dto.Email) || string.IsNullOrWhiteSpace(dto.Password))
                return BadRequest(GenericResponseDTO<string>.Fail("Credenciales incompletas."));

            var email = dto.Email.Trim().ToLowerInvariant();
            var pass = dto.Password;

            var (result, token) = await _usuarioService.Login(email, pass);

            if (result)
                return Ok(GenericResponseDTO<string>.Ok(token, "Login Exitoso"));

            return BadRequest(GenericResponseDTO<string>.Fail(_usuarioService.LastError));
        }

        [HttpPost("LoginPanelAdministrador")]
        [SwaggerResponse(200, "Objeto de respuesta", typeof(GenericResponseDTO<string>))]
        [AllowAnonymous]
        public async Task<IActionResult> LoginPanelAdministrador([FromBody] LoginUsuarioDTO dto)
        {

            var (result, token) = await _usuarioService.LoginPanelAdministrador(dto.Email, dto.Password);

            if (result)
                return Ok(GenericResponseDTO<string>.Ok(token, "Login Exitoso"));
            else
                return BadRequest(GenericResponseDTO<string>.Fail(_usuarioService.LastError));
        }

        [HttpPost("PasswordRecovery")]
        [SwaggerResponse(200, "Objeto de respuesta", typeof(GenericResponseDTO<bool>))]
        [AllowAnonymous]
        public async Task<IActionResult> PasswordRecovery([FromBody] PasswordRecoveryDTO dto)
        {

            var result = await _usuarioService.PasswordRecoveryPeticion(dto.Email);

            if (result)
                return Ok(GenericResponseDTO<bool>.Ok(true));
            else
                return BadRequest(GenericResponseDTO<bool>.Fail(_usuarioService.LastError));
        }

        [HttpPost("PasswordReset")]
        [SwaggerResponse(200, "Objeto de respuesta", typeof(GenericResponseDTO<bool>))]
        [AllowAnonymous]
        public async Task<IActionResult> PasswordReset([FromBody] PasswordResetDTO dto)
        {

            var result = await _usuarioService.ResetPassword(dto.Token, dto.NewPassword);

            if (result)
                return Ok(GenericResponseDTO<bool>.Ok(result));
            else
                return BadRequest(GenericResponseDTO<bool>.Fail(_usuarioService.LastError));
        }

        [HttpGet("GetCelulaLocal")]
        [AllowAnonymous]
        public async Task<IActionResult> GetCelulaLocal(long UsuarioID)
        {
            CelulaDTO c = new CelulaDTO();

            c.Yo = DataAccess.fromQueryObject<UsuarioDTO>($"select * from usuario where ID = '{UsuarioID}'");
            if (c.Yo != null) {
                if (c.Yo.UsuarioParent != null) { 
                    c.Padre = DataAccess.fromQueryObject<UsuarioDTO>($"select * from usuario where ID = '{c.Yo.UsuarioParent}'");
                }

                c.Hijos = DataAccess.fromQueryListOf<UsuarioDTO>($"select * from usuario where UsuarioParent = '{UsuarioID}'");
            }
            

            return Ok(c);
        }

        [HttpGet("GetUsuarioLogeado")]
        [SwaggerResponse(200, "Objeto de respuesta", typeof(Usuario))]
        public async Task<IActionResult> GetUsuarioLogeado()
        {
            if (!long.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var UsuarioID))
            {
                return BadRequest(GenericResponseDTO<string>.Fail("No se pudo obtener el ID de usuario"));
            }

            var result = await _usuarioService.GetUsuario(UsuarioID);

            if (_usuarioService.HasError == false)
            {
                if (result == null)
                {
                    return NotFound(GenericResponseDTO<string>.Fail("No se encontro usuario."));
                }
                return Ok(GenericResponseDTO<Usuario>.Ok(result, "Consulta exitosa"));
            }
            else
            {
                return BadRequest(GenericResponseDTO<string>.Fail(_usuarioService.LastError));
            }
        }

        [HttpPost("ActualizarUsuario")]
        [SwaggerResponse(200, "Objeto de respuesta", typeof(GenericResponseDTO<bool>))]
        public async Task<IActionResult> ActualizarUsuario([FromBody] UsuarioDTO dto)
        {

            if (!long.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var UsuarioID))
            {
                return BadRequest(GenericResponseDTO<string>.Fail("No se pudo obtener el ID de usuario"));
            }

            var usuarioModel = await _usuarioService.GetUsuario(UsuarioID);

            if(usuarioModel == null)
            {
                return BadRequest(GenericResponseDTO<string>.Fail("No se pudo obtener el usuario"));
            }

            var existeUsuarioEmail = await _usuarioService.ExisteEmail(UsuarioID,dto.Email);

            if(existeUsuarioEmail == true)
            {
                return BadRequest(GenericResponseDTO<string>.Fail("Ya existe otro usuario con ese correo electrónico."));
            }
            else
            {
                if (_usuarioService.HasError)
                {
                    return BadRequest(GenericResponseDTO<string>.Fail("Ocurrio un error al verificar existencia de correo electronico " + _usuarioService.LastError ));
                }
            }
            
            usuarioModel.Apellidos = dto.Apellidos;
            usuarioModel.Nombres = dto.Nombres;
            usuarioModel.Celular = dto.Celular;
            usuarioModel.Ciudad =   dto.Ciudad;
            usuarioModel.CatalogoEstadoID = dto.CatalogoEstadoID;
            usuarioModel.EstadoTexto = dto.EstadoTexto;
            usuarioModel.Email = dto.Email;
            usuarioModel.UsuarioParent = dto.UsuarioParent;
            usuarioModel.CodigoInvitacion = dto.CodigoInvitacion;

            var result = await _usuarioService.Update(usuarioModel);

            if (result)
                return Ok(GenericResponseDTO<bool>.Ok(result));
            else
                return BadRequest(GenericResponseDTO<bool>.Fail(_usuarioService.LastError));
        }

        [HttpGet("GetUsuariosPaginated")]
        [SwaggerResponse(200, "Objeto de respuesta", typeof(PaginationModelDTO<List<UsuarioCatalogoDTO>>))]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetUsuariosPaginated(
            [FromQuery] int page,
            [FromQuery] int size,
            [FromQuery] string sortBy = "",
            [FromQuery] string sortDir = "",
            [FromQuery] string searchQuery = "",
            [FromQuery] int? rolesId = null   // nuevo
)
        {
            var result = await _usuarioService.GetUsuariosPaginated(page, size, sortBy, sortDir, searchQuery, rolesId);
            if (_usuarioService.HasError == false)
            {
                if (result.Items == null || result.Total == 0)
                    return NotFound(GenericResponseDTO<string>.Fail("No se encontraron registros en catalogo de grupos.", false));
                return Ok(GenericResponseDTO<PaginationModelDTO<List<UsuarioCatalogoDTO>>>.Ok(result, "Consulta exitosa"));
            }
            return BadRequest(GenericResponseDTO<string>.Fail(_usuarioService.LastError));
        }


        [HttpGet("GetUserBusqueda")]
        [SwaggerResponse(200, "Objeto de respuesta", typeof(List<UsuarioCatalogoDTO>))]
        public async Task<IActionResult> GetUserBusqueda(string searchParam = "")
        {

            var result = await _usuarioService.GetUsuarioBusqueda(searchParam);

            if (_usuarioService.HasError == false)
            {
                if (result == null)
                {
                    return NotFound(GenericResponseDTO<string>.Fail("No se encontro usuario."));
                }
                return Ok(GenericResponseDTO<List<UsuarioCatalogoDTO>>.Ok(result, "Consulta exitosa"));
            }
            else
            {
                return BadRequest(GenericResponseDTO<string>.Fail(_usuarioService.LastError));
            }
        }

        [HttpGet("GetUserByID")]
        [SwaggerResponse(200, "Objeto de respuesta", typeof(UsuarioEditDTO))]
        public async Task<IActionResult> GetUserByID(long usuarioID )
        {

            var result = await _usuarioService.GetUsuarioByID(usuarioID);

            if (_usuarioService.HasError == false)
            {
                if (result == null)
                {
                    return NotFound(GenericResponseDTO<string>.Fail("No se encontro usuario."));
                }
                return Ok(GenericResponseDTO<UsuarioEditDTO>.Ok(result, "Consulta exitosa"));
            }
            else
            {
                return BadRequest(GenericResponseDTO<string>.Fail(_usuarioService.LastError));
            }
        }

        [HttpPost("Save")]
        [SwaggerResponse(200, "Objeto de respuesta", typeof(GenericResponseDTO<bool>))]
        public async Task<IActionResult> Save([FromBody] UsuarioEditDTO dto)
        {
            var id = dto.ID == null ? 0 : dto.ID.Value;


            var existeUsuarioEmail = await _usuarioService.ExisteEmail(id, dto.Email);

            if (existeUsuarioEmail == true)
            {
                return BadRequest(GenericResponseDTO<string>.Fail("Ya existe otro usuario con ese correo electrónico."));
            }
            else
            {
                if (_usuarioService.HasError)
                {
                    return BadRequest(GenericResponseDTO<string>.Fail("Ocurrio un error al verificar existencia de correo electronico " + _usuarioService.LastError));
                }
            }

            var usuarioModel = await _usuarioService.GetUsuario(id);

            if (id > 0)
            {
                if (usuarioModel == null)
                {
                    return BadRequest(GenericResponseDTO<string>.Fail("No se pudo obtener el usuario"));
                }
            }
          
            if (usuarioModel == null)
            {
                usuarioModel = new Usuario() { 
                    Apellidos = string.Empty,
                    Celular = string.Empty,
                    Email = string.Empty,
                    Nombres = string.Empty,
                    Password = string.Empty,
                };
            }

            var rol = await _usuarioService.GetRolEnum(dto.RolesEnum!.Value);
            if (rol == null)
            {
                return BadRequest(GenericResponseDTO<string>.Fail("No se pudo obtener el rol"));
            }

            usuarioModel.Nombres = dto.Nombres;
            usuarioModel.Apellidos = dto.Apellidos;
            usuarioModel.Celular = dto.Celular;
            usuarioModel.Ciudad = dto.Ciudad;
            usuarioModel.CatalogoEstadoID = dto.CatalogoEstadoID;
            usuarioModel.EstadoTexto = dto.EstadoTexto;
            usuarioModel.Email = dto.Email;
            usuarioModel.UsuarioParent = dto.UsuarioParentID;
            usuarioModel.CatalogoPaisID = dto.CatalogoPaisID;
            usuarioModel.FuenteOrigenID = dto.FuenteOrigenID;
            usuarioModel.GrupoID = dto.GrupoID == 0 ? null : dto.GrupoID;
            usuarioModel.RolesID = rol.ID;

            if (!string.IsNullOrEmpty(dto.Password))
            {
                if(rol.EnumValue == EBEnums.RolesEnum.Admin && id > 0)
                {
                    return BadRequest(GenericResponseDTO<string>.Fail("No se puede cambiar contraseña de un administrador."));
                }
                usuarioModel.Password = _usuarioService.EncryptPassword(dto.Password!);
            }

            var result = await _usuarioService.Save(usuarioModel, dto.EmpresaID);

            if (result)
                return Ok(GenericResponseDTO<bool>.Ok(result));
            else
                return BadRequest(GenericResponseDTO<bool>.Fail(_usuarioService.LastError));
        }


        [HttpGet("GetEmpresaUsuario")]
        [SwaggerResponse(200, "Objeto de respuesta", typeof(Empresa))]
        public async Task<IActionResult> GetEmpresaUsuario(long usuarioID)
        {

            var result = await _usuarioService.GetEmpresaByUsuario(usuarioID);

            if (_usuarioService.HasError == false)
            {
                if (result == null)
                {
                    return NotFound(GenericResponseDTO<string>.Fail("No se encontro empresa."));
                }
                return Ok(GenericResponseDTO<Empresa>.Ok(result, "Consulta exitosa"));
            }
            else
            {
                return BadRequest(GenericResponseDTO<string>.Fail(_usuarioService.LastError));
            }
        }

        [HttpGet("PasswordRecovery/Verify/{token}")]
        [AllowAnonymous]
        [SwaggerResponse(200, "Token válido", typeof(GenericResponseDTO<object>))]
        [SwaggerResponse(404, "Token no existe")]
        [SwaggerResponse(410, "Token expirado o usado")]
        public async Task<IActionResult> VerifyPasswordRecovery(string token)
        {
            // ✅ Usa la inyección directa, no accedas a campos internos de otro servicio
            var peticion = await _passwordRecoveryService.GetSolicitudByToken(token);
            if (peticion == null)
                return NotFound(GenericResponseDTO<string>.Fail("Token no encontrado."));

            // Usa UTC para coincidir con lo que guardas
            if (DateTime.UtcNow > peticion.FechaVencimiento)
            {
                await _passwordRecoveryService.DeleteSolicitud(peticion);
                return StatusCode(410, GenericResponseDTO<string>.Fail("El enlace ha vencido."));
            }

            if (peticion.Usado || peticion.Eliminado)
                return StatusCode(410, GenericResponseDTO<string>.Fail("El enlace ya fue utilizado."));

            return Ok(GenericResponseDTO<object>.Ok(new { token, expiresAtUtc = peticion.FechaVencimiento }));
        }



    }
}
