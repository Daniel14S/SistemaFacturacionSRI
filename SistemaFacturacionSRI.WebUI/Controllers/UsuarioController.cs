using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaFacturacionSRI.Application.DTOs.Auth;
using SistemaFacturacionSRI.Application.DTOs.Common;
using SistemaFacturacionSRI.Application.DTOs.Usuario;
using SistemaFacturacionSRI.Application.Interfaces.Services;
using SistemaFacturacionSRI.WebUI.Authorization;

namespace SistemaFacturacionSRI.WebUI.Controllers
{
    /// <summary>
    /// Controlador para la gestión de usuarios del sistema.
    /// Solo accesible para usuarios con rol Administrador.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    [AdminAuthorize]
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

        [HttpGet]
        [ProducesResponseType(typeof(PagedResultDto<UsuarioListDto>), StatusCodes.Status200OK)]
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
                return StatusCode(500, new { message = "Error interno al listar usuarios" });
            }
        }

        // ================= T-33: OBTENER POR ID =================

        [HttpGet("{id}")]
        [ProducesResponseType(typeof(UsuarioDto), StatusCodes.Status200OK)]
        public async Task<ActionResult<UsuarioDto>> ObtenerUsuarioPorId(int id)
        {
            try
            {
                if (id <= 0)
                    return BadRequest(new { message = "El ID debe ser mayor a cero" });

                var usuario = await _usuarioService.ObtenerUsuarioPorIdAsync(id);

                if (usuario == null)
                    return NotFound(new { message = $"No se encontró el usuario con ID {id}" });

                return Ok(usuario);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener usuario");
                return StatusCode(500, new { message = "Error interno" });
            }
        }

        // ================= T-34: CREAR USUARIO =================

        [HttpPost]
        [ProducesResponseType(typeof(UsuarioDto), StatusCodes.Status201Created)]
        public async Task<ActionResult<UsuarioDto>> CrearUsuario([FromBody] CrearUsuarioDto dto)
        {
            try
            {
                if (dto == null)
                    return BadRequest(new { message = "Los datos del usuario son requeridos" });

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

                return CreatedAtAction(nameof(ObtenerUsuarioPorId),
                    new { id = usuario.Id }, usuario);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear usuario");
                return StatusCode(500, new { message = "Error inesperado" });
            }
        }

        // ================= ENDPOINTS PENDIENTES (SIN ASYNC) =================

        /// <summary>
        /// Actualiza la información de un usuario existente.
        /// </summary>
        [HttpPut("{id}")]
        [ProducesResponseType(typeof(UsuarioDto), StatusCodes.Status200OK)]
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
                _logger.LogError(ex, "Error al actualizar usuario");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "Error interno al actualizar usuario" });
            }
        }

        /// <summary>
        /// Desactiva un usuario (soft delete).
        /// </summary>
        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
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
                _logger.LogError(ex, "Error al desactivar usuario");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "Error interno al desactivar usuario" });
            }
        }

        /// <summary>
        /// Activa un usuario previamente desactivado.
        /// </summary>
        [HttpPut("{id}/activar")]
        public ActionResult ActivarUsuario(int id)
        {
            return StatusCode(StatusCodes.Status501NotImplemented, new
            {
                message = "Funcionalidad pendiente de implementar"
            });
        }

        /// <summary>
        /// Cambia el rol de un usuario.
        /// </summary>
        [HttpPut("{id}/rol")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
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
                _logger.LogError(ex, "Error al cambiar rol");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "Error interno al cambiar el rol" });
            }
        }
    }
}
