using SistemaFacturacionSRI.Application.DTOs.Lote;
using System.Net.Http.Json;

namespace SistemaFacturacionSRI.WebUI.Services
{
    public class LoteHttpService
    {
        private readonly HttpClient _httpClient;

        public LoteHttpService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        // Obtener todos los lotes
        public async Task<IEnumerable<LoteDto>> ObtenerTodosAsync()
        {
            var response = await _httpClient.GetAsync("api/lote");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<IEnumerable<LoteDto>>() ?? new List<LoteDto>();
        }

        // Crear nuevo lote
        public async Task<LoteDto> CrearAsync(CrearLoteDto dto)
        {
            var response = await _httpClient.PostAsJsonAsync("api/lote", dto);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<LoteDto>() ?? new LoteDto();
        }

        // Obtener lote por ID
        public async Task<LoteDto?> ObtenerPorIdAsync(int id)
        {
            var response = await _httpClient.GetAsync($"api/lote/{id}");
            if (!response.IsSuccessStatusCode)
                return null;

            return await response.Content.ReadFromJsonAsync<LoteDto>();
        }

        // Actualizar lote existente
        // Cambiamos el tipo del parámetro para aceptar ActualizarLoteDto
        public async Task<LoteDto> ActualizarAsync(ActualizarLoteDto dto)
        {
            var response = await _httpClient.PutAsJsonAsync($"api/lote/{dto.LoteId}", dto);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<LoteDto>() ?? new LoteDto();
        }

        // Eliminar lote por ID
        public async Task EliminarAsync(int id)
        {
            var response = await _httpClient.DeleteAsync($"api/lote/{id}");
            response.EnsureSuccessStatusCode();
        }

        // Obtener lotes por producto
        public async Task<IEnumerable<LoteDto>> ObtenerLotesPorProductoAsync(int productoId)
        {
            var response = await _httpClient.GetAsync($"api/lote/por-producto/{productoId}");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<IEnumerable<LoteDto>>() ?? new List<LoteDto>();
        }
    }
}
