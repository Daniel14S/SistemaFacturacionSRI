// WebUI/Controllers/FacturaController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaFacturacionSRI.Application.DTOs.Common;
using SistemaFacturacionSRI.Application.DTOs.Factura;
using SistemaFacturacionSRI.Application.Interfaces.Services;
using SistemaFacturacionSRI.Domain.Enums;
using SistemaFacturacionSRI.WebUI.Authorization;
using System.Security.Claims;

namespace SistemaFacturacionSRI.WebUI.Controllers
{
    /// <summary>
    /// Controlador REST para gestión de facturas electrónicas.
    /// PERMISOS:
    /// - Administrador: Acceso completo
    /// - Vendedor: Crear, listar sus facturas, ver detalles de sus facturas
    /// T-026 a T-032: SPRINT 3 - DÍA 1-2
    /// </summary>
    [Authorize] // Requiere autenticación
    [ApiController]
    [Route("api/[controller]")]
    public class FacturaController : ControllerBase
    {
        private readonly IFacturaService _facturaService;
        private readonly ILogger<FacturaController> _logger;

        public FacturaController(
            IFacturaService facturaService,
            ILogger<FacturaController> logger)
        {
            _facturaService = facturaService;
            _logger = logger;
        }

        // ==================== MÉTODOS AUXILIARES ====================

        /// <summary>
        /// Obtiene el ID del usuario autenticado desde el token JWT
        /// </summary>
        private int ObtenerUsuarioId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value 
                           ?? User.FindFirst("sub")?.Value
                           ?? User.FindFirst("userId")?.Value;
            
            if (int.TryParse(userIdClaim, out int userId))
            {
                return userId;
            }

            throw new UnauthorizedAccessException("No se pudo obtener el ID del usuario");
        }

        /// <summary>
        /// Obtiene el rol del usuario autenticado
        /// </summary>
        private string ObtenerRolUsuario()
        {
            return User.FindFirst(ClaimTypes.Role)?.Value 
                ?? User.FindFirst("role")?.Value 
                ?? string.Empty;
        }

        /// <summary>
        /// Verifica si el usuario es administrador
        /// </summary>
        private bool EsAdministrador()
        {
            return ObtenerRolUsuario().Equals("Administrador", StringComparison.OrdinalIgnoreCase);
        }

        // ==================== ENDPOINTS ====================

        /// <summary>
        /// T-027: POST /api/factura
        /// Crea una nueva factura en estado BORRADOR
        /// PERMISOS: Administrador ✅ | Vendedor ✅
        /// </summary>
        [HttpPost]
        [Authorize(Policy = AuthorizationPolicies.AdminOrVendedor)]
        [ProducesResponseType(typeof(FacturaDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<FacturaDto>> CrearFactura([FromBody] CrearFacturaDto dto)
        {
            try
            {
                if (dto == null)
                {
                    return BadRequest(new { message = "Los datos de la factura son requeridos" });
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

                // Obtener usuario autenticado
                int usuarioId = ObtenerUsuarioId();

                // TODO: Llamar al servicio de creación de factura
                // var factura = await _facturaService.CrearFacturaAsync(dto, usuarioId);
                
                // TEMPORAL: Respuesta simulada hasta que Pedro implemente FacturaService
                _logger.LogInformation(
                    "Solicitud de creación de factura recibida. Usuario: {UsuarioId}, Cliente: {ClienteId}, Detalles: {CantidadDetalles}",
                    usuarioId, dto.ClienteId, dto.Detalles.Count);

                return StatusCode(StatusCodes.Status501NotImplemented, new
                {
                    message = "Endpoint en desarrollo. Esperando implementación de FacturaService (T-020 - Pedro)",
                    datosRecibidos = new
                    {
                        usuarioId,
                        dto.ClienteId,
                        cantidadProductos = dto.Detalles.Count
                    }
                });

                // CÓDIGO FINAL (descomentar cuando Pedro termine T-020):
                /*
                return CreatedAtAction(
                    nameof(ObtenerFacturaPorId),
                    new { id = factura.Id },
                    factura
                );
                */
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Error de validación al crear factura");
                return BadRequest(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear factura");
                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    message = "Error interno del servidor"
                });
            }
        }

        /// <summary>
        /// T-028: GET /api/factura
        /// Lista facturas con filtros y paginación
        /// PERMISOS: Administrador ✅ (ve todas) | Vendedor ✅ (solo sus facturas)
        /// </summary>
        [HttpGet]
        [Authorize(Policy = AuthorizationPolicies.AdminOrVendedor)]
        [ProducesResponseType(typeof(PagedResultDto<FacturaDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<PagedResultDto<FacturaDto>>> ListarFacturas(
            [FromQuery] FiltroFacturaDto filtro)
        {
            try
            {
                // Si es Vendedor, forzar filtro por su propio usuarioId
                if (!EsAdministrador())
                {
                    filtro.UsuarioId = ObtenerUsuarioId();
                }

                var resultado = await _facturaService.ListarFacturasAsync(filtro);

                return Ok(resultado);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al listar facturas");
                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    message = "Error interno al listar facturas"
                });
            }
        }

        /// <summary>
        /// T-029: GET /api/factura/{id}
        /// Obtiene una factura completa por ID
        /// PERMISOS: Administrador ✅ (cualquier factura) | Vendedor ✅ (solo sus facturas)
        /// </summary>
        [HttpGet("{id}")]
        [Authorize(Policy = AuthorizationPolicies.AdminOrVendedor)]
        [ProducesResponseType(typeof(FacturaDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<FacturaDto>> ObtenerFacturaPorId(int id)
        {
            try
            {
                if (id <= 0)
                {
                    return BadRequest(new { message = "El ID debe ser mayor a cero" });
                }

                var factura = await _facturaService.ObtenerPorIdAsync(id);

                if (factura == null)
                {
                    return NotFound(new { message = $"Factura con ID {id} no encontrada" });
                }

                // Validar permisos: Vendedor solo ve sus facturas
                if (!EsAdministrador())
                {
                    int usuarioId = ObtenerUsuarioId();
                    
                    if (factura.UsuarioId != usuarioId)
                    {
                        _logger.LogWarning(
                            "Usuario {UsuarioId} intentó acceder a factura {FacturaId} de otro usuario",
                            usuarioId, id);
                        
                        return Forbid();
                    }
                }

                return Ok(factura);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener factura {FacturaId}", id);
                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    message = "Error interno al obtener factura"
                });
            }
        }

        /// <summary>
        /// T-030: PATCH /api/factura/{id}/anular
        /// Anula una factura (solo Administrador)
        /// PERMISOS: Administrador ✅ | Vendedor ❌
        /// </summary>
        [HttpPatch("{id}/anular")]
        [AdminAuthorize] // Solo Admin
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult> AnularFactura(int id, [FromBody] AnularFacturaDto dto)
        {
            try
            {
                if (id <= 0)
                {
                    return BadRequest(new { message = "El ID debe ser mayor a cero" });
                }

                if (dto == null)
                {
                    return BadRequest(new { message = "Debe proporcionar un motivo para la anulación" });
                }

                int usuarioId = ObtenerUsuarioId();

                var facturaAnulada = await _facturaService.AnularFacturaAsync(id, usuarioId, dto.Motivo);

                _logger.LogInformation(
                    "Factura {FacturaId} anulada por usuario {UsuarioId}. Motivo: {Motivo}",
                    id, usuarioId, dto.Motivo);

                return Ok(new
                {
                    message = "Factura anulada exitosamente",
                    facturaId = id,
                    nuevoEstado = facturaAnulada.Estado.ToString(),
                    fechaAnulacion = DateTime.Now
                });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al anular factura {FacturaId}", id);
                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    message = "Error interno al anular factura"
                });
            }
        }

        /// <summary>
        /// T-031: POST /api/factura/{id}/reenviar
        /// Reenvía una factura rechazada al SRI
        /// PERMISOS: Administrador ✅ | Vendedor ✅ (solo sus facturas)
        /// </summary>
        [HttpPost("{id}/reenviar")]
        [Authorize(Policy = AuthorizationPolicies.AdminOrVendedor)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult> ReenviarFactura(int id)
        {
            try
            {
                if (id <= 0)
                {
                    return BadRequest(new { message = "El ID debe ser mayor a cero" });
                }

                var factura = await _facturaService.ObtenerPorIdAsync(id);

                if (factura == null)
                {
                    return NotFound(new { message = $"Factura con ID {id} no encontrada" });
                }

                // Validar permisos: Vendedor solo puede reenviar sus facturas
                if (!EsAdministrador() && factura.UsuarioId != ObtenerUsuarioId())
                {
                    return Forbid();
                }

                // Solo se pueden reenviar facturas DEVUELTA o NO_AUTORIZADA
                if (factura.Estado != EstadoFactura.DEVUELTA && 
                    factura.Estado != EstadoFactura.NO_AUTORIZADA)
                {
                    return BadRequest(new
                    {
                        message = $"Solo se pueden reenviar facturas DEVUELTA o NO_AUTORIZADA. Estado actual: {factura.Estado}"
                    });
                }

                // TODO: Llamar al servicio de integración SRI (T-082 - Pedro)
                // await _sriIntegracionService.ProcesarFacturaCompletaAsync(id);

                _logger.LogInformation("Factura {FacturaId} marcada para reenvío al SRI", id);

                return Ok(new
                {
                    message = "Factura marcada para reenvío al SRI. Consulte el estado en unos minutos.",
                    facturaId = id,
                    estadoActual = factura.Estado.ToString(),
                    nota = "Funcionalidad de reenvío pendiente de implementación (T-082 - Pedro)"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al reenviar factura {FacturaId}", id);
                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    message = "Error interno al reenviar factura"
                });
            }
        }
    }
}