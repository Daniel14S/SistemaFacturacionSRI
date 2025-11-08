using SistemaFacturacionSRI.Application.DTOs.Categoria;
using System.Net.Http.Json;

namespace SistemaFacturacionSRI.WebUI.Services
{
    /// <summary>
    /// Servicio cliente HTTP para consumir la API de Categorías.
    /// Este servicio se comunica con CategoriasController en el backend.
    /// </summary>
    public class CategoriaHttpService : ICategoriaHttpService
    {
        private readonly HttpClient _httpClient;
        private const string API_BASE_URL = "/api/categorias";

        public CategoriaHttpService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        /// <summary>
        /// Obtiene todas las categorías activas.
        /// GET /api/categorias
        /// </summary>
        public async Task<List<CategoriaDto>> ObtenerTodasAsync()
        {
            try
            {
                var categorias = await _httpClient.GetFromJsonAsync<List<CategoriaDto>>(API_BASE_URL);
                return categorias ?? new List<CategoriaDto>();
            }
            catch (HttpRequestException ex)
            {
                throw new Exception($"Error al obtener categorías: {ex.Message}", ex);
            }
        }
    }
}