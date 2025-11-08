using System.Net.Http.Json;
using SistemaFacturacionSRI.Application.DTOs.Categoria;

namespace SistemaFacturacionSRI.WebUI.Services
{
    public class CategoriaHttpService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<CategoriaHttpService> _logger;
        private const string ApiEndpoint = "api/Categoria";

        public CategoriaHttpService(HttpClient httpClient, ILogger<CategoriaHttpService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task<List<CategoriaDto>> ObtenerTodasAsync()
        {
            try
            {
                var response = await _httpClient.GetFromJsonAsync<List<CategoriaDto>>(ApiEndpoint);
                return response ?? new List<CategoriaDto>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener todas las categorías");
                throw;
            }
        }

        public async Task<List<CategoriaDto>> ObtenerActivasAsync()
        {
            try
            {
                var response = await _httpClient.GetFromJsonAsync<List<CategoriaDto>>($"{ApiEndpoint}/activas");
                return response ?? new List<CategoriaDto>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener categorías activas");
                throw;
            }
        }

        public async Task<CategoriaDto?> ObtenerPorIdAsync(int id)
        {
            try
            {
                return await _httpClient.GetFromJsonAsync<CategoriaDto>($"{ApiEndpoint}/{id}");
            }
            catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener categoría por ID: {Id}", id);
                throw;
            }
        }

        public async Task<CategoriaDto?> ObtenerPorCodigoAsync(string codigo)
        {
            try
            {
                return await _httpClient.GetFromJsonAsync<CategoriaDto>($"{ApiEndpoint}/codigo/{codigo}");
            }
            catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener categoría por código: {Codigo}", codigo);
                throw;
            }
        }

        public async Task<List<CategoriaDto>> BuscarAsync(string termino)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(termino))
                    return new List<CategoriaDto>();

                var response = await _httpClient.GetFromJsonAsync<List<CategoriaDto>>($"{ApiEndpoint}/buscar/{termino}");
                return response ?? new List<CategoriaDto>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al buscar categorías con término: {Termino}", termino);
                throw;
            }
        }

        public async Task<CategoriaDto> CrearAsync(CreateCategoriaDto dto)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync(ApiEndpoint, dto);
                response.EnsureSuccessStatusCode();
                
                var categoria = await response.Content.ReadFromJsonAsync<CategoriaDto>();
                return categoria ?? throw new Exception("No se pudo crear la categoría");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear categoría");
                throw;
            }
        }

        public async Task<CategoriaDto> ActualizarAsync(UpdateCategoriaDto dto)
        {
            try
            {
                var response = await _httpClient.PutAsJsonAsync($"{ApiEndpoint}/{dto.Id}", dto);
                response.EnsureSuccessStatusCode();
                
                var categoria = await response.Content.ReadFromJsonAsync<CategoriaDto>();
                return categoria ?? throw new Exception("No se pudo actualizar la categoría");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al actualizar categoría");
                throw;
            }
        }

        public async Task<bool> EliminarAsync(int id)
        {
            try
            {
                var response = await _httpClient.DeleteAsync($"{ApiEndpoint}/{id}");
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al eliminar categoría con ID: {Id}", id);
                throw;
            }
        }

        public async Task<bool> ExisteCodigoAsync(string codigo, int? idExcluir = null)
        {
            try
            {
                var url = $"{ApiEndpoint}/existe-codigo/{codigo}";
                if (idExcluir.HasValue)
                    url += $"?idExcluir={idExcluir.Value}";

                var response = await _httpClient.GetFromJsonAsync<bool>(url);
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al verificar código de categoría");
                throw;
            }
        }
    }
}