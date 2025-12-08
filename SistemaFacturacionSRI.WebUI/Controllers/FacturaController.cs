using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaFacturacionSRI.Domain.DTOs.Common;
using SistemaFacturacionSRI.Domain.DTOs.Factura;
using SistemaFacturacionSRI.Domain.Interfaces.Services;
using SistemaFacturacionSRI.Domain.Enums;
using SistemaFacturacionSRI.WebUI.Authorization;
using SistemaFacturacionSRI.Domain.Interfaces;
using SistemaFacturacionSRI.Domain.Interfaces.Repositories;
using Microsoft.AspNetCore.Hosting;
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
        private readonly IPdfGeneratorService _pdfGeneratorService;
        private readonly IWebHostEnvironment _environment;
        private readonly ISriIntegracionService _sriIntegracionService;
        private readonly IEmailFacturaService _emailFacturaService;
        private readonly ILogger<FacturaController> _logger;
        private readonly IXmlGeneratorService _xmlGeneratorService;
        private readonly ILoteService _loteService;
        private readonly IFacturaRepository _facturaRepository;

        public FacturaController(
            IFacturaService facturaService,
            IPdfGeneratorService pdfGeneratorService,
            IWebHostEnvironment environment,
            ISriIntegracionService sriIntegracionService,
            IEmailFacturaService emailFacturaService,
            ILogger<FacturaController> logger,
            IXmlGeneratorService xmlGeneratorService,
            ILoteService loteService,
            IFacturaRepository facturaRepository)
        {
            _facturaService = facturaService;
            _pdfGeneratorService = pdfGeneratorService;
            _environment = environment;
            _sriIntegracionService = sriIntegracionService;
            _emailFacturaService = emailFacturaService;
            _logger = logger;
            _xmlGeneratorService = xmlGeneratorService;
            _loteService = loteService;
            _facturaRepository = facturaRepository;
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
                _logger.LogInformation("═══════════════════════════════════════════════════════════");
                _logger.LogInformation("🆕 INICIANDO CREACIÓN DE FACTURA");
                _logger.LogInformation("═══════════════════════════════════════════════════════════");

                // 1. Validación inicial de modelo
                if (dto == null)
                {
                    _logger.LogWarning("❌ Datos de factura nulos");
                    return BadRequest(new { message = "Los datos de la factura son requeridos" });
                }

                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("❌ Modelo inválido: {Errores}",
                        string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));

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
                    _logger.LogWarning("❌ Factura sin detalles");
                    return BadRequest(new
                    {
                        message = "La factura debe tener al menos un producto"
                    });
                }

                _logger.LogInformation("📋 Detalles recibidos: {CantidadDetalles}", dto.Detalles.Count);

                // Validar cantidades positivas
                var detallesInvalidos = dto.Detalles
                    .Where(d => d.Cantidad <= 0 || d.PrecioUnitario <= 0)
                    .ToList();

                if (detallesInvalidos.Any())
                {
                    _logger.LogWarning("❌ Detalles con cantidades o precios inválidos: {Cantidad}", detallesInvalidos.Count);

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

                _logger.LogInformation("👤 Usuario autenticado: {UsuarioId}", usuarioId);
                _logger.LogInformation("👥 Cliente seleccionado: {ClienteId}", dto.ClienteId);
                _logger.LogInformation("📦 Cantidad de productos: {CantidadProductos}", dto.Detalles.Count);

                // 4. Llamar al servicio de creación (T-020 de Pedro)
                _logger.LogInformation("🔄 Llamando a FacturaService.CrearFacturaAsync...");
                var factura = await _facturaService.CrearFacturaAsync(dto, usuarioId);

                _logger.LogInformation("✅ Factura creada exitosamente en BD");
                _logger.LogInformation("   - ID: {FacturaId}", factura.Id);
                _logger.LogInformation("   - Número: {NumeroFactura}", factura.NumeroFactura);
                _logger.LogInformation("   - Clave Acceso: {ClaveAcceso}", factura.ClaveAcceso);
                _logger.LogInformation("   - Total: ${Total:F2}", factura.Total);
                _logger.LogInformation("   - Estado: {Estado}", factura.Estado);

                // 5. Generar XML del comprobante inmediatamente después de crear la factura
                // En CrearFactura(), REEMPLAZAR la sección de validación (líneas 188-210):

                // 5. Generar XML del comprobante inmediatamente después de crear la factura
                try
                {
                    _logger.LogInformation("📄 Generando XML para factura {FacturaId}...", factura.Id);

                    // Generar XML
                    var xmlContent = await _xmlGeneratorService.GenerarXmlFacturaAsync(factura.Id);

                    _logger.LogInformation("✅ XML generado correctamente");
                    _logger.LogInformation("   - Tamaño: {Tamaño} caracteres", xmlContent.Length);

                    // ⚠️ VALIDACIÓN XSD DESHABILITADA TEMPORALMENTE
                    // TODO: Agregar archivo XSD en Resources/XSD/factura_v2.1.0.xsd
                    /*
                    _logger.LogInformation("🔍 Validando XML contra esquema XSD...");
                    var (esValido, errores) = await _xmlGeneratorService.ValidarXmlContraEsquemaAsync(xmlContent);

                    if (!esValido)
                    {
                        _logger.LogError("❌ XML no válido según esquema XSD");
                        foreach (var error in errores)
                        {
                            _logger.LogError("   - {Error}", error);
                        }
                        throw new InvalidOperationException("XML no válido: " + string.Join(", ", errores));
                    }

                    _logger.LogInformation("✅ XML válido según esquema XSD");
                    */
                    _logger.LogWarning("⚠️ Validación XSD omitida - agregar archivo factura_v2.1.0.xsd para habilitar");

                    // Guardar XML en archivo
                    _logger.LogInformation("💾 Guardando XML en archivo...");
                    var xmlPath = await _xmlGeneratorService.GuardarXmlEnArchivoAsync(xmlContent, factura.ClaveAcceso);

                    _logger.LogInformation("✅ XML guardado exitosamente");
                    _logger.LogInformation("   - Ruta: {XmlPath}", xmlPath);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "❌ Error al generar XML para la factura {FacturaId}", factura.Id);
                    return StatusCode(StatusCodes.Status500InternalServerError, new
                    {
                        message = "Error al generar el archivo XML",
                        tipo = "XmlGenerationError",
                        detalles = ex.Message
                    });
                }

                _logger.LogInformation("═══════════════════════════════════════════════════════════");
                _logger.LogInformation("🎉 FACTURA Y XML CREADOS EXITOSAMENTE - ID: {FacturaId}", factura.Id);
                _logger.LogInformation("═══════════════════════════════════════════════════════════");

                // 6. Retornar resultado 201 Created con Location header
                return CreatedAtAction(
                    nameof(ObtenerFacturaPorId),
                    new { id = factura.Id },
                    new
                    {
                        message = "Factura y XML creados exitosamente",
                        factura = factura,
                        enlaces = new
                        {
                            verDetalle = $"/api/factura/{factura.Id}",
                            descargarXml = $"/api/factura/{factura.Id}/xml",
                            firmarFactura = $"/api/factura/{factura.Id}/firmar",
                            enviarSri = $"/api/factura/{factura.Id}/enviar-sri"
                        }
                    }
                );
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "❌ Error de validación al crear factura");
                return BadRequest(new
                {
                    message = ex.Message,
                    tipo = "ValidationError"
                });
            }
            catch (InvalidOperationException ex)
            {
                // Errores de negocio (cliente inactivo, producto sin stock, etc.)
                _logger.LogWarning(ex, "❌ Error de negocio al crear factura");
                return BadRequest(new
                {
                    message = ex.Message,
                    tipo = "BusinessRuleError"
                });
            }
            catch (KeyNotFoundException ex)
            {
                // Cliente o producto no encontrado
                _logger.LogWarning(ex, "❌ Recurso no encontrado al crear factura");
                return NotFound(new
                {
                    message = ex.Message,
                    tipo = "NotFoundError"
                });
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "❌ Acceso no autorizado al crear factura");
                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💥 ERROR CRÍTICO al crear factura");
                _logger.LogError("   Usuario: {UsuarioId}, Cliente: {ClienteId}",
                    ObtenerUsuarioId(), dto.ClienteId);
                _logger.LogError("   Tipo excepción: {TipoExcepcion}", ex.GetType().Name);
                _logger.LogError("   Mensaje: {Mensaje}", ex.Message);
                _logger.LogError("   InnerException: {InnerException}", ex.InnerException?.Message);

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

                if (!Enum.TryParse(factura.Estado, true, out EstadoFactura estadoActual))
                {
                    return BadRequest(new
                    {
                        message = $"El estado actual de la factura no es válido: {factura.Estado}"
                    });
                }

                // Solo se pueden reenviar facturas DEVUELTA o NO_AUTORIZADA
                if (estadoActual != EstadoFactura.DEVUELTA && 
                    estadoActual != EstadoFactura.NO_AUTORIZADA)
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
                    estadoActual = factura.Estado,
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
                estado = factura.Estado
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

/// <summary>
/// GET /api/factura/{id}/pdf
/// Genera y descarga el PDF RIDE de una factura
/// PERMISOS: Administrador ✅ (cualquier factura) | Vendedor ✅ (solo sus facturas)
/// </summary>
[HttpGet("{id}/pdf")]
[Authorize(Policy = AuthorizationPolicies.AdminOrVendedor)]
[ProducesResponseType(typeof(FileResult), StatusCodes.Status200OK)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
[ProducesResponseType(StatusCodes.Status404NotFound)]
[ProducesResponseType(StatusCodes.Status500InternalServerError)]
public async Task<IActionResult> DescargarPdfRide(int id)
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
                    "Usuario {UsuarioId} intentó descargar PDF de factura {FacturaId} de otro usuario",
                    usuarioId, id);
                
                return Forbid();
            }
        }

        // 3. Generar el PDF RIDE
        byte[] pdfBytes = await _pdfGeneratorService.GenerarRideBytesAsync(id);

        // 4. Generar nombre del archivo para descarga
        string nombreArchivo = string.IsNullOrWhiteSpace(factura.ClaveAcceso)
            ? $"RIDE_{factura.NumeroFactura}.pdf"
            : $"RIDE_{factura.ClaveAcceso}.pdf";

        _logger.LogInformation(
            "Usuario {UsuarioId} descargó PDF RIDE de factura {FacturaId} ({NumeroFactura})",
            ObtenerUsuarioId(), id, factura.NumeroFactura);

        // 5. Retornar el archivo PDF
        return File(
            pdfBytes,
            "application/pdf",
            nombreArchivo
        );
    }
    catch (KeyNotFoundException ex)
    {
        return NotFound(new { message = ex.Message });
    }
    catch (UnauthorizedAccessException ex)
    {
        _logger.LogWarning(ex, "Acceso no autorizado al descargar PDF de factura {FacturaId}", id);
        return Forbid();
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error al generar PDF RIDE de factura {FacturaId}", id);
        return StatusCode(StatusCodes.Status500InternalServerError, new
        {
            message = "Error interno al generar el archivo PDF"
        });
    }
}

/// <summary>
/// GET /api/factura/{id}/pdf/almacenar
/// Genera y almacena el PDF RIDE en el servidor
/// PERMISOS: Administrador ✅ | Vendedor ✅
/// </summary>
[HttpPost("{id}/pdf/almacenar")]
[Authorize(Policy = AuthorizationPolicies.AdminOrVendedor)]
[ProducesResponseType(StatusCodes.Status200OK)]
[ProducesResponseType(StatusCodes.Status404NotFound)]
[ProducesResponseType(StatusCodes.Status500InternalServerError)]
public async Task<IActionResult> AlmacenarPdfRide(int id)
{
    try
    {
        if (id <= 0)
        {
            return BadRequest(new { message = "El ID debe ser mayor a cero" });
        }

        // Generar y almacenar el PDF
        string rutaPdf = await _pdfGeneratorService.GenerarYAlmacenarRideAsync(id, _environment.WebRootPath);

        _logger.LogInformation(
            "PDF RIDE generado y almacenado para factura {FacturaId}. Ruta: {Ruta}",
            id, rutaPdf);

        return Ok(new
        {
            message = "PDF RIDE generado y almacenado exitosamente",
            facturaId = id,
            rutaPdf = rutaPdf,
            urlDescarga = $"/api/factura/{id}/pdf"
        });
    }
    catch (KeyNotFoundException ex)
    {
        return NotFound(new { message = ex.Message });
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error al almacenar PDF RIDE de factura {FacturaId}", id);
        return StatusCode(StatusCodes.Status500InternalServerError, new
        {
            message = "Error interno al almacenar el archivo PDF"
        });
    }
}


/// <summary>
/// T-065: POST /api/factura/{id}/firmar
/// Firma electrónicamente el XML de una factura con certificado digital XADES-BES
/// Genera el XML si no existe, lo firma, almacena el XML firmado y actualiza el estado a FIRMADA
/// PERMISOS: Administrador ✅ | Vendedor ✅ (solo sus facturas)
/// </summary>
[HttpPost("{id}/firmar")]
[Authorize(Policy = AuthorizationPolicies.AdminOrVendedor)]
[ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
[ProducesResponseType(StatusCodes.Status400BadRequest)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
[ProducesResponseType(StatusCodes.Status404NotFound)]
[ProducesResponseType(StatusCodes.Status500InternalServerError)]
public async Task<IActionResult> FirmarFactura(int id)
{
    try
    {
        if (id <= 0)
        {
            return BadRequest(new { message = "El ID debe ser mayor a cero" });
        }

        // 1. Obtener la factura para validar permisos
        var factura = await _facturaService.ObtenerPorIdAsync(id);

        if (factura == null)
        {
            return NotFound(new 
            { 
                message = $"Factura con ID {id} no encontrada" 
            });
        }

        // 2. Validar permisos: Vendedor solo puede firmar sus propias facturas
        if (!EsAdministrador())
        {
            int usuarioId = ObtenerUsuarioId();
            
            if (factura.UsuarioId != usuarioId)
            {
                _logger.LogWarning(
                    "Usuario {UsuarioId} intentó firmar factura {FacturaId} de otro usuario",
                    usuarioId, id);
                
                return Forbid();
            }
        }

        // 3. Validar estado actual de la factura
        if (!Enum.TryParse(factura.Estado, true, out EstadoFactura estadoActual))
        {
            return BadRequest(new
            {
                message = $"El estado actual de la factura no es válido: {factura.Estado}"
            });
        }

        // Solo se pueden firmar facturas en estado BORRADOR o GENERADA
        if (estadoActual != EstadoFactura.BORRADOR && 
            estadoActual != EstadoFactura.GENERADA)
        {
            return BadRequest(new
            {
                message = $"Solo se pueden firmar facturas en estado BORRADOR o GENERADA. Estado actual: {factura.Estado}",
                estadoActual = factura.Estado,
                estadosPermitidos = new[] { "BORRADOR", "GENERADA" }
            });
        }

        // 4. Validar que no esté ya firmada
        if (!string.IsNullOrWhiteSpace(factura.XmlFirmadoPath))
        {
            return BadRequest(new
            {
                message = "La factura ya ha sido firmada anteriormente",
                xmlFirmadoPath = factura.XmlFirmadoPath,
                estadoActual = factura.Estado
            });
        }

        _logger.LogInformation(
            "Iniciando firma electrónica de factura {FacturaId} ({NumeroFactura}). Usuario: {UsuarioId}",
            id, factura.NumeroFactura, ObtenerUsuarioId());

        // 5. Llamar al servicio que genera XML, firma y almacena
        var (xmlPath, xmlFirmadoPath) = await _facturaService.FirmarYAlmacenarXmlAsync(id);

        _logger.LogInformation(
            "Factura {FacturaId} firmada exitosamente. XML: {XmlPath}, XML Firmado: {XmlFirmadoPath}",
            id, xmlPath, xmlFirmadoPath);

        // 6. Obtener la factura actualizada para devolver en la respuesta
        var facturaActualizada = await _facturaService.ObtenerPorIdAsync(id);

        // 7. Retornar respuesta exitosa
        return Ok(new
        {
            message = "Factura firmada electrónicamente de forma exitosa",
            facturaId = id,
            numeroFactura = factura.NumeroFactura,
            claveAcceso = factura.ClaveAcceso,
            estadoAnterior = estadoActual.ToString(),
            estadoActual = facturaActualizada?.Estado ?? "FIRMADA",
            archivos = new
            {
                xmlOriginal = xmlPath,
                xmlFirmado = xmlFirmadoPath,
                urlDescargarXml = $"/api/factura/{id}/xml",
                urlDescargarXmlFirmado = xmlFirmadoPath
            },
            fechaFirma = DateTime.Now,
            siguientePaso = new
            {
                accion = "Enviar al SRI",
                endpoint = $"/api/factura/{id}/enviar-sri",
                descripcion = "La factura está lista para ser enviada al Servicio de Rentas Internas"
            }
        });
    }
    catch (KeyNotFoundException ex)
    {
        _logger.LogWarning(ex, "Factura {FacturaId} no encontrada al intentar firmar", id);
        return NotFound(new { message = ex.Message });
    }
    catch (InvalidOperationException ex)
    {
        // Errores de validación de estado o configuración
        _logger.LogWarning(ex, "Error de validación al firmar factura {FacturaId}", id);
        return BadRequest(new 
        { 
            message = ex.Message,
            tipo = "ValidationError"
        });
    }
    catch (UnauthorizedAccessException ex)
    {
        _logger.LogWarning(ex, "Acceso no autorizado al firmar factura {FacturaId}", id);
        return Forbid();
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error interno al firmar factura {FacturaId}", id);
        
        return StatusCode(StatusCodes.Status500InternalServerError, new
        {
            message = "Error interno del servidor al firmar la factura",
            tipo = "InternalError",
            detalles = ex.Message
        });
    }
}

/// <summary>
/// T-084: POST /api/factura/{id}/enviar-sri
/// Procesa una factura completa: genera XML → firma → envía al SRI → consulta autorización → actualiza BD
/// Usa la integración REAL de Pedro (ISriIntegracionService)
/// PERMISOS: Administrador ✅ | Vendedor ✅ (solo sus facturas)
/// </summary>
[HttpPost("{id}/enviar-sri")]
[Authorize(Policy = AuthorizationPolicies.AdminOrVendedor)]
[ProducesResponseType(typeof(EnviarSriResponseDto), StatusCodes.Status200OK)]
[ProducesResponseType(StatusCodes.Status400BadRequest)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
[ProducesResponseType(StatusCodes.Status404NotFound)]
[ProducesResponseType(StatusCodes.Status500InternalServerError)]
public async Task<IActionResult> EnviarAlSri(int id)
{
    try
    {
        if (id <= 0)
        {
            return BadRequest(new { message = "El ID debe ser mayor a cero" });
        }

        // 1. Obtener la factura para validar permisos y estado
        var factura = await _facturaService.ObtenerPorIdAsync(id);

        if (factura == null)
        {
            return NotFound(new 
            { 
                message = $"Factura con ID {id} no encontrada" 
            });
        }

        // 2. Validar permisos: Vendedor solo puede enviar sus propias facturas
        if (!EsAdministrador())
        {
            int usuarioId = ObtenerUsuarioId();
            
            if (factura.UsuarioId != usuarioId)
            {
                _logger.LogWarning(
                    "Usuario {UsuarioId} intentó enviar factura {FacturaId} de otro usuario al SRI",
                    usuarioId, id);
                
                return Forbid();
            }
        }

        // 3. Validar estado actual
        if (!Enum.TryParse(factura.Estado, true, out EstadoFactura estadoActual))
        {
            return BadRequest(new
            {
                message = $"El estado actual de la factura no es válido: {factura.Estado}"
            });
        }

        // Solo se pueden enviar facturas FIRMADA, DEVUELTA, NO_AUTORIZADA o PENDIENTE
        var estadosPermitidos = new[] 
        { 
            EstadoFactura.FIRMADA, 
            EstadoFactura.DEVUELTA, 
            EstadoFactura.NO_AUTORIZADA,
            EstadoFactura.PENDIENTE
        };

        if (!estadosPermitidos.Contains(estadoActual))
        {
            return BadRequest(new
            {
                message = $"Solo se pueden enviar facturas FIRMADA, DEVUELTA, NO_AUTORIZADA o PENDIENTE. Estado actual: {factura.Estado}",
                estadoActual = factura.Estado,
                estadosPermitidos = estadosPermitidos.Select(e => e.ToString()),
                sugerencia = estadoActual == EstadoFactura.BORRADOR 
                    ? "Debe firmar la factura antes de enviarla al SRI"
                    : "El estado actual no permite el envío al SRI"
            });
        }

        _logger.LogInformation(
            "════════════════════════════════════════════════════════════════");
        _logger.LogInformation(
            "Iniciando envío de factura {FacturaId} ({NumeroFactura}) al SRI", 
            id, factura.NumeroFactura);
        _logger.LogInformation(
            "Usuario: {UsuarioId}, Estado actual: {Estado}", 
            ObtenerUsuarioId(), factura.Estado);
        _logger.LogInformation(
            "════════════════════════════════════════════════════════════════");

        // 4. Llamar al servicio de integración SRI de Pedro (T-082)
        var resultado = await _sriIntegracionService.ProcesarFacturaCompletaAsync(id);

        // 5. Mapear resultado a DTO de respuesta
        var response = EnviarSriResponseDto.MapearDesde(resultado, factura.NumeroFactura);

        // 6. Logging del resultado
        if (resultado.Exitoso)
        {
            _logger.LogInformation(
                "════════════════════════════════════════════════════════════════");
            _logger.LogInformation(
                "🎉 Factura {FacturaId} AUTORIZADA exitosamente por el SRI", id);
            _logger.LogInformation(
                "Número de Autorización: {NumeroAutorizacion}", resultado.NumeroAutorizacion);
            _logger.LogInformation(
                "Tiempo total: {TiempoTotal:F2}s, Intentos: {Intentos}",
                resultado.TiempoTotal.TotalSeconds, resultado.TotalIntentos);
            _logger.LogInformation(
                "════════════════════════════════════════════════════════════════");
        }
        else
        {
            _logger.LogWarning(
                "════════════════════════════════════════════════════════════════");
            _logger.LogWarning(
                "⚠️ Factura {FacturaId} NO AUTORIZADA por el SRI", id);
            _logger.LogWarning(
                "Estado final: {EstadoFinal}", resultado.EstadoFinal);
            _logger.LogWarning(
                "Error: {Error}", resultado.MensajeError);
            _logger.LogWarning(
                "Tiempo total: {TiempoTotal:F2}s, Intentos: {Intentos}",
                resultado.TiempoTotal.TotalSeconds, resultado.TotalIntentos);
            _logger.LogWarning(
                "════════════════════════════════════════════════════════════════");
        }

        // 7. Retornar respuesta
        return Ok(response);
    }
    catch (KeyNotFoundException ex)
    {
        _logger.LogWarning(ex, "Factura {FacturaId} no encontrada al intentar enviar al SRI", id);
        return NotFound(new { message = ex.Message });
    }
    catch (InvalidOperationException ex)
    {
        _logger.LogWarning(ex, "Error de validación al enviar factura {FacturaId} al SRI", id);
        return BadRequest(new 
        { 
            message = ex.Message,
            tipo = "ValidationError"
        });
    }
    catch (UnauthorizedAccessException ex)
    {
        _logger.LogWarning(ex, "Acceso no autorizado al enviar factura {FacturaId} al SRI", id);
        return Forbid();
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "❌ Error crítico al enviar factura {FacturaId} al SRI", id);
        
        return StatusCode(StatusCodes.Status500InternalServerError, new
        {
            message = "Error interno del servidor al enviar la factura al SRI",
            tipo = "InternalError",
            detalles = ex.Message
        });
    }
}

/// <summary>
/// POST /api/factura/{id}/enviar-correo
    /// Envía la factura por correo al cliente (sin importar el estado SRI)
    /// PERMISOS: Administrador ✅ | Vendedor ✅ (solo sus facturas)
    /// </summary>
    [HttpPost("{id}/enviar-correo")]
    [Authorize(Policy = AuthorizationPolicies.AdminOrVendedor)]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> EnviarCorreoCliente(int id)
    {
        try
        {
            _logger.LogInformation("📧 Iniciando envío de correo para factura {FacturaId}", id);

            // 1. Verificar que la factura existe
            var factura = await _facturaService.ObtenerPorIdAsync(id);
            if (factura == null)
            {
                _logger.LogWarning("❌ Factura {FacturaId} no encontrada", id);
                return NotFound(new { message = $"Factura {id} no encontrada" });
            }

            // 2. Validar permisos: Vendedor solo puede enviar sus propias facturas
            if (!EsAdministrador())
            {
                var usuarioId = ObtenerUsuarioId();
                if (factura.UsuarioId != usuarioId)
                {
                    _logger.LogWarning("❌ Usuario {UsuarioId} intentó enviar factura {FacturaId} que no le pertenece", 
                        usuarioId, id);
                    return Forbid();
                }
            }

            // 3. Verificar que el cliente tiene email
            if (string.IsNullOrEmpty(factura.Cliente?.Email))
            {
                _logger.LogWarning("❌ Cliente de factura {FacturaId} no tiene email registrado", id);
                return BadRequest(new 
                { 
                    message = "El cliente no tiene un email registrado",
                    tipo = "EmailNotFound"
                });
            }

            // 4. Verificar si el correo ya fue enviado previamente
            bool esPrimerEnvio = !factura.CorreoEnviado;
            
            if (factura.CorreoEnviado)
            {
                _logger.LogInformation("⚠️ Factura {FacturaId} ya fue enviada previamente el {FechaEnvio}. Reenviando...", 
                    id, factura.FechaEnvioCorreo);
            }

            // 5. Enviar correo usando el servicio existente
            await _emailFacturaService.EnviarFacturaAsync(id);

            _logger.LogInformation("✅ Correo enviado exitosamente para factura {FacturaId} a {Email}", 
                id, factura.Cliente.Email);

            // 6. Reducir stock SOLO la primera vez que se envía el correo
            bool stockReducido = false;
            if (esPrimerEnvio)
            {
                _logger.LogInformation("📦 Iniciando reducción de stock para factura {FacturaId} (primer envío)", id);
                
                foreach (var detalle in factura.Detalles)
                {
                    if (detalle.ProductoId > 0 && detalle.Cantidad > 0)
                    {
                        try
                        {
                            await _loteService.ReducirStockProductoAsync(detalle.ProductoId, detalle.Cantidad);
                            _logger.LogInformation(
                                "✅ Stock reducido: Producto {ProductoId}, Cantidad: {Cantidad}",
                                detalle.ProductoId, detalle.Cantidad);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(
                                ex,
                                "⚠️ No se pudo reducir stock para producto {ProductoId} en factura {FacturaId}: {Error}",
                                detalle.ProductoId, id, ex.Message);
                            // Continuar con los demás productos aunque uno falle
                        }
                    }
                }

                _logger.LogInformation("✅ Proceso de reducción de stock completado para factura {FacturaId}", id);
                stockReducido = true;

                // 7. Marcar la factura como enviada por correo en la base de datos
                var facturaEntidad = await _facturaRepository.ObtenerPorIdAsync(id);
                if (facturaEntidad != null)
                {
                    facturaEntidad.CorreoEnviado = true;
                    facturaEntidad.FechaEnvioCorreo = DateTime.Now;
                    await _facturaRepository.ActualizarAsync(facturaEntidad);
                    
                    // Actualizar también el DTO para la respuesta
                    factura.CorreoEnviado = true;
                    factura.FechaEnvioCorreo = facturaEntidad.FechaEnvioCorreo;
                    
                    _logger.LogInformation("✅ Factura {FacturaId} marcada como enviada por correo", id);
                }
            }
            else
            {
                _logger.LogInformation("ℹ️ Stock NO reducido porque el correo ya fue enviado previamente");
            }

            // 8. Construir respuesta
            var avisoEstado = factura.Estado != "AUTORIZADA" 
                ? "⚠️ NOTA: Esta factura NO está autorizada por el SRI. Se envió para registro interno del cliente."
                : "✅ Factura autorizada enviada al cliente.";

            var mensajeEnvio = esPrimerEnvio
                ? $"Correo enviado exitosamente a {factura.Cliente.Email}"
                : $"Correo reenviado exitosamente a {factura.Cliente.Email} (ya fue enviado el {factura.FechaEnvioCorreo:dd/MM/yyyy HH:mm})";

            return Ok(new
            {
                success = true,
                message = mensajeEnvio,
                facturaId = id,
                numeroFactura = factura.NumeroFactura,
                destinatario = factura.Cliente.Email,
                estadoFactura = factura.Estado,
                aviso = avisoEstado,
                esPrimerEnvio = esPrimerEnvio,
                stockActualizado = stockReducido,
                fechaEnvio = factura.FechaEnvioCorreo
            });
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "❌ Factura {FacturaId} no encontrada al enviar correo", id);
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "❌ Error al enviar correo de factura {FacturaId}", id);
            return BadRequest(new 
            { 
                message = ex.Message,
                tipo = "EmailSendError"
            });
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "❌ Acceso no autorizado al enviar correo de factura {FacturaId}", id);
            return Forbid();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error crítico al enviar correo de factura {FacturaId}", id);
            
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                message = "Error interno del servidor al enviar el correo",
                tipo = "InternalError",
                detalles = ex.Message
            });
        }
    }

    /// <summary>
    /// POST /api/factura/{id}/cambiar-estado-pendiente
    /// Cambia el estado de una factura DEVUELTA o NO_AUTORIZADA a PENDIENTE para permitir reenvío
    /// PERMISOS: Administrador ✅ | Vendedor ✅ (solo sus facturas)
    /// </summary>
    [HttpPost("{id}/cambiar-estado-pendiente")]
    [Authorize(Policy = AuthorizationPolicies.AdminOrVendedor)]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CambiarEstadoPendiente(int id)
    {
        try
        {
            _logger.LogInformation("🔄 Cambiando estado a PENDIENTE para factura {FacturaId}", id);

            // 1. Verificar que la factura existe
            var factura = await _facturaService.ObtenerPorIdAsync(id);
            if (factura == null)
            {
                _logger.LogWarning("❌ Factura {FacturaId} no encontrada", id);
                return NotFound(new { message = $"Factura {id} no encontrada" });
            }

            // 2. Validar permisos: Vendedor solo puede modificar sus propias facturas
            if (!EsAdministrador())
            {
                var usuarioId = ObtenerUsuarioId();
                if (factura.UsuarioId != usuarioId)
                {
                    _logger.LogWarning("❌ Usuario {UsuarioId} intentó modificar factura {FacturaId} que no le pertenece", 
                        usuarioId, id);
                    return Forbid();
                }
            }

            // 3. Validar que el estado actual es DEVUELTA o NO_AUTORIZADA
            if (factura.Estado != "DEVUELTA" && factura.Estado != "NO_AUTORIZADA")
            {
                _logger.LogWarning("❌ Factura {FacturaId} tiene estado {Estado}, solo se puede cambiar desde DEVUELTA o NO_AUTORIZADA", 
                    id, factura.Estado);
                return BadRequest(new 
                { 
                    message = $"Solo se puede cambiar a PENDIENTE desde estado DEVUELTA o NO_AUTORIZADA. Estado actual: {factura.Estado}",
                    estadoActual = factura.Estado
                });
            }

            // 4. Cambiar el estado usando el servicio de factura
            await _facturaService.ActualizarEstadoAsync(id, EstadoFactura.PENDIENTE);

            _logger.LogInformation("✅ Estado cambiado a PENDIENTE para factura {FacturaId}", id);

            return Ok(new
            {
                success = true,
                message = "Estado cambiado a PENDIENTE exitosamente",
                facturaId = id,
                numeroFactura = factura.NumeroFactura,
                estadoAnterior = factura.Estado,
                estadoActual = "PENDIENTE",
                aviso = "Ahora puede reenviar la factura al SRI o descargar XML/PDF"
            });
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "❌ Factura {FacturaId} no encontrada", id);
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "❌ Error al cambiar estado de factura {FacturaId}", id);
            return BadRequest(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "❌ Acceso no autorizado al cambiar estado de factura {FacturaId}", id);
            return Forbid();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error crítico al cambiar estado de factura {FacturaId}", id);
            
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                message = "Error interno del servidor al cambiar el estado",
                tipo = "InternalError",
                detalles = ex.Message
            });
        }
    }
    }
}
