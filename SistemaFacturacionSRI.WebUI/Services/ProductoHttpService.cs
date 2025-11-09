using SistemaFacturacionSRI.Application.DTOs.Producto;
using System.Net.Http.Json;

namespace SistemaFacturacionSRI.WebUI.Services
{
    /// <summary>
    /// Servicio cliente HTTP para consumir la API de Productos.
    /// Este servicio se comunica con ProductoController en el backend.
    /// </summary>
    public class ProductoHttpService
    {
        private readonly HttpClient _httpClient;
        private const string API_BASE_URL = "/api/producto";
        public ProductoHttpService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        /// <summary>
        /// Obtiene todos los productos.
        /// GET /api/productos
        /// </summary>
        public async Task<List<ProductoDto>> ObtenerTodosAsync()
        {
            try
            {
                var productos = await _httpClient.GetFromJsonAsync<List<ProductoDto>>(API_BASE_URL);
                return productos ?? new List<ProductoDto>();
            }
            catch (HttpRequestException ex)
            {
                throw new Exception($"Error al obtener productos: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Obtiene un producto por su ID.
        /// GET /api/productos/{id}
        /// </summary>
        public async Task<ProductoDto?> ObtenerPorIdAsync(int id)
        {
            try
            {
                return await _httpClient.GetFromJsonAsync<ProductoDto>($"{API_BASE_URL}/{id}");
            }
            catch (HttpRequestException ex)
            {
                throw new Exception($"Error al obtener el producto: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Crea un nuevo producto.
        /// POST /api/productos
        /// </summary>
        public async Task<ProductoDto> CrearAsync(CrearProductoDto dto)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync(API_BASE_URL, dto);
                
                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Error al crear producto: {error}");
                }

                var productoCreado = await response.Content.ReadFromJsonAsync<ProductoDto>();
                return productoCreado ?? throw new Exception("No se recibió respuesta del servidor");
            }
            catch (HttpRequestException ex)
            {
                throw new Exception($"Error de conexión al crear producto: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Actualiza un producto existente.
        /// PUT /api/productos/{id}
        /// </summary>
        public async Task<ProductoDto> ActualizarAsync(ActualizarProductoDto dto)
        {
            try
            {
                var response = await _httpClient.PutAsJsonAsync($"{API_BASE_URL}/{dto.Id}", dto);
                
                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Error al actualizar producto: {error}");
                }

                var productoActualizado = await response.Content.ReadFromJsonAsync<ProductoDto>();
                return productoActualizado ?? throw new Exception("No se recibió respuesta del servidor");
            }
            catch (HttpRequestException ex)
            {
                throw new Exception($"Error de conexión al actualizar producto: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Elimina un producto por su ID.
        /// DELETE /api/productos/{id}
        /// </summary>
        public async Task EliminarAsync(int id)
        {
            try
            {
                var response = await _httpClient.DeleteAsync($"{API_BASE_URL}/{id}");
                
                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Error al eliminar producto: {error}");
                }
            }
            catch (HttpRequestException ex)
            {
                throw new Exception($"Error de conexión al eliminar producto: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Busca productos por nombre.
        /// </summary>
        public async Task<List<ProductoDto>> BuscarPorNombreAsync(string nombre)
        {
            try
            {
                var productos = await ObtenerTodosAsync();
                return productos
                    .Where(p => p.Nombre.Contains(nombre, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }
            catch (Exception ex)
            {
                throw new Exception($"Error al buscar productos: {ex.Message}", ex);
            }
        }

        
    }
}