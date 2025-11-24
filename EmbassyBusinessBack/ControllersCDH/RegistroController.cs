using Dapper;
using EBDTOs;
using EBDTOs.CDH;
using EBEntities;
using EmbassyBusinessBack.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Data;
using System.Data.SqlClient;

namespace EmbassyBusinessBack.Controllers
{
    [Route("api/cdh/registro")]
    [ApiController]
    [AllowAnonymous] // El registro debe ser público
    public class CDHRegistroController : ControllerBase
    {
        private readonly IConfiguration _config;

        public CDHRegistroController(IConfiguration config)
        {
            _config = config;
        }

        private SqlConnection Conn()
        {
            return new SqlConnection(_config.GetConnectionString("SQLConexion"));
        }


        [HttpPost("enviar-codigo")]
        public async Task<IActionResult> EnviarCodigo([FromBody] CodigoEnviarDTO dto,
                                              [FromServices] TwilioSmsService sms)
        {
            if (dto == null)
                return BadRequest(GenericResponseDTO<bool>.Fail("Datos incompletos"));

            if (string.IsNullOrWhiteSpace(dto.Telefono))
                return BadRequest(GenericResponseDTO<bool>.Fail("Debe enviar teléfono"));

            // Normalizar teléfono
            dto.Telefono = dto.Telefono
                .Replace(" ", "").Replace("-", "")
                .Replace("(", "").Replace(")", "")
                .Replace("+", "");

            var codigo = new Random().Next(10000, 99999).ToString();

            using var con = Conn();
            await con.OpenAsync();

            string sql = @"
        INSERT INTO CDH_RegistroVerificacion
        (TipoRegistro, Telefono, CodigoVerificacion)
        VALUES (1, @tel, @codigo)";

            await con.ExecuteAsync(sql, new { tel = dto.Telefono, codigo });

            try
            {
                await sms.EnviarSmsAsync(dto.Telefono, $"Tu código es: {codigo}");
            }
            catch (Exception ex)
            {
                return StatusCode(500, GenericResponseDTO<bool>.Fail($"Error Twilio: {ex.Message}"));
            }

            return Ok(GenericResponseDTO<string>.Ok(codigo, "Código generado"));
        }

        [HttpPost("validar-codigo")]
        public async Task<IActionResult> ValidarCodigo([FromBody] CodigoValidarDTO dto)
        {
            // Normalizar teléfono
            dto.Telefono = dto.Telefono?
                .Replace(" ", "")
                .Replace("-", "")
                .Replace("(", "")
                .Replace(")", "")
                .Replace("+", "");

            using var con = Conn();
            await con.OpenAsync();

            string sql = @"
        SELECT TOP 1 * 
        FROM CDH_RegistroVerificacion
        WHERE Telefono = @tel AND CodigoVerificacion = @codigo
        ORDER BY ID DESC";

            var registro = await con.QueryFirstOrDefaultAsync(sql, new
            {
                tel = dto.Telefono,
                codigo = dto.Codigo
            });

            if (registro == null)
                return BadRequest(GenericResponseDTO<bool>.Fail("Código incorrecto"));

            // ====== UPDATE DEL REGISTRO ======
            await con.ExecuteAsync(@"
        UPDATE CDH_RegistroVerificacion
        SET Validado = 1,
            FechaValidacion = GETDATE()
        WHERE ID = @id;
    ", new { id = registro.ID });

            // ====== CREAR USUARIO ======
            var usuario = await con.QueryFirstOrDefaultAsync(
                "SELECT * FROM dbo.Usuario WHERE Celular = @tel",
                new { tel = dto.Telefono });

            if (usuario == null)
            {
                string insert = @"
            INSERT INTO dbo.Usuario
            (Nombres, Apellidos, Email, Password, Celular, 
             CatalogoPaisID, CatalogoEstadoID,
             Ciudad, EstadoTexto, FuenteOrigenID, 
             FechaCreacion, Eliminado, MostrarOnboarding, RolesID)
            VALUES (
               NULL, NULL, '', '', @tel,
               151, 6, 2, NULL, 2,
               GETDATE(), 0, 1, 3
            );";

                await con.ExecuteScalarAsync<int>(insert, new { tel = dto.Telefono });
            }

            return Ok(GenericResponseDTO<bool>.Ok(true, "Código válido"));
        }



        [HttpPost("crear-password")]
        public async Task<IActionResult> CrearPassword([FromBody] CrearPasswordDTO dto)
        {
            using var con = Conn();
            await con.OpenAsync();

            var usuario = await con.QueryFirstOrDefaultAsync(
                "SELECT * FROM dbo.Usuario WHERE Celular = @tel",
                new { tel = dto.Telefono });

            if (usuario == null)
                return BadRequest(GenericResponseDTO<bool>.Fail("Usuario no encontrado"));

            string hash = BCrypt.Net.BCrypt.HashPassword(dto.Password);

            await con.ExecuteAsync(@"
        UPDATE dbo.Usuario
        SET Password = @pass,
            MostrarOnboarding = 1
        WHERE Celular = @tel",
                new { pass = hash, tel = dto.Telefono });

            return Ok(GenericResponseDTO<bool>.Ok(true, "Password creado"));
        }
    }
}