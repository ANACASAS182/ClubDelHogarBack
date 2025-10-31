using EBDTOs;
using EBEntities;
using EBServices.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using System.Security.Claims;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class BancoUsuarioController : ControllerBase
{
    private readonly IBancoUsuarioService _bancoUsuarioService;

    public BancoUsuarioController(IBancoUsuarioService bancoUsuarioService)
    {
        _bancoUsuarioService = bancoUsuarioService;
    }

    [HttpGet("GetBancoByID")]
    [SwaggerResponse(200, "Objeto de respuesta", typeof(BancoUsuario))]
    public async Task<IActionResult> GetBancoByID([FromQuery] long id)
    {
        if (!long.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var usuarioId))
            return BadRequest(GenericResponseDTO<string>.Fail("No se pudo obtener el ID de usuario"));

        var result = await _bancoUsuarioService.GetBancoByID(id, usuarioId);

        if (_bancoUsuarioService.HasError == false)
        {
            if (result == null)
                return NotFound(GenericResponseDTO<string>.Fail("No se encontró banco."));
            return Ok(GenericResponseDTO<BancoUsuario>.Ok(result, "Consulta exitosa"));
        }
        else
        {
            return BadRequest(GenericResponseDTO<string>.Fail(_bancoUsuarioService.LastError));
        }
    }

    [HttpGet("GetBancosUsuario")]
    [SwaggerResponse(200, "Objeto de respuesta", typeof(List<BancoUsuario>))]
    public async Task<IActionResult> GetBancosUsuario()
    {
        if (!long.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var usuarioId))
            return BadRequest(GenericResponseDTO<string>.Fail("No se pudo obtener el ID de usuario"));

        var result = await _bancoUsuarioService.GetBancosUsuario(usuarioId);

        if (_bancoUsuarioService.HasError == false)
        {
            if (result == null || result.Count == 0)
                return NotFound(GenericResponseDTO<string>.Fail("No se encontraron bancos.", false));

            return Ok(GenericResponseDTO<List<BancoUsuario>>.Ok(result, "Consulta exitosa"));
        }
        else
        {
            return BadRequest(GenericResponseDTO<string>.Fail(_bancoUsuarioService.LastError));
        }
    }

    [HttpPost("Save")]
    [SwaggerResponse(200, "Objeto de respuesta", typeof(bool))]
    public async Task<IActionResult> Save([FromBody] BancoUsuario model)
    {
        if (!long.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var usuarioId))
            return BadRequest(GenericResponseDTO<string>.Fail("No se pudo obtener el ID de usuario"));

        // 👇 Asegura que la PK real (ID) tenga el valor del proxy (Id) que envía el front
        if (model.ID == 0 && model.Id > 0) model.ID = model.Id;

        if (model.Id > 0)
        {
            var existente = await _bancoUsuarioService.GetBancoByID(model.Id, usuarioId);
            if (existente == null)
                return BadRequest(GenericResponseDTO<string>.Fail("No se pudo obtener el banco."));

            existente.CatBancoId = model.CatBancoId;
            existente.BancoOtro = model.BancoOtro;
            existente.NombreBanco = model.NombreBanco;
            existente.NombreTitular = model.NombreTitular;
            existente.TipoCuenta = model.TipoCuenta;
            existente.NumeroCuenta = model.NumeroCuenta;

            var okEdit = await _bancoUsuarioService.Save(existente, usuarioId);
            if (_bancoUsuarioService.HasError) return BadRequest(GenericResponseDTO<string>.Fail(_bancoUsuarioService.LastError));
            return Ok(GenericResponseDTO<bool>.Ok(okEdit, "Actualizado"));
        }
        else
        {
            model.Id = 0;
            model.ID = 0;                 // 👈 por claridad
            model.UsuarioId = usuarioId;

            var ok = await _bancoUsuarioService.Save(model, usuarioId);
            if (_bancoUsuarioService.HasError)
                return BadRequest(GenericResponseDTO<string>.Fail(_bancoUsuarioService.LastError));

            return Ok(GenericResponseDTO<bool>.Ok(ok, "Guardado"));
        }
    }

    [HttpGet("SoftDelete")]
    [SwaggerResponse(200, "Objeto de respuesta", typeof(bool))]
    public async Task<IActionResult> SoftDelete([FromQuery] long bancoID)
    {
        var result = await _bancoUsuarioService.Delete(bancoID);

        if (_bancoUsuarioService.HasError == false)
            return Ok(GenericResponseDTO<bool>.Ok(result, "Eliminación exitosa"));
        else
            return BadRequest(GenericResponseDTO<string>.Fail(_bancoUsuarioService.LastError));
    }
}