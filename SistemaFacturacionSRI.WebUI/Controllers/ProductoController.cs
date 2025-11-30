using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaFacturacionSRI.Domain.Interfaces.Services;
using SistemaFacturacionSRI.Domain.DTOs.Producto;
using SistemaFacturacionSRI.WebUI.Authorization;

namespace SistemaFacturacionSRI.WebUI.Controllers
{
    /// <summary>
    /// Controlador REST para la gestión de productos.
    /// CAPA DE PRESENTACIÓN en Arquitectura Onion.
    /// Solo orquesta las llamadas a la capa de Application (IProductoService).
    /// 
    /// PERMISOS:
    /// - Administrador: Acceso completo (CRUD)
    /// - Vendedor: Solo lectura (GET)
    /// </summary>
    [Authorize] // Requiere autenticación para TODOS los endpoints
    [ApiController]
    [Route("api/[controller]")]
    public class ProductoController : ControllerBase
    {
        private readonly IProductoService _productoService;
        private readonly ILogger<ProductoController> _logger;

        public ProductoController(
            IProductoService productoService,
            ILogger<ProductoController> logger)
        {
            _productoService = productoService;
            _logger = logger;
        }

        // ========== ENDPOINTS DE LECTURA (Administrador Y Vendedor) ==========

        /// <summary>
        /// Lista todos los productos activos.
        /// PERMISOS: Administrador ✅ | Vendedor ✅
        /// </summary>
        [HttpGet]
        [Authorize(Policy = AuthorizationPolicies.AdminOrVendedor)]
        [ProducesResponseType(typeof(List<ProductoDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<List<ProductoDto>>> GetProductos()
        {
            try
            {
                var productos = await _productoService.ObtenerTodosConLotePrioritarioAsync();
                return Ok(productos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener productos");
                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    message = "Error interno al obtener productos"
                });
            }
        }

        /// <summary>
        /// Obtiene un producto por su ID.
        /// PERMISOS: Administrador ✅ | Vendedor ✅
        /// </summary>
        [HttpGet("{id}")]
        [Authorize(Policy = AuthorizationPolicies.AdminOrVendedor)]
        [ProducesResponseType(typeof(ProductoDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ProductoDto>> GetProducto(int id)
        {
            try
            {
                var producto = await _productoService.ObtenerPorIdAsync(id);

                if (producto == null)
                {
                    return NotFound(new { message = $"Producto con ID {id} no encontrado" });
                }

                return Ok(producto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener producto {ProductoId}", id);
                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    message = "Error interno al obtener producto"
                });
            }
        }

        /// <summary>
        /// Busca productos por nombre.
        /// PERMISOS: Administrador ✅ | Vendedor ✅
        /// </summary>
        [HttpGet("buscar")]
        [Authorize(Policy = AuthorizationPolicies.AdminOrVendedor)]
        [ProducesResponseType(typeof(List<ProductoDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<List<ProductoDto>>> BuscarProductos([FromQuery] string termino)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(termino))
                {
                    return BadRequest(new { message = "El término de búsqueda es requerido" });
                }

                var productos = await _productoService.BuscarPorNombreAsync(termino);
                return Ok(productos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al buscar productos");
                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    message = "Error interno al buscar productos"
                });
            }
        }

        /// <summary>
        /// GET /api/producto/search?termino=laptop
        /// Busca productos por nombre o código.
        /// PERMISOS: Administrador ✅ | Vendedor ✅
        /// </summary>
        [HttpGet("search")]
        [Authorize(Policy = AuthorizationPolicies.AdminOrVendedor)]
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
        /// PERMISOS: Administrador ✅ | Vendedor ✅
        /// </summary>
        [HttpGet("con-stock")]
        [Authorize(Policy = AuthorizationPolicies.AdminOrVendedor)]
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

        // ========== ENDPOINTS DE ESCRITURA (Solo Administrador) ==========

        /// <summary>
        /// Crea un nuevo producto.
        /// PERMISOS: Administrador ✅ | Vendedor ❌
        /// </summary>
        [HttpPost]
        [AdminAuthorize] // Solo administradores
        [ProducesResponseType(typeof(ProductoDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<ProductoDto>> CrearProducto([FromBody] CrearProductoDto dto)
        {
            try
            {
                if (dto == null)
                {
                    return BadRequest(new { message = "Los datos del producto son requeridos" });
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

                var productoCreado = await _productoService.CrearAsync(dto);

                return CreatedAtAction(
                    nameof(GetProducto),
                    new { id = productoCreado.Id },
                    productoCreado
                );
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear producto");
                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    message = "Error interno al crear producto"
                });
            }
        }

        /// <summary>
        /// Actualiza un producto existente.
        /// PERMISOS: Administrador ✅ | Vendedor ❌
        /// </summary>
        [HttpPut("{id}")]
        [AdminAuthorize] // Solo administradores
        [ProducesResponseType(typeof(ProductoDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ProductoDto>> ActualizarProducto(int id, [FromBody] ActualizarProductoDto dto)
        {
            try
            {
                if (dto == null)
                {
                    return BadRequest(new { message = "Los datos del producto son requeridos" });
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

                if (dto.Id != 0 && dto.Id != id)
                {
                    return BadRequest(new { message = "El ID de la ruta debe coincidir con el ID del producto" });
                }

                dto.Id = id;
                var productoActualizado = await _productoService.ActualizarAsync(dto);

                return Ok(productoActualizado);
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
                _logger.LogError(ex, "Error al actualizar producto");
                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    message = "Error interno al actualizar producto"
                });
            }
        }

        /// <summary>
        /// Elimina un producto (soft delete).
        /// PERMISOS: Administrador ✅ | Vendedor ❌
        /// </summary>
        [HttpDelete("{id}")]
        [AdminAuthorize] // Solo administradores
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult> EliminarProducto(int id)
        {
            try
            {
                if (id <= 0)
                {
                    return BadRequest(new { message = "El ID debe ser mayor a cero" });
                }

                await _productoService.EliminarAsync(id);
                return NoContent();
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al eliminar producto");
                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    message = "Error interno al eliminar producto"
                });
            }
        }

        /// <summary>
        /// Reactiva un producto previamente eliminado.
        /// PERMISOS: Administrador ✅ | Vendedor ❌
        /// </summary>
        [HttpPatch("{id}/reactivar")]
        [AdminAuthorize] // Solo administradores
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult> ReactivarProducto(int id)
        {
            try
            {
                if (id <= 0)
                {
                    return BadRequest(new { message = "El ID debe ser mayor a cero" });
                }

                await _productoService.ReactivarAsync(id);
                return NoContent();
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al reactivar producto");
                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    message = "Error interno al reactivar producto"
                });
            }
        }
    }
}