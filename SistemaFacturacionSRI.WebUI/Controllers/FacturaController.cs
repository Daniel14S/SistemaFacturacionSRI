using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaFacturacionSRI.Domain.DTOs.Common;
using SistemaFacturacionSRI.Domain.DTOs.Factura;
using SistemaFacturacionSRI.Domain.Interfaces.Services;
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
/// IMPLEMENTA: T-020 de Pedro (Lógica completa de creación)
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
        // 1. Validación inicial de modelo
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

        // 2. Validaciones adicionales de negocio
        if (dto.Detalles == null || !dto.Detalles.Any())
        {
            return BadRequest(new 
            { 
                message = "La factura debe tener al menos un producto" 
            });
        }

        // Validar cantidades positivas
        var detallesInvalidos = dto.Detalles
            .Where(d => d.Cantidad <= 0 || d.PrecioUnitario <= 0)
            .ToList();

        if (detallesInvalidos.Any())
        {
            return BadRequest(new
            {
                message = "Todos los productos deben tener cantidad y precio mayor a cero",
                detallesInvalidos = detallesInvalidos.Select(d => new
                {
                    d.ProductoId,
                    d.Cantidad,
                    d.PrecioUnitario
                })
            });
        }

        // 3. Obtener usuario autenticado
        int usuarioId = ObtenerUsuarioId();

        _logger.LogInformation(
            "Iniciando creación de factura. Usuario: {UsuarioId}, Cliente: {ClienteId}, Productos: {CantidadProductos}",
            usuarioId, dto.ClienteId, dto.Detalles.Count);

        // 4. Llamar al servicio de creación (T-020 de Pedro)
        var factura = await _facturaService.CrearFacturaAsync(dto, usuarioId);

        _logger.LogInformation(
            "Factura creada exitosamente. ID: {FacturaId}, Número: {NumeroFactura}, Total: {Total}",
            factura.Id, factura.NumeroFactura, factura.Total);

        // 5. Retornar resultado 201 Created con Location header
        return CreatedAtAction(
            nameof(ObtenerFacturaPorId),
            new { id = factura.Id },
            new
            {
                message = "Factura creada exitosamente",
                factura = factura,
                enlaces = new
                {
                    verDetalle = $"/api/factura/{factura.Id}",
                    descargarXml = $"/api/factura/{factura.Id}/xml",
                    enviarSri = $"/api/factura/{factura.Id}/enviar-sri"
                }
            }
        );
    }
    catch (ArgumentException ex)
    {
        _logger.LogWarning(ex, "Error de validación al crear factura");
        return BadRequest(new 
        { 
            message = ex.Message,
            tipo = "ValidationError"
        });
    }
    catch (InvalidOperationException ex)
    {
        // Errores de negocio (cliente inactivo, producto sin stock, etc.)
        _logger.LogWarning(ex, "Error de negocio al crear factura");
        return BadRequest(new 
        { 
            message = ex.Message,
            tipo = "BusinessRuleError"
        });
    }
    catch (KeyNotFoundException ex)
    {
        // Cliente o producto no encontrado
        _logger.LogWarning(ex, "Recurso no encontrado al crear factura");
        return NotFound(new 
        { 
            message = ex.Message,
            tipo = "NotFoundError"
        });
    }
    catch (UnauthorizedAccessException ex)
    {
        _logger.LogWarning(ex, "Acceso no autorizado al crear factura");
        return Unauthorized(new { message = ex.Message });
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error interno al crear factura. Usuario: {UsuarioId}, Cliente: {ClienteId}", 
            ObtenerUsuarioId(), dto.ClienteId);
        
        return StatusCode(StatusCodes.Status500InternalServerError, new
        {
            message = "Error interno del servidor al crear la factura",
            tipo = "InternalError"
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
            catch (UnauthorizedAccessException )
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


        // ==================== AGREGAR ESTE MÉTODO AL FINAL DE FacturaController.cs ====================
// Ubicación: SistemaFacturacionSRI.WebUI/Controllers/FacturaController.cs
// Agregar ANTES de la última llave de cierre }

/// <summary>
/// T-048: GET /api/factura/{id}/xml
/// Descarga el archivo XML original de una factura
/// PERMISOS: Administrador ✅ (cualquier factura) | Vendedor ✅ (solo sus facturas)
/// </summary>
[HttpGet("{id}/xml")]
[Authorize(Policy = AuthorizationPolicies.AdminOrVendedor)]
[ProducesResponseType(typeof(FileResult), StatusCodes.Status200OK)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
[ProducesResponseType(StatusCodes.Status404NotFound)]
[ProducesResponseType(StatusCodes.Status500InternalServerError)]
public async Task<IActionResult> DescargarXml(int id)
{
    try
    {
        if (id <= 0)
        {
            return BadRequest(new { message = "El ID debe ser mayor a cero" });
        }

        // 1. Obtener la factura
        var factura = await _facturaService.ObtenerPorIdAsync(id);

        if (factura == null)
        {
            return NotFound(new 
            { 
                message = $"Factura con ID {id} no encontrada" 
            });
        }

        // 2. Validar permisos: Vendedor solo puede descargar sus propias facturas
        if (!EsAdministrador())
        {
            int usuarioId = ObtenerUsuarioId();
            
            if (factura.UsuarioId != usuarioId)
            {
                _logger.LogWarning(
                    "Usuario {UsuarioId} intentó descargar XML de factura {FacturaId} de otro usuario",
                    usuarioId, id);
                
                return Forbid();
            }
        }

        // 3. Verificar que el XML existe
        if (string.IsNullOrWhiteSpace(factura.XmlPath))
        {
            return NotFound(new 
            { 
                message = "El archivo XML aún no ha sido generado para esta factura",
                facturaId = id,
                estado = factura.Estado.ToString()
            });
        }

        // 4. Construir la ruta completa del archivo
        // XmlPath ya incluye "comprobantes/xml/[ClaveAcceso].xml"
        var rutaCompleta = Path.Combine(
            Directory.GetCurrentDirectory(), 
            "wwwroot", 
            factura.XmlPath.TrimStart('/', '\\')
        );

        // 5. Verificar que el archivo físico existe
        if (!System.IO.File.Exists(rutaCompleta))
        {
            _logger.LogError(
                "Archivo XML no encontrado en disco. Factura: {FacturaId}, Ruta: {Ruta}",
                id, rutaCompleta);

            return NotFound(new 
            { 
                message = "El archivo XML no fue encontrado en el servidor",
                facturaId = id,
                rutaEsperada = factura.XmlPath
            });
        }

        // 6. Leer el archivo XML
        byte[] xmlBytes = await System.IO.File.ReadAllBytesAsync(rutaCompleta);

        // 7. Generar nombre del archivo para descarga
        string nombreArchivo = string.IsNullOrWhiteSpace(factura.ClaveAcceso)
            ? $"factura_{factura.NumeroFactura}.xml"
            : $"{factura.ClaveAcceso}.xml";

        // Limpiar caracteres especiales del nombre de archivo
        nombreArchivo = nombreArchivo.Replace("-", "");

        _logger.LogInformation(
            "Usuario {UsuarioId} descargó XML de factura {FacturaId} ({NumeroFactura})",
            ObtenerUsuarioId(), id, factura.NumeroFactura);

        // 8. Retornar el archivo XML con headers apropiados
        return File(
            xmlBytes,
            "application/xml",
            nombreArchivo
        );
    }
    catch (UnauthorizedAccessException ex)
    {
        _logger.LogWarning(ex, "Acceso no autorizado al descargar XML de factura {FacturaId}", id);
        return Forbid();
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error al descargar XML de factura {FacturaId}", id);
        return StatusCode(StatusCodes.Status500InternalServerError, new
        {
            message = "Error interno al descargar el archivo XML"
        });
    }
}

    }

    
}