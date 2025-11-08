using Microsoft.AspNetCore.Mvc;
using SistemaFacturacionSRI.Application.DTOs.Categoria;
using SistemaFacturacionSRI.Application.Interfaces.Services;

namespace SistemaFacturacionSRI.WebUI.Controllers
{
    /// <summary>
    /// Controller para gestionar operaciones de Categorías.
    /// CAPA DE PRESENTACIÓN en Arquitectura Onion.
    /// Solo orquesta las llamadas a la capa de Application (ICategoriaService).
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class CategoriasController : ControllerBase
    {
        private readonly ICategoriaService _categoriaService;
        private readonly ILogger<CategoriasController> _logger;

        public CategoriasController(
            ICategoriaService categoriaService,
            ILogger<CategoriasController> logger)
        {
            _categoriaService = categoriaService ?? throw new ArgumentNullException(nameof(categoriaService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            
            _logger.LogInformation("CategoriasController inicializado correctamente");
        }

        /// <summary>
        /// GET /api/categorias
        /// Obtiene la lista de todas las categorías activas.
        /// </summary>
        /// <returns>Lista de categorías</returns>
        /// <response code="200">Lista obtenida exitosamente</response>
        /// <response code="500">Error interno del servidor</response>
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<CategoriaDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<IEnumerable<CategoriaDto>>> ObtenerTodas()
        {
            try
            {
                _logger.LogInformation("GET /api/categorias - Obteniendo todas las categorías");

                var categorias = await _categoriaService.ObtenerTodasAsync();
                
                _logger.LogInformation("Se obtuvieron {Count} categorías", categorias.Count);
                
                return Ok(categorias);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener la lista de categorías");
                return StatusCode(500, new 
                { 
                    error = "Error interno al obtener categorías",
                    mensaje = "Ocurrió un error inesperado. Por favor contacte al administrador."
                });
            }
        }
    }
}