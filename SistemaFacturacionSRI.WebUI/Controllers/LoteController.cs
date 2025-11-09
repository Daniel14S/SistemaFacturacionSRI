using Microsoft.AspNetCore.Mvc;
using SistemaFacturacionSRI.Application.DTOs.Lote;
using SistemaFacturacionSRI.Application.Interfaces.Services;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SistemaFacturacionSRI.WebUI.Controllers
{
    /// <summary>
    /// Endpoints REST para consultar lotes y su información asociada.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class LoteController : ControllerBase
    {
        private readonly ILoteService _loteService;
        private readonly ILogger<LoteController> _logger;

        public LoteController(ILoteService loteService, ILogger<LoteController> logger)
        {
            _loteService = loteService;
            _logger = logger;
            _logger.LogInformation("LoteController inicializado correctamente");
        }

        /// <summary>
        /// GET /api/lote
        /// Obtiene todos los lotes registrados.
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<LoteDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<IEnumerable<LoteDto>>> ObtenerTodos()
        {
            try
            {
                _logger.LogInformation("GET /api/lote - Recuperando lotes");
                var lotes = await _loteService.ObtenerTodosAsync();
                _logger.LogInformation("Se obtuvieron {Count} lotes", lotes.Count());
                return Ok(lotes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener la lista de lotes");
                return StatusCode(500, new
                {
                    error = "Error interno al obtener los lotes",
                    mensaje = "Ocurrió un error inesperado. Por favor contacte al administrador."
                });
            }
        }

        /// <summary>
        /// POST /api/lote
        /// Registra un nuevo lote asociado a un producto existente.
        /// </summary>
        [HttpPost]
        [ProducesResponseType(typeof(LoteDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<LoteDto>> Crear([FromBody] CrearLoteDto dto)
        {
            try
            {
                _logger.LogInformation("POST /api/lote - Creando lote para producto {ProductoId}", dto?.ProductoId);

                if (dto == null)
                {
                    return BadRequest(new { error = "Los datos del lote son requeridos" });
                }

                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var loteCreado = await _loteService.CrearAsync(dto);
                _logger.LogInformation("Lote {LoteId} creado correctamente", loteCreado.LoteId);
                return Created($"api/lote/{loteCreado.LoteId}", loteCreado);
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "Producto no encontrado al crear lote");
                return NotFound(new { error = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Validación de negocio fallida al crear lote");
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inesperado al crear lote");
                return StatusCode(500, new
                {
                    error = "Error interno al crear el lote",
                    mensaje = "Ocurrió un error inesperado. Por favor contacte al administrador."
                });
            }
        }

        /// <summary>
/// GET /api/lote/por-producto/{productoId}
/// Obtiene todos los lotes relacionados con un producto específico.
/// </summary>
[HttpGet("por-producto/{productoId}")]
[ProducesResponseType(typeof(IEnumerable<LoteDto>), StatusCodes.Status200OK)]
[ProducesResponseType(StatusCodes.Status404NotFound)]
[ProducesResponseType(StatusCodes.Status500InternalServerError)]
public async Task<ActionResult<IEnumerable<LoteDto>>> ObtenerPorProducto(int productoId)
{
    try
    {
        _logger.LogInformation("GET /api/lote/por-producto/{ProductoId} - Recuperando lotes", productoId);
        var lotes = await _loteService.ObtenerPorProductoAsync(productoId);

        if (lotes == null || !lotes.Any())
        {
            _logger.LogWarning("No se encontraron lotes para el producto {ProductoId}", productoId);
            return NotFound(new { mensaje = "No se encontraron lotes para este producto." });
        }

        _logger.LogInformation("Se obtuvieron {Count} lotes para el producto {ProductoId}", lotes.Count(), productoId);
        return Ok(lotes);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error al obtener lotes por producto {ProductoId}", productoId);
        return StatusCode(500, new
        {
            error = "Error interno al obtener los lotes",
            mensaje = "Ocurrió un error inesperado. Por favor contacte al administrador."
        });
    }
}


    }
}
