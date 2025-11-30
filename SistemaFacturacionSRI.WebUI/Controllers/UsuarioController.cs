using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaFacturacionSRI.Domain.DTOs.Auth;
using SistemaFacturacionSRI.Domain.DTOs.Common;
using SistemaFacturacionSRI.Domain.DTOs.Usuario;
using SistemaFacturacionSRI.Domain.Interfaces.Services;
using SistemaFacturacionSRI.WebUI.Authorization;
using System.Security.Claims;

namespace SistemaFacturacionSRI.WebUI.Controllers
{
    /// <summary>
    /// Controlador para la gestión de usuarios del sistema.
    /// Solo accesible para usuarios con rol Administrador.
    /// </summary>
    [Route("api/usuarios")]
    [ApiController]
    public class UsuarioController : ControllerBase
    {
        private readonly IUsuarioService _usuarioService;
        private readonly ILogger<UsuarioController> _logger;

        public UsuarioController(
            IUsuarioService usuarioService,
            ILogger<UsuarioController> logger)
        {
            _usuarioService = usuarioService;
            _logger = logger;
        }

        // ================= T-32: LISTAR USUARIOS =================

        /// <summary>
        /// Lista todos los usuarios del sistema con filtros y paginación.
        /// </summary>
        [AdminAuthorize]
        [HttpGet]
        [ProducesResponseType(typeof(PagedResultDto<UsuarioListDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<PagedResultDto<UsuarioListDto>>> ListarUsuarios(
            [FromQuery] string? busqueda = null,
            [FromQuery] int? rolId = null,
            [FromQuery] bool? estado = null,
            [FromQuery] bool? soloBloqueados = null,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? orderBy = null,
            [FromQuery] bool orderAscending = false)
        {
            try
            {
                var filtro = new FiltroUsuarioDto
                {
                    Busqueda = busqueda,
                    RolId = rolId,
                    Estado = estado,
                    SoloBloqueados = soloBloqueados,
                    PageNumber = pageNumber,
                    PageSize = pageSize,
                    OrderBy = orderBy,
                    OrderAscending = orderAscending
                };

                var resultado = await _usuarioService.ListarUsuariosAsync(filtro);
                return Ok(resultado);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al listar usuarios");
                return StatusCode(StatusCodes.Status500InternalServerError, 
                    new { message = "Error interno al listar usuarios" });
            }
        }

        // ================= T-33: OBTENER POR ID =================

        /// <summary>
        /// Obtiene un usuario específico por su ID.
        /// </summary>
        [AdminAuthorize]
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(UsuarioDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<UsuarioDto>> ObtenerUsuarioPorId(int id)
        {
            try
            {
                if (id <= 0)
                {
                    return BadRequest(new { message = "El ID debe ser mayor a cero" });
                }

                var usuario = await _usuarioService.ObtenerUsuarioPorIdAsync(id);

                if (usuario == null)
                {
                    return NotFound(new { message = $"No se encontró el usuario con ID {id}" });
                }

                return Ok(usuario);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener usuario {UsuarioId}", id);
                return StatusCode(StatusCodes.Status500InternalServerError, 
                    new { message = "Error interno al obtener usuario" });
            }
        }

        // ================= T-34: CREAR USUARIO =================

        /// <summary>
        /// Crea un nuevo usuario en el sistema.
        /// </summary>
        [AdminAuthorize]
        [HttpPost]
        [ProducesResponseType(typeof(UsuarioDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<UsuarioDto>> CrearUsuario([FromBody] CrearUsuarioDto dto)
        {
            try
            {
                if (dto == null)
                {
                    return BadRequest(new { message = "Los datos del usuario son requeridos" });
                }

                if (!ModelState.IsValid)
                {
                    return BadRequest(new
                    {
                        message = "Datos inválidos",
                        errors = ModelState.Values
                            .SelectMany(v => v.Errors)
                            .Select(e => e.ErrorMessage)
                    });
                }

                var usuario = await _usuarioService.CrearUsuarioAsync(dto);

                return CreatedAtAction(
                    nameof(ObtenerUsuarioPorId),
                    new { id = usuario.Id }, 
                    usuario
                );
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear usuario");
                return StatusCode(StatusCodes.Status500InternalServerError, 
                    new { message = "Error interno al crear usuario" });
            }
        }

        // ================= T-35: ACTUALIZAR USUARIO =================

        /// <summary>
        /// Actualiza la información de un usuario existente.
        /// </summary>
        [AdminAuthorize]
        [HttpPut("{id}")]
        [ProducesResponseType(typeof(UsuarioDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<UsuarioDto>> ActualizarUsuario(int id, [FromBody] ActualizarUsuarioDto dto)
        {
            try
            {
                if (dto == null)
                {
                    return BadRequest(new { message = "Los datos del usuario son requeridos" });
                }

                if (id <= 0)
                {
                    return BadRequest(new { message = "El ID debe ser mayor a cero" });
                }

                if (!ModelState.IsValid)
                {
                    return BadRequest(new
                    {
                        message = "Datos inválidos",
                        errors = ModelState.Values
                            .SelectMany(v => v.Errors)
                            .Select(e => e.ErrorMessage)
                    });
                }

                if (dto.UsuarioId != 0 && dto.UsuarioId != id)
                {
                    return BadRequest(new { message = "El ID de la ruta debe coincidir con el ID del usuario" });
                }

                dto.UsuarioId = id;

                var usuarioActualizado = await _usuarioService.ActualizarUsuarioAsync(dto);

                return Ok(usuarioActualizado);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al actualizar usuario {UsuarioId}", id);
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "Error interno al actualizar usuario" });
            }
        }

        // ================= T-36: DESACTIVAR USUARIO (SOFT DELETE) =================

        /// <summary>
        /// Desactiva un usuario (soft delete). No se elimina físicamente de la BD.
        /// </summary>
        [AdminAuthorize]
        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult> DesactivarUsuario(int id)
        {
            try
            {
                if (id <= 0)
                {
                    return BadRequest(new { message = "El ID debe ser mayor a cero" });
                }

                await _usuarioService.DesactivarUsuarioAsync(id);

                return NoContent();
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al desactivar usuario {UsuarioId}", id);
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "Error interno al desactivar usuario" });
            }
        }

        // ================= T-27: ACTIVAR USUARIO =================

        /// <summary>
        /// Activa un usuario previamente desactivado y resetea intentos de login.
        /// </summary>
        [AdminAuthorize]
        [HttpPut("{id}/activar")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult> ActivarUsuario(int id)
        {
            try
            {
                if (id <= 0)
                {
                    return BadRequest(new { message = "El ID debe ser mayor a cero" });
                }

                await _usuarioService.ActivarUsuarioAsync(id);

                return NoContent();
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al activar usuario {UsuarioId}", id);
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "Error interno al activar usuario" });
            }
        }

        // ================= T-37: CAMBIAR ROL =================

        /// <summary>
        /// Cambia el rol de un usuario (solo Admin).
        /// No permite degradar al último administrador del sistema.
        /// </summary>
        [AdminAuthorize]
        [HttpPut("{id}/rol")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult> CambiarRol(int id, [FromBody] CambiarRolDto dto)
        {
            try
            {
                if (dto == null)
                {
                    return BadRequest(new { message = "Los datos del cambio de rol son requeridos" });
                }

                if (id <= 0)
                {
                    return BadRequest(new { message = "El ID debe ser mayor a cero" });
                }

                dto.UsuarioId = id;
                ModelState.Remove(nameof(CambiarRolDto.UsuarioId));

                if (!ModelState.IsValid)
                {
                    return BadRequest(new
                    {
                        message = "Datos inválidos",
                        errors = ModelState.Values
                            .SelectMany(v => v.Errors)
                            .Select(e => e.ErrorMessage)
                    });
                }

                await _usuarioService.CambiarRolAsync(dto);

                return NoContent();
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cambiar rol del usuario {UsuarioId}", id);
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "Error interno al cambiar el rol" });
            }
        }
// WebUI/Controllers/UsuarioController.cs - AGREGAR AL FINAL

// ================= CAMBIAR CONTRASEÑA (PROPIO USUARIO) =================

/// <summary>
/// Permite a un usuario cambiar su propia contraseña.
/// </summary>
// ================= CAMBIAR CONTRASEÑA (PROPIO USUARIO) =================

/// <summary>
/// Permite a un usuario cambiar su propia contraseña.
/// </summary>
[Authorize(Policy = AuthorizationPolicies.AdminOrVendedor)]
[HttpPut("{id}/cambiar-password")]
[ProducesResponseType(StatusCodes.Status204NoContent)]
[ProducesResponseType(StatusCodes.Status400BadRequest)]
[ProducesResponseType(StatusCodes.Status404NotFound)]
[ProducesResponseType(StatusCodes.Status500InternalServerError)]
public async Task<ActionResult> CambiarPassword(int id, [FromBody] CambiarPasswordDto dto)
{
    try
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)
                         ?? User.FindFirst("userId")
                         ?? User.FindFirst("sub");

        if (!User.IsInRole("Administrador"))
        {
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userIdFromToken) || userIdFromToken != id)
            {
                return Forbid();
            }
        }

        if (dto == null)
        {
            return BadRequest(new { message = "Los datos son requeridos" });
        }

        if (id <= 0)
        {
            return BadRequest(new { message = "El ID debe ser mayor a cero" });
        }

        dto.UsuarioId = id;

        if (!ModelState.IsValid)
        {
            return BadRequest(new
            {
                message = "Datos inválidos",
                errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
            });
        }

        await _usuarioService.CambiarPasswordAsync(dto);

        return NoContent();
    }
    catch (InvalidOperationException ex)
    {
        return BadRequest(new { message = ex.Message });
    }
    catch (KeyNotFoundException ex)
    {
        return NotFound(new { message = ex.Message });
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error al cambiar contraseña del usuario {UsuarioId}", id);
        return StatusCode(StatusCodes.Status500InternalServerError,
            new { message = "Error interno al cambiar contraseña" });
    }
}
// ================= VERIFICAR CÉDULA DUPLICADA =================

/// <summary>
/// Verifica si una cédula ya está registrada en el sistema.
/// </summary>
/*
[HttpGet("existe-cedula")]
[ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
[ProducesResponseType(StatusCodes.Status500InternalServerError)]
public async Task<ActionResult<bool>> ExisteCedula(
    [FromQuery] string cedula, 
    [FromQuery] int? excluirUsuarioId = null)
{
    try
    {
        if (string.IsNullOrWhiteSpace(cedula))
        {
            return BadRequest(new { message = "La cédula es requerida" });
        }

        var existe = await _usuarioService.ExisteCedulaAsync(cedula, excluirUsuarioId);
        return Ok(existe);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error al verificar cédula duplicada");
        return StatusCode(StatusCodes.Status500InternalServerError, 
            new { message = "Error al verificar la cédula" });
    }
}
*/




    }
}