using Microsoft.AspNetCore.Mvc;
using SistemaFacturacionSRI.Application.Interfaces.Services;
using SistemaFacturacionSRI.Application.DTOs;
using SistemaFacturacionSRI.Application.DTOs.Producto;

namespace SistemaFacturacionSRI.WebUI.Controllers
{
    /// <summary>
    /// Controller para gestionar operaciones CRUD de productos.
    /// CAPA DE PRESENTACIÓN en Arquitectura Onion.
    /// Solo orquesta las llamadas a la capa de Application (IProductoService).
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class ProductoController : ControllerBase
    {
        // ============================================
        // INYECCIÓN DE DEPENDENCIAS (Arquitectura Onion)
        // ============================================
        
        /// <summary>
        /// Servicio de la capa Application que contiene la lógica de negocio.
        /// El Controller NO debe tener lógica, solo delegar.
        /// </summary>
        private readonly IProductoService _productoService;
        
        /// <summary>
        /// Logger para registrar eventos del controller.
        /// </summary>
        private readonly ILogger<ProductoController> _logger;

        /// <summary>
        /// Constructor del controller.
        /// ASP.NET Core inyecta automáticamente las dependencias configuradas en Program.cs
        /// </summary>
        /// <param name="productoService">Servicio de productos de la capa Application</param>
        /// <param name="logger">Logger para registrar eventos</param>
        public ProductoController(
            IProductoService productoService,
            ILogger<ProductoController> logger)
        {
            _productoService = productoService ?? throw new ArgumentNullException(nameof(productoService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            
            _logger.LogInformation("ProductoController inicializado correctamente");
        }

        // ============================================
        // ENDPOINTS DE LA API REST
        // ============================================

        /// <summary>
        /// GET /api/producto
        /// Obtiene la lista de todos los productos activos CON LOTE PRIORITARIO.
        /// El lote prioritario es el que tiene la fecha de expiración más cercana.
        /// </summary>
        /// <returns>Lista de productos con información del lote más próximo a vencer</returns>
        /// <response code="200">Lista obtenida exitosamente</response>
        /// <response code="500">Error interno del servidor</response>
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<ProductoDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<IEnumerable<ProductoDto>>> ObtenerTodos()
        {
            try
            {
                _logger.LogInformation("GET /api/producto - Obteniendo todos los productos con lote prioritario");

                // CAMBIO CRÍTICO: Usar método que incluye lote prioritario
                // Esto calcula automáticamente cuál lote está más próximo a vencer
                var productos = await _productoService.ObtenerTodosConLotePrioritarioAsync();
                
                _logger.LogInformation("Se obtuvieron {Count} productos con lote prioritario", productos.Count());
                
                return Ok(productos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener la lista de productos");
                return StatusCode(500, new 
                { 
                    error = "Error interno al obtener productos",
                    mensaje = "Ocurrió un error inesperado. Por favor contacte al administrador."
                });
            }
        }

        /// <summary>
        /// GET /api/producto/{id}
        /// Obtiene un producto específico por su ID.
        /// </summary>
        /// <param name="id">ID del producto</param>
        /// <returns>Producto encontrado</returns>
        /// <response code="200">Producto encontrado</response>
        /// <response code="400">ID inválido</response>
        /// <response code="404">Producto no encontrado</response>
        /// <response code="500">Error interno del servidor</response>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(ProductoDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<ProductoDto>> ObtenerPorId(int id)
        {
            try
            {
                _logger.LogInformation("GET /api/producto/{Id} - Buscando producto", id);

                // Validación básica de entrada
                if (id <= 0)
                {
                    _logger.LogWarning("Intento de obtener producto con ID inválido: {Id}", id);
                    return BadRequest(new { error = "El ID debe ser mayor a 0" });
                }

                // Delegar a la capa Application
                var producto = await _productoService.ObtenerPorIdAsync(id);
                
                if (producto == null)
                {
                    _logger.LogWarning("Producto con ID {Id} no encontrado", id);
                    return NotFound(new { error = $"Producto con ID {id} no encontrado" });
                }

                _logger.LogInformation("Producto {Id} obtenido exitosamente", id);
                return Ok(producto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener producto {Id}", id);
                return StatusCode(500, new 
                { 
                    error = "Error interno al obtener producto",
                    mensaje = "Ocurrió un error inesperado. Por favor contacte al administrador."
                });
            }
        }

        /// <summary>
        /// POST /api/producto
        /// Crea un nuevo producto en el sistema.
        /// </summary>
        /// <param name="dto">Datos del producto a crear</param>
        /// <returns>Producto creado con su ID asignado</returns>
        /// <response code="201">Producto creado exitosamente</response>
        /// <response code="400">Datos inválidos</response>
        /// <response code="500">Error interno del servidor</response>
        [HttpPost]
        [ProducesResponseType(typeof(ProductoDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<ProductoDto>> Crear([FromBody] CrearProductoDto dto)
        {
            try
            {
                _logger.LogInformation("POST /api/producto - Creando nuevo producto: {Codigo}", dto?.Codigo);

                // Validar que el DTO no sea nulo
                if (dto == null)
                {
                    _logger.LogWarning("Intento de crear producto con datos nulos");
                    return BadRequest(new { error = "Los datos del producto son requeridos" });
                }

                // Validar ModelState (Data Annotations del DTO)
                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("Validación fallida al crear producto: {Errors}", 
                        string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
                    return BadRequest(ModelState);
                }

                // Delegar a la capa Application
                // La validación de código único se hace en ProductoService
                var productoCreado = await _productoService.CrearAsync(dto);
                
                _logger.LogInformation("Producto creado exitosamente con ID {Id}", productoCreado.Id);
                
                // Retornar 201 Created con la URL del recurso creado
                return CreatedAtAction(
                    nameof(ObtenerPorId), 
                    new { id = productoCreado.Id }, 
                    productoCreado
                );
            }
            catch (InvalidOperationException ex)
            {
                // Errores de validación de negocio (código duplicado, etc.)
                _logger.LogWarning(ex, "Error de validación al crear producto");
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear producto");
                return StatusCode(500, new 
                { 
                    error = "Error interno al crear producto",
                    mensaje = "Ocurrió un error inesperado. Por favor contacte al administrador."
                });
            }
        }

        /// <summary>
        /// PUT /api/producto/{id}
        /// Actualiza un producto existente.
        /// </summary>
        /// <param name="id">ID del producto a actualizar</param>
        /// <param name="dto">Nuevos datos del producto</param>
        /// <returns>Producto actualizado</returns>
        /// <response code="200">Producto actualizado exitosamente</response>
        /// <response code="400">Datos inválidos o ID no coincide</response>
        /// <response code="404">Producto no encontrado</response>
        /// <response code="500">Error interno del servidor</response>
        [HttpPut("{id}")]
        [ProducesResponseType(typeof(ProductoDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<ProductoDto>> Actualizar(int id, [FromBody] ActualizarProductoDto dto)
        {
            try
            {
                _logger.LogInformation("PUT /api/producto/{Id} - Actualizando producto", id);

                // Validación básica
                if (id <= 0)
                {
                    _logger.LogWarning("Intento de actualizar con ID inválido: {Id}", id);
                    return BadRequest(new { error = "El ID debe ser mayor a 0" });
                }

                if (dto == null)
                {
                    _logger.LogWarning("Intento de actualizar producto {Id} con datos nulos", id);
                    return BadRequest(new { error = "Los datos del producto son requeridos" });
                }

                // Validar que el ID de la ruta coincida con el ID del DTO
                if (id != dto.Id)
                {
                    _logger.LogWarning("ID de ruta {RouteId} no coincide con ID del DTO {DtoId}", id, dto.Id);
                    return BadRequest(new { error = "El ID de la ruta no coincide con el ID del producto" });
                }

                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("Validación fallida al actualizar producto {Id}", id);
                    return BadRequest(ModelState);
                }

                // Delegar a la capa Application
                var productoActualizado = await _productoService.ActualizarAsync(dto);
                
                _logger.LogInformation("Producto {Id} actualizado exitosamente", id);
                return Ok(productoActualizado);
            }
            catch (KeyNotFoundException ex)
            {
                // Producto no existe
                _logger.LogWarning(ex, "Producto {Id} no encontrado para actualizar", id);
                return NotFound(new { error = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                // Errores de validación de negocio
                _logger.LogWarning(ex, "Error de validación al actualizar producto {Id}", id);
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al actualizar producto {Id}", id);
                return StatusCode(500, new 
                { 
                    error = "Error interno al actualizar producto",
                    mensaje = "Ocurrió un error inesperado. Por favor contacte al administrador."
                });
            }
        }

        /// <summary>
        /// DELETE /api/producto/{id}
        /// Elimina lógicamente un producto (lo marca como inactivo).
        /// SOLO se puede eliminar si NO tiene stock disponible.
        /// </summary>
        /// <param name="id">ID del producto a eliminar</param>
        /// <returns>Confirmación de eliminación</returns>
        /// <response code="200">Producto eliminado exitosamente</response>
        /// <response code="400">ID inválido o producto tiene stock</response>
        /// <response code="404">Producto no encontrado</response>
        /// <response code="500">Error interno del servidor</response>
        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult> Eliminar(int id)
        {
            try
            {
                _logger.LogInformation("DELETE /api/producto/{Id} - Eliminando producto", id);

                // Validación básica
                if (id <= 0)
                {
                    _logger.LogWarning("Intento de eliminar con ID inválido: {Id}", id);
                    return BadRequest(new { error = "El ID debe ser mayor a 0" });
                }

                // Delegar a la capa Application
                // La validación de stock se hace en ProductoService.EliminarAsync()
                await _productoService.EliminarAsync(id);
                
                _logger.LogInformation("Producto {Id} eliminado exitosamente", id);
                
                return Ok(new 
                { 
                    mensaje = "Producto eliminado exitosamente",
                    id 
                });
            }
            catch (KeyNotFoundException ex)
            {
                // Producto no existe
                _logger.LogWarning(ex, "Producto {Id} no encontrado para eliminar", id);
                return NotFound(new { error = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                // Producto tiene stock disponible
                _logger.LogWarning(ex, "No se puede eliminar producto {Id}: tiene stock", id);
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al eliminar producto {Id}", id);
                return StatusCode(500, new 
                { 
                    error = "Error interno al eliminar producto",
                    mensaje = "Ocurrió un error inesperado. Por favor contacte al administrador."
                });
            }
        }

        /// <summary>
        /// PUT /api/producto/{id}/reactivar
        /// Reactiva lógicamente un producto previamente inactivo.
        /// </summary>
        [HttpPut("{id}/reactivar")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Reactivar(int id)
        {
            try
            {
                _logger.LogInformation("PUT /api/producto/{Id}/reactivar - Reactivando producto", id);

                if (id <= 0)
                {
                    _logger.LogWarning("Intento de reactivar producto con ID inválido: {Id}", id);
                    return BadRequest(new { error = "El ID debe ser mayor a 0" });
                }

                await _productoService.ReactivarAsync(id);

                return Ok(new { mensaje = "Producto reactivado correctamente" });
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "Producto {Id} no encontrado para reactivar", id);
                return NotFound(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al reactivar producto {Id}", id);
                return StatusCode(500, new
                {
                    error = "Error interno al reactivar producto",
                    mensaje = "Ocurrió un error inesperado. Por favor contacte al administrador."
                });
            }
        }

        // ============================================
        // ENDPOINTS ADICIONALES (ÚTILES PARA EL SISTEMA)
        // ============================================

        /// <summary>
        /// GET /api/producto/buscar?termino=laptop
        /// Busca productos por nombre o código.
        /// </summary>
        /// <param name="termino">Término de búsqueda</param>
        /// <returns>Lista de productos que coinciden con el término</returns>
        [HttpGet("search")]
        [ProducesResponseType(typeof(IEnumerable<ProductoDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<IEnumerable<ProductoDto>>> Buscar([FromQuery] string termino)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(termino))
                {
                    return BadRequest(new { error = "El término de búsqueda es requerido" });
                }

                _logger.LogInformation("Buscando productos con término: {Termino}", termino);
                
                var productos = await _productoService.SearchByCodeOrNameAsync(termino);
                
                return Ok(productos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al buscar productos");
                return StatusCode(500, new { error = "Error en búsqueda" });
            }
        }

        /// <summary>
        /// GET /api/producto/con-stock
        /// Obtiene solo los productos que tienen stock disponible.
        /// </summary>
        /// <returns>Lista de productos con stock</returns>
        [HttpGet("con-stock")]
        [ProducesResponseType(typeof(IEnumerable<ProductoDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<ProductoDto>>> ObtenerConStock()
        {
            try
            {
                _logger.LogInformation("Obteniendo productos con stock disponible");
                
                var productos = await _productoService.ObtenerProductosConStockAsync();
                
                return Ok(productos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener productos con stock");
                return StatusCode(500, new { error = "Error al obtener productos con stock" });
            }
        }
    }
}