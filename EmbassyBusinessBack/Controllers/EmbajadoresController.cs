using EBDTOs;
using EBEmail;
using EBRepositories.Interfaces;
using EBServices.Interfaces;
using EmbassyBusinessBack.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Security.Cryptography;
using static Org.BouncyCastle.Math.EC.ECCurve;
using static System.Net.WebRequestMethods;

namespace EmbassyBusinessBack.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class EmbajadoresController : ControllerBase
    {
        private readonly IEmailService _emailService;
        private readonly IConfiguration _config;

        public EmbajadoresController(IEmailService emailService, IConfiguration config)
        {
            _emailService = emailService;
            _config = config;
        }

        // Código URL-seguro (alfanumérico legible)
        public static string GenerarCodigoInvitacion(int length)
        {
            const string alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz23456789";
            var bytes = RandomNumberGenerator.GetBytes(length);
            var chars = new char[length];
            for (int i = 0; i < length; i++)
                chars[i] = alphabet[bytes[i] % alphabet.Length];
            return new string(chars);
        }

        /*public IActionResult Index()
        {
            return View();
        }*/


        [HttpGet("getArbolEmbajadores")]
        [AllowAnonymous]
        public IActionResult GetArbolEmbajadores(int baseId = 0)
        {
            const int GRUPO_EMBASSY = 2;

            // Estructura de salida compatible con lo que ya consumes en el front
            var display = new CelulaDisplay
            {
                nivel2 = new List<NodoCelula>(),
                nivel3 = new List<NodoCelula>()
            };

            // -------- helper locals ----------
            List<NodoCelula> GetEmbajadoresTopLevel()    
            {
                var list = new List<NodoCelula>();
                var dt = DataAccess.performQuery($@"
            SELECT ID, Nombres, Apellidos, Email
            FROM Usuario
            WHERE Eliminado = 0
              AND RolesID = 3
              AND GrupoID = {GRUPO_EMBASSY}
              AND (UsuarioParent IS NULL OR UsuarioParent = 0);");

                foreach (DataRow dr in dt.Rows)
                {
                    list.Add(new NodoCelula
                    {
                        usuarioId = Convert.ToInt32(dr["ID"]),
                        nombre = $"{dr["Nombres"]} {dr["Apellidos"]}",
                        contacto = dr["Email"]?.ToString(),
                        parentId = 0
                    });
                }
                return list;
            }

            List<NodoCelula> GetEmbajadoresHijos(int parentId)
            {
                var list = new List<NodoCelula>();
                var dt = DataAccess.performQuery($@"
            SELECT ID, Nombres, Apellidos, Email, UsuarioParent
            FROM Usuario
            WHERE Eliminado = 0
              AND RolesID = 3
              AND UsuarioParent = {parentId};");

                foreach (DataRow dr in dt.Rows)
                {
                    list.Add(new NodoCelula
                    {
                        usuarioId = Convert.ToInt32(dr["ID"]),
                        nombre = $"{dr["Nombres"]} {dr["Apellidos"]}",
                        contacto = dr["Email"]?.ToString(),
                        parentId = Convert.ToInt32(dr["UsuarioParent"])
                    });
                }
                return list;
            }
            // -----------------------------------

            // Nivel1 (raíz)
            var baseNode = new NodoCelula();
            if (baseId == 0)
            {
                baseNode.usuarioId = 0;
                baseNode.nombre = "Embassy";
                baseNode.contacto = "Embajadores";
                baseNode.parentId = 0;

                // Nivel2: todos los embajadores top-level (sin parent)
                var n2 = GetEmbajadoresTopLevel();
                display.nivel2.AddRange(n2);

                // Nivel3: embajadores invitados por cada embajador top-level
                foreach (var e in n2)
                {
                    var hijos = GetEmbajadoresHijos(e.usuarioId);
                    display.nivel3.AddRange(hijos);
                }
            }
            else
            {
                // Cargar datos del embajador base
                var dtUsuario = DataAccess.performQuery($@"
            SELECT u.ID,
                   (u.Nombres + ' ' + u.Apellidos) AS UsuarioNombre,
                   u.Email,
                   u.UsuarioParent,
                   p.Nombres + ' ' + p.Apellidos AS ParentNombre
            FROM Usuario u
            LEFT JOIN Usuario p ON p.ID = u.UsuarioParent
            WHERE u.ID = {baseId};");

                if (dtUsuario.Rows.Count == 0)
                    return NotFound($"No existe el usuario {baseId}");

                var r = dtUsuario.Rows[0];
                baseNode.usuarioId = baseId;
                baseNode.nombre = r["UsuarioNombre"]?.ToString();
                baseNode.contacto = r["Email"]?.ToString();
                baseNode.parentId = string.IsNullOrWhiteSpace(r["UsuarioParent"]?.ToString()) ? 0 : Convert.ToInt32(r["UsuarioParent"]);
                baseNode.parentNombre = r["ParentNombre"]?.ToString();

                // Nivel2: embajadores directamente invitados por este embajador base
                var n2 = GetEmbajadoresHijos(baseId);
                display.nivel2.AddRange(n2);

                // Nivel3: nietos (invitados de cada embajador hijo)
                foreach (var h in n2)
                {
                    var nietos = GetEmbajadoresHijos(h.usuarioId);
                    display.nivel3.AddRange(nietos);
                }
            }

            display.nivel1 = baseNode;
            return Ok(display);
        }

        [HttpGet("getCelulaFromHere")]
        [AllowAnonymous]
        public async Task<IActionResult> getCelulaFromHere(int embajadorBase)
        {
            CelulaDisplay display = new CelulaDisplay
            {
                nivel2 = new List<NodoCelula>(),
                nivel3 = new List<NodoCelula>(),
                nivel4 = new List<NodoCelula>() // ya no se usa, lo dejamos vacío
            };

            NodoCelula nodoBase = new NodoCelula();
            int? baseRol = null;

            // ===== Nodo base =====
            if (embajadorBase == 0)
            {
                nodoBase.usuarioId = 0;
                nodoBase.nombre = "Embassy";
                nodoBase.contacto = "Embajadores con cartera de empresas";
                nodoBase.parentId = 0;
            }
            else
            {
                var dtUsuario = DataAccess.performQuery($@"
            SELECT u.ID,
                   (u.Nombres + ' ' + u.Apellidos) AS UsuarioNombre,
                   u.Email,
                   u.UsuarioParent,
                   p.Nombres + ' ' + p.Apellidos AS ParentNombre,
                   u.RolesID
            FROM Usuario u
            LEFT JOIN Usuario p ON p.ID = u.UsuarioParent
            WHERE u.ID = {embajadorBase};");

                if (dtUsuario.Rows.Count > 0)
                {
                    var r = dtUsuario.Rows[0];
                    nodoBase.usuarioId = embajadorBase;
                    nodoBase.nombre = r["UsuarioNombre"]?.ToString();
                    nodoBase.contacto = r["Email"]?.ToString();
                    nodoBase.parentId = string.IsNullOrWhiteSpace(r["UsuarioParent"]?.ToString()) ? 0 : Convert.ToInt32(r["UsuarioParent"]);
                    nodoBase.parentNombre = r["ParentNombre"]?.ToString();
                    baseRol = string.IsNullOrWhiteSpace(r["RolesID"]?.ToString()) ? null : Convert.ToInt32(r["RolesID"]);
                }
            }

            // ===== Helpers =====

            // Embajadores (RolesID=3) que tengan al menos 1 empresa
            List<NodoCelula> GetEmbajadoresConEmpresa()
            {
                var list = new List<NodoCelula>();
                var dt = DataAccess.performQuery(@"
            SELECT DISTINCT u.ID, u.Nombres, u.Apellidos, u.Email
            FROM Usuario u
            WHERE u.Eliminado = 0
              AND u.RolesID = 3
              AND EXISTS (SELECT 1 FROM Empresa e WHERE e.Eliminado = 0 AND e.embajadorId = u.ID);");

                foreach (DataRow dr in dt.Rows)
                {
                    list.Add(new NodoCelula
                    {
                        usuarioId = Convert.ToInt32(dr["ID"]),
                        nombre = $"{dr["Nombres"]} {dr["Apellidos"]}",
                        contacto = dr["Email"]?.ToString(),
                        parentId = 0
                    });
                }
                return list;
            }

            // Empresas ligadas a un usuario (embajador)
            List<NodoCelula> GetEmpresasByUsuario(int userId)
            {
                var list = new List<NodoCelula>();
                var dt = DataAccess.performQuery($@"
            SELECT ID, RFC, RazonSocial, NombreComercial, Descripcion, embajadorId
            FROM Empresa
            WHERE Eliminado = 0 AND embajadorId = {userId};");

                foreach (DataRow dr in dt.Rows)
                {
                    list.Add(new NodoCelula
                    {
                        usuarioId = Convert.ToInt32(dr["ID"]),
                        nombre = dr["NombreComercial"]?.ToString() ?? dr["RazonSocial"]?.ToString(),
                        contacto = dr["RFC"]?.ToString() ?? dr["Descripcion"]?.ToString(),
                        parentId = userId
                    });
                }
                return list;
            }

            // ===== Carga según contexto =====

            if (embajadorBase == 0)
            {
                // ROOT: embajadores con empresa (nivel2) y sus empresas (nivel3)
                var embajadores = GetEmbajadoresConEmpresa();
                display.nivel2.AddRange(embajadores);

                foreach (var emb in embajadores)
                {
                    var empresas = GetEmpresasByUsuario(emb.usuarioId);
                    display.nivel3.AddRange(empresas);
                }
            }
            else
            {
                // DRILLDOWN: si el base es embajador → muestra SOLO sus empresas (en nivel2)
                switch (baseRol)
                {
                    case 3: // Embajador
                        {
                            var empresas = GetEmpresasByUsuario(embajadorBase);
                            display.nivel2.AddRange(empresas);
                            break;
                        }
                    case 1: // Admin
                    case 2: // Socio
                    default:
                        {
                            var embajadores = GetEmbajadoresConEmpresa();
                            display.nivel2.AddRange(embajadores);
                            foreach (var emb in embajadores)
                            {
                                var empresas = GetEmpresasByUsuario(emb.usuarioId);
                                display.nivel3.AddRange(empresas);
                            }
                            break;
                        }
                }
            }

            display.nivel1 = nodoBase;
            return Ok(display);
        }

        // ---- Metodo para EmbassyFront (Celula)
        public class MiCelulaDisplay
        {
            public NodoCelula? padre { get; set; }
            public NodoCelula yo { get; set; } = new NodoCelula();
            public List<NodoCelula> hijos { get; set; } = new();
        }

        [HttpGet("miCelula")]
        [AllowAnonymous]
        public IActionResult MiCelula(int yoId, int limit = 4)
        {
            var result = new MiCelulaDisplay();

            // Usuario Principal
            var dtYo = DataAccess.performQuery($@"
            SELECT u.ID, u.Nombres, u.Apellidos, u.Email, u.UsuarioParent
            FROM Usuario u
            WHERE u.ID = {yoId};");
            if (dtYo.Rows.Count == 0) return NotFound();

            var rYo = dtYo.Rows[0];
            result.yo.usuarioId = yoId;
            result.yo.nombre = $"{rYo["Nombres"]} {rYo["Apellidos"]}".Trim();
            result.yo.contacto = rYo["Email"]?.ToString();

            // PADRE (opcional)
            if (!string.IsNullOrWhiteSpace(rYo["UsuarioParent"]?.ToString()))
            {
                var parentId = Convert.ToInt32(rYo["UsuarioParent"]);
                var dtP = DataAccess.performQuery($@"
            SELECT ID, Nombres, Apellidos, Email
            FROM Usuario
            WHERE ID = {parentId};");
                if (dtP.Rows.Count > 0)
                {
                    var rp = dtP.Rows[0];
                    result.padre = new NodoCelula
                    {
                        usuarioId = parentId,
                        nombre = $"{rp["Nombres"]} {rp["Apellidos"]}".Trim(),
                        contacto = rp["Email"]?.ToString()
                    };
                }
            }

            // HIJOS (invitados por otros embajadores) — max 4
            var dtH = DataAccess.performQuery($@"
            SELECT TOP {limit} ID, Nombres, Apellidos, Email
            FROM Usuario
            WHERE Eliminado = 0 AND UsuarioParent = {yoId}
            ORDER BY FechaCreacion ASC;");

            foreach (DataRow dr in dtH.Rows)
            {
                result.hijos.Add(new NodoCelula
                {
                    usuarioId = Convert.ToInt32(dr["ID"]),
                    nombre = $"{dr["Nombres"]} {dr["Apellidos"]}".Trim(),
                    contacto = dr["Email"]?.ToString()
                });
            }

            return Ok(result);
        }


        [HttpGet("GetDatosInvitacion")]
        [HttpGet("/api/Usuario/GetDatosInvitacion")] // alias legacy
        [AllowAnonymous]

        public async Task<IActionResult> GetDatosInvitacion(string codigo)
        {
            var i = new InvitacionEmbajadorDTO();
            i.vigente = false;

            string q = $@"select invitaciones.*,  Usuarioinvita.Nombres as UsuarioInvitaNombres, Usuarioinvita.Apellidos as UsuarioInvitaApellidos, 
            isnull(UsuarioRegistrado.ID, 0) as UsuarioRegistradoId from invitaciones
            left join Usuario as UsuarioRegistrado on UsuarioRegistrado.CodigoInvitacion = invitaciones.CodigoDeInvitacion
            left join usuario as Usuarioinvita on Usuarioinvita.ID = Invitaciones.EmbajadorReferenteID 
            where CodigoDeInvitacion =  '{codigo}'
            ";

            DataTable dtInvitacion = DataAccess.performQuery(q);
            if (dtInvitacion.Rows.Count > 0) {
                i.nombreInvitador = dtInvitacion.Rows[0]["UsuarioInvitaNombres"].ToString() + " " + dtInvitacion.Rows[0]["UsuarioInvitaApellidos"].ToString();
                i.correoElectronicoInvitacion = dtInvitacion.Rows[0]["CorreoElectronico"].ToString();
                i.codigo = codigo;
                i.embajadorReferenteId = long.Parse(dtInvitacion.Rows[0]["EmbajadorReferenteID"].ToString());

                long UsuarioRegistradoId = long.Parse(dtInvitacion.Rows[0]["UsuarioRegistradoId"].ToString());
                if (UsuarioRegistradoId == 0) {
                    i.vigente = true;
                }
            }

            return Ok(i);
        }


        public class InvitadoResumen
        {
            public DateTime fechaInvitacion { get; set; }
            public string nombre { get; set; } = "";
            public string estatus { get; set; } = ""; // "Pendiente" | "Aceptado"
        }

        public class ResumenInvitadosDTO
        {
            public List<InvitadoResumen> embajadoresInvitados { get; set; } = new();   // pendientes
            public int aceptados { get; set; } = 0;                                    // total aceptados
            public List<InvitadoResumen> embajadoresAceptados { get; set; } = new();   // lista aceptados (opcional)
        }

        [HttpGet("InvitadosResumen")]
        [AllowAnonymous]
        public IActionResult InvitadosResumen(int embajadorId, int top = 5)
        {
            var dto = new ResumenInvitadosDTO();

            // Pendientes: invitaciones sin usuario registrado (JOIN por CodigoInvitacion)
            var qPend = $@"
        SELECT TOP {top} i.FechaHora, i.CorreoElectronico
        FROM Invitaciones i
        LEFT JOIN Usuario u ON u.CodigoInvitacion = i.CodigoDeInvitacion
        WHERE i.EmbajadorReferenteID = {embajadorId}
          AND u.ID IS NULL
        ORDER BY i.FechaHora DESC;";
            var dtPend = DataAccess.performQuery(qPend);
            foreach (DataRow r in dtPend.Rows)
            {
                dto.embajadoresInvitados.Add(new InvitadoResumen
                {
                    fechaInvitacion = Convert.ToDateTime(r["FechaHora"]),
                    nombre = r["CorreoElectronico"]?.ToString() ?? "",
                    estatus = "Pendiente"
                });
            }

            // Aceptados: con usuario registrado
            var qAccList = $@"
        SELECT TOP {top} i.FechaHora, u.Nombres, u.Apellidos
        FROM Invitaciones i
        JOIN Usuario u ON u.CodigoInvitacion = i.CodigoDeInvitacion
        WHERE i.EmbajadorReferenteID = {embajadorId}
        ORDER BY i.FechaHora DESC;";
            var dtAccList = DataAccess.performQuery(qAccList);
            foreach (DataRow r in dtAccList.Rows)
            {
                dto.embajadoresAceptados.Add(new InvitadoResumen
                {
                    fechaInvitacion = Convert.ToDateTime(r["FechaHora"]),
                    nombre = $"{r["Nombres"]} {r["Apellidos"]}".Trim(),
                    estatus = "Aceptado"
                });
            }

            // Total aceptados (sin TOP)
            var qAccCount = $@"
        SELECT COUNT(1) AS Cnt
        FROM Invitaciones i
        JOIN Usuario u ON u.CodigoInvitacion = i.CodigoDeInvitacion
        WHERE i.EmbajadorReferenteID = {embajadorId};";
            var dtAccCount = DataAccess.performQuery(qAccCount);
            dto.aceptados = dtAccCount.Rows.Count > 0 ? Convert.ToInt32(dtAccCount.Rows[0]["Cnt"]) : 0;

            return Ok(dto);
        }


        [HttpPost("InvitarEmbajador")]
        [AllowAnonymous]
        public async Task<IActionResult> InvitarEmbajador([FromBody] InvitacionEmbajadorRequest request)
        {
            var response = new RespuestaEstatusMensaje
            {
                estatus = -1,
                mensaje = "Lo sentimos, su invitación no pudo ser enviada. Por favor intente de nuevo más tarde"
            };

            try
            {
                // 1) Validaciones mínimas
                if (request == null || request.referente_id <= 0 || string.IsNullOrWhiteSpace(request.email))
                {
                    response.mensaje = "Parámetros incompletos.";
                    return Ok(response);
                }

                // 2) URL base
                var baseUrl = _config["Urls:AdminBase"]?.TrimEnd('/');
                if (string.IsNullOrWhiteSpace(baseUrl))
                {
                    response.mensaje = "Configuración faltante: Urls:AdminBase.";
                    return Ok(response);
                }

                // 3) Verificar embajador referente
                var dtEmbajadorReferente = DataAccess.performQuery(
                    $"SELECT TOP 1 Nombres, Apellidos FROM Usuario WHERE ID = {request.referente_id} AND Eliminado = 0");
                if (dtEmbajadorReferente.Rows.Count == 0)
                {
                    response.mensaje = "No existe el embajador referente.";
                    return Ok(response);
                }
                var nombreInvitador = $"{dtEmbajadorReferente.Rows[0]["Nombres"]} {dtEmbajadorReferente.Rows[0]["Apellidos"]}".Trim();

                // 4) Normalizaciones y validaciones de email
                var email = request.email.Trim().ToLowerInvariant();

                var dtUsuarioYaExiste = DataAccess.performQuery(
                    $"SELECT TOP 1 ID FROM Usuario WHERE LOWER(Email) = '{email.Replace("'", "''")}' AND Eliminado = 0");
                if (dtUsuarioYaExiste.Rows.Count > 0)
                {
                    response.estatus = 0;
                    response.mensaje = "Ya existe un usuario registrado con este correo electrónico.";
                    return Ok(response);
                }

                var dtInvitacionPrevia = DataAccess.performQuery(
                    $"SELECT TOP 1 1 FROM Invitaciones WHERE EmbajadorReferenteID = {request.referente_id} AND LOWER(CorreoElectronico) = '{email.Replace("'", "''")}'");
                if (dtInvitacionPrevia.Rows.Count > 0)
                {
                    response.estatus = 0;
                    response.mensaje = "Ya has enviado previamente una invitación a este correo.";
                    return Ok(response);
                }

                var codeLen = _config.GetValue<int>("Invitations:CodeLength", 8);
                var codigo = GenerarCodigoInvitacion(codeLen);
                var enlaceRegistro = $"{baseUrl}/registro/{Uri.EscapeDataString(codigo)}";

                // 6) HTML del correo (igual que ya lo tenías)
                var mensajeHtml = $@"
<html>
<head><meta charset='utf-8'/><style>
body{{font-family:Arial,sans-serif;background:#f9f9f9;padding:20px;color:#333}}
.container{{background:#fff;padding:30px;border-radius:10px;box-shadow:0 2px 8px rgba(0,0,0,.1);max-width:600px;margin:40px auto;text-align:center}}
.button-wrapper{{margin:30px 0 10px}} .small-text{{font-size:12px;color:#888;margin-top:20px}}
</style></head>
<body><div class='container'>
<h2>¡Hola!</h2>
<p><strong>{System.Net.WebUtility.HtmlEncode(nombreInvitador)}</strong> te ha invitado a formar parte de su núcleo de embajadores.</p>
<p>Haz clic en el botón de abajo para registrarte como embajador.</p>
<div class='button-wrapper'>
  <table role='presentation' style='margin:auto'><tr><td align='center' bgcolor='#4CAF50' style='border-radius:5px;'>
    <a href='{enlaceRegistro}' target='_blank' style='font-size:16px;font-family:Arial;color:#fff;text-decoration:none;padding:12px 25px;display:inline-block;'>Registrarme ahora</a>
  </td></tr></table>
</div>
<p class='small-text'>Si no estás seguro de por qué recibiste esta invitación, puedes ignorar este mensaje de manera segura.</p>
</div></body></html>";

                // 7) INSERT (sin columnas calculadas CorreoNormalizado/CodigoNorm)
                int invitacionId;
                using (var cn = new SqlConnection(DataAccess.ConnectionString()))
                {
                    await cn.OpenAsync();
                    using (var tx = cn.BeginTransaction())
                    {
                        const string insertSql = @"
INSERT INTO dbo.Invitaciones
(EmbajadorReferenteID, CorreoElectronico, CodigoDeInvitacion, FechaHora, PrimeraLinea, Aceptado, Activo, UsuarioIDCreado)
OUTPUT INSERTED.ID
VALUES
(@EmbajadorReferenteID, @CorreoElectronico, @CodigoDeInvitacion, GETDATE(), @PrimeraLinea, 0, 1, @UsuarioIDCreado);";

                        using (var cmd = new SqlCommand(insertSql, cn, tx))
                        {
                            cmd.Parameters.AddWithValue("@EmbajadorReferenteID", request.referente_id);
                            cmd.Parameters.AddWithValue("@CorreoElectronico", email);
                            cmd.Parameters.AddWithValue("@CodigoDeInvitacion", codigo);
                            cmd.Parameters.AddWithValue("@PrimeraLinea", request.primeraLinea ? 1 : 0);
                            cmd.Parameters.AddWithValue("@UsuarioIDCreado", request.referente_id); // o 0/null si así lo manejas

                            invitacionId = (int)await cmd.ExecuteScalarAsync();
                        }

                        tx.Commit(); // confirmamos el insert
                    }
                }

                // 8) Enviar correo (si falla, la invitación ya quedó registrada)
                var resultadoMail = await _emailService.SendEmailAsync(
                    email,
                    "Te invito a formar parte de mi núcleo de embajadores",
                    mensajeHtml,
                    "Embassy Embajadores de Negocios");

                if (!resultadoMail.Success)
                {
                    response.estatus = -1;
                    response.mensaje = "La invitación fue registrada pero no se pudo enviar el correo. Intenta reenviarlo más tarde.";
                    return Ok(response);
                }

                response.estatus = 1;
                response.mensaje = "Hemos enviado tu invitación con éxito. Muy pronto, un nuevo embajador se unirá a tu núcleo.";
                return Ok(response);
            }
            catch (Exception ex)
            {
                return Ok(new RespuestaEstatusMensaje
                {
                    estatus = -1,
                    mensaje = "Error al guardar/enviar invitación: " + ex.Message
                });
            }
        }


        // DTOs originales…
        public class RespuestaEstatusMensaje
        {
            public int estatus { get; set; }
            public string mensaje { get; set; } = "";
        }
        public class InvitacionEmbajadorRequest
        {
            public int referente_id { get; set; }
            public string email { get; set; } = "";
            public bool primeraLinea { get; set; }
        }
    }

    public class InvitacionEmbajadorRequest
    {
        public int referente_id { get; set; }
        public string email { get; set; }
        public bool primeraLinea { get; set; }
    }

    public class InvitacionEmbajadorDTO
    {
        public bool vigente { get; set; }
        public string codigo { get; set; }
        public string nombreInvitador { get; set; }
        public string correoElectronicoInvitacion { get; set; }
        public long embajadorReferenteId { get; set; }
    }

    public class CelulaDisplay { 
        public NodoCelula nivel1 { get; set; }

        public List<NodoCelula> nivel2 { get; set; }

        public List<NodoCelula> nivel3 { get; set; }

        public List<NodoCelula> nivel4 { get; set; }
    }

    public class NodoCelula { 
        public int usuarioId { get; set; }
        public string nombre { get; set; }
        public string contacto { get; set;}
        public int parentId { get; set; }
        public string parentNombre { get; set; }


    }
}
