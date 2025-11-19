using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaFacturacionSRI.Application.DTOs.Lote;
using SistemaFacturacionSRI.Application.Interfaces.Services;
using SistemaFacturacionSRI.WebUI.Authorization;

namespace SistemaFacturacionSRI.WebUI.Controllers
{
    /// <summary>
    /// Controlador REST para gestión de lotes de productos.
    /// Endpoints REST para consultar lotes y su información asociada.
    /// 
    /// PERMISOS:
    /// - Administrador: Acceso completo (CRUD)
    /// - Vendedor: Solo lectura (GET)
    /// </summary>
    [Authorize] // Requiere autenticación para TODOS los endpoints
    [ApiController]
    [Route("api/[controller]")]
    public class LoteController : ControllerBase
    {
        private readonly ILoteService _loteService;
        private readonly ILogger<LoteController> _logger;

        public LoteController(
            ILoteService loteService,
            ILogger<LoteController> logger)
        {
            _loteService = loteService;
            _logger = logger;
        }

        // ========== ENDPOINTS DE LECTURA (Administrador Y Vendedor) ==========

        /// <summary>
        /// Lista todos los lotes.
        /// PERMISOS: Administrador ✅ | Vendedor ✅
        /// </summary>
        [HttpGet]
        [Authorize(Policy = AuthorizationPolicies.AdminOrVendedor)]
        [ProducesResponseType(typeof(List<LoteDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<LoteDto>>> GetLotes()
        {
            try
            {
                var lotes = await _loteService.ObtenerTodosAsync();
                return Ok(lotes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener lotes");
                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    message = "Error interno al obtener lotes"
                });
            }
        }

        /// <summary>
        /// Obtiene un lote por su ID.
        /// PERMISOS: Administrador ✅ | Vendedor ✅
        /// </summary>
        [HttpGet("{id}")]
        [Authorize(Policy = AuthorizationPolicies.AdminOrVendedor)]
        [ProducesResponseType(typeof(LoteDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<LoteDto>> GetLote(int id)
        {
            try
            {
                var lote = await _loteService.ObtenerPorIdAsync(id);

                if (lote == null)
                {
                    return NotFound(new { message = $"Lote con ID {id} no encontrado" });
                }

                return Ok(lote);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener lote {LoteId}", id);
                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    message = "Error interno al obtener lote"
                });
            }
        }

        /// <summary>
        /// Obtiene todos los lotes de un producto específico.
        /// PERMISOS: Administrador ✅ | Vendedor ✅
        /// </summary>
        [HttpGet("producto/{productoId}")]
        [Authorize(Policy = AuthorizationPolicies.AdminOrVendedor)]
        [ProducesResponseType(typeof(List<LoteDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<LoteDto>>> GetLotesPorProducto(int productoId)
        {
            try
            {
                var lotes = await _loteService.ObtenerPorProductoAsync(productoId);
                return Ok(lotes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener lotes del producto {ProductoId}", productoId);
                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    message = "Error interno al obtener lotes del producto"
                });
            }
        }

        // ========== ENDPOINTS DE ESCRITURA (Solo Administrador) ==========

        /// <summary>
        /// Crea un nuevo lote.
        /// PERMISOS: Administrador ✅ | Vendedor ❌
        /// </summary>
        [HttpPost]
        [AdminAuthorize] // Solo administradores
        [ProducesResponseType(typeof(LoteDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<LoteDto>> CrearLote([FromBody] CrearLoteDto dto)
        {
            try
            {
                if (dto == null)
                {
                    return BadRequest(new { message = "Los datos del lote son requeridos" });
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

                var loteCreado = await _loteService.CrearAsync(dto);

                return CreatedAtAction(
                    nameof(GetLote),
                    new { id = loteCreado.LoteId },
                    loteCreado
                );
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear lote");
                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    message = "Error interno al crear lote"
                });
            }
        }

        /// <summary>
        /// Actualiza un lote existente.
        /// PERMISOS: Administrador ✅ | Vendedor ❌
        /// </summary>
        [HttpPut("{id}")]
        [AdminAuthorize] // Solo administradores
        [ProducesResponseType(typeof(LoteDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<LoteDto>> ActualizarLote(int id, [FromBody] ActualizarLoteDto dto)
        {
            try
            {
                if (dto == null)
                {
                    return BadRequest(new { message = "Los datos del lote son requeridos" });
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

                if (dto.LoteId != 0 && dto.LoteId != id)
                {
                    return BadRequest(new { message = "El ID de la ruta debe coincidir con el ID del lote" });
                }

                dto.LoteId = id;
                var loteActualizado = await _loteService.ActualizarAsync(dto);

                return Ok(loteActualizado);
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
                _logger.LogError(ex, "Error al actualizar lote");
                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    message = "Error interno al actualizar lote"
                });
            }
        }

        /// <summary>
        /// Elimina un lote (soft delete).
        /// PERMISOS: Administrador ✅ | Vendedor ❌
        /// </summary>
        [HttpDelete("{id}")]
        [AdminAuthorize] // Solo administradores
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult> EliminarLote(int id)
        {
            try
            {
                if (id <= 0)
                {
                    return BadRequest(new { message = "El ID debe ser mayor a cero" });
                }

                await _loteService.EliminarAsync(id);
                return NoContent();
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al eliminar lote");
                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    message = "Error interno al eliminar lote"
                });
            }
        }
    }
}