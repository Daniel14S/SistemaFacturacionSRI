using Microsoft.AspNetCore.Mvc;
using SistemaFacturacionSRI.Application.DTOs.Categoria;
using SistemaFacturacionSRI.Application.Interfaces.Services;

namespace SistemaFacturacionSRI.WebUI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CategoriaController : ControllerBase
    {
        private readonly ICategoriaService _categoriaService;
        private readonly ILogger<CategoriaController> _logger;

        public CategoriaController(ICategoriaService categoriaService, ILogger<CategoriaController> logger)
        {
            _categoriaService = categoriaService;
            _logger = logger;
        }

        /// <summary>
        /// Obtiene todas las categorías
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<CategoriaDto>>> ObtenerTodas()
        {
            try
            {
                var categorias = await _categoriaService.ObtenerTodasAsync();
                return Ok(categorias);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener todas las categorías");
                return StatusCode(500, "Error interno del servidor");
            }
        }

        /// <summary>
        /// Obtiene solo las categorías activas
        /// </summary>
        [HttpGet("activas")]
        public async Task<ActionResult<IEnumerable<CategoriaDto>>> ObtenerActivas()
        {
            try
            {
                var categorias = await _categoriaService.ObtenerActivasAsync();
                return Ok(categorias);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener categorías activas");
                return StatusCode(500, "Error interno del servidor");
            }
        }

        /// <summary>
        /// Obtiene una categoría por ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<CategoriaDto>> ObtenerPorId(int id)
        {
            try
            {
                var categoria = await _categoriaService.ObtenerPorIdAsync(id);
                if (categoria == null)
                {
                    return NotFound($"No se encontró la categoría con ID {id}");
                }
                return Ok(categoria);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener categoría por ID: {Id}", id);
                return StatusCode(500, "Error interno del servidor");
            }
        }

        /// <summary>
        /// Obtiene una categoría por código
        /// </summary>
        [HttpGet("codigo/{codigo}")]
        public async Task<ActionResult<CategoriaDto>> ObtenerPorCodigo(string codigo)
        {
            try
            {
                var categoria = await _categoriaService.ObtenerPorCodigoAsync(codigo);
                if (categoria == null)
                {
                    return NotFound($"No se encontró la categoría con código '{codigo}'");
                }
                return Ok(categoria);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener categoría por código: {Codigo}", codigo);
                return StatusCode(500, "Error interno del servidor");
            }
        }

        /// <summary>
        /// Busca categorías por término (código o nombre)
        /// </summary>
        [HttpGet("buscar/{termino}")]
        public async Task<ActionResult<IEnumerable<CategoriaDto>>> Buscar(string termino)
        {
            try
            {
                var categorias = await _categoriaService.BuscarAsync(termino);
                return Ok(categorias);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al buscar categorías con término: {Termino}", termino);
                return StatusCode(500, "Error interno del servidor");
            }
        }

        /// <summary>
        /// Crea una nueva categoría
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<CategoriaDto>> Crear([FromBody] CreateCategoriaDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var categoria = await _categoriaService.CrearAsync(dto);
                return CreatedAtAction(nameof(ObtenerPorId), new { id = categoria.Id }, categoria);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear categoría");
                return StatusCode(500, "Error interno del servidor");
            }
        }

        /// <summary>
        /// Actualiza una categoría existente
        /// </summary>
        [HttpPut("{id}")]
        public async Task<ActionResult<CategoriaDto>> Actualizar(int id, [FromBody] UpdateCategoriaDto dto)
        {
            try
            {
                if (id != dto.Id)
                {
                    return BadRequest("El ID de la ruta no coincide con el ID del objeto");
                }

                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var categoria = await _categoriaService.ActualizarAsync(dto);
                if (categoria == null)
                {
                    return NotFound($"No se encontró la categoría con ID {id}");
                }

                return Ok(categoria);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al actualizar categoría con ID: {Id}", id);
                return StatusCode(500, "Error interno del servidor");
            }
        }

        /// <summary>
        /// Elimina una categoría
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<ActionResult> Eliminar(int id)
        {
            try
            {
                var resultado = await _categoriaService.EliminarAsync(id);
                if (!resultado)
                {
                    return NotFound($"No se encontró la categoría con ID {id}");
                }

                return NoContent();
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al eliminar categoría con ID: {Id}", id);
                return StatusCode(500, "Error interno del servidor");
            }
        }

        /// <summary>
        /// Verifica si existe un código de categoría
        /// </summary>
        [HttpGet("existe-codigo/{codigo}")]
        public async Task<ActionResult<bool>> ExisteCodigo(string codigo, [FromQuery] int? idExcluir = null)
        {
            try
            {
                var existe = await _categoriaService.ExisteCodigoAsync(codigo, idExcluir);
                return Ok(existe);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al verificar código de categoría");
                return StatusCode(500, "Error interno del servidor");
            }
        }
    }
}