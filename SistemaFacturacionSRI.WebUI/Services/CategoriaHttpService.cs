using System.Net.Http.Json;
using SistemaFacturacionSRI.Application.DTOs.Categoria;

namespace SistemaFacturacionSRI.WebUI.Services
{
    public class CategoriaHttpService : ICategoriaHttpService
    {
        private readonly HttpClient _httpClient;
        private const string ApiEndpoint = "api/Categoria";

        public CategoriaHttpService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<List<CategoriaDto>> ObtenerTodasAsync()
        {
            try
            {
                var response = await _httpClient.GetFromJsonAsync<List<CategoriaDto>>(ApiEndpoint);
                return response ?? new List<CategoriaDto>();
            }
            catch (Exception)
            {
                return new List<CategoriaDto>();
            }
        }

        public async Task<List<CategoriaDto>> ObtenerActivasAsync()
        {
            try
            {
                var response = await _httpClient.GetFromJsonAsync<List<CategoriaDto>>($"{ApiEndpoint}/activas");
                return response ?? new List<CategoriaDto>();
            }
            catch (Exception)
            {
                return new List<CategoriaDto>();
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
            catch (Exception)
            {
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
            catch (Exception)
            {
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
            catch (Exception)
            {
                return new List<CategoriaDto>();
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
            catch (Exception)
            {
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
            catch (Exception)
            {
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
            catch (Exception)
            {
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
            catch (Exception)
            {
                throw;
            }
        }
    }
}