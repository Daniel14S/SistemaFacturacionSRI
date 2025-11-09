using SistemaFacturacionSRI.Application.DTOs.Lote;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace SistemaFacturacionSRI.WebUI.Services
{
    /// <summary>
    /// Cliente HTTP para consumir el endpoint de lotes desde los componentes Blazor.
    /// </summary>
    public class LoteHttpService
    {
        private readonly HttpClient _httpClient;
        private const string ApiBaseUrl = "/api/lote";

        public LoteHttpService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

    public async Task<List<LoteDto>> ObtenerTodosAsync()
        {
            try
            {
                var lotes = await _httpClient.GetFromJsonAsync<List<LoteDto>>(ApiBaseUrl);
                return lotes ?? new List<LoteDto>();
            }
            catch (HttpRequestException ex)
            {
                throw new Exception($"Error al obtener los lotes: {ex.Message}", ex);
            }
        }

        public async Task<LoteDto> CrearAsync(CrearLoteDto dto)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync(ApiBaseUrl, dto);

                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Error al crear el lote: {error}");
                }

                var loteCreado = await response.Content.ReadFromJsonAsync<LoteDto>();
                return loteCreado ?? throw new Exception("No se recibió respuesta del servidor al crear el lote.");
            }
            catch (HttpRequestException ex)
            {
                throw new Exception($"Error de conexión al crear el lote: {ex.Message}", ex);
            }
        }

        public async Task<List<LoteDto>> ObtenerLotesPorProductoAsync(int productoId)
{
    try
    {
        // Llama al endpoint del backend que devuelve los lotes por producto
        var lotes = await _httpClient.GetFromJsonAsync<List<LoteDto>>($"{ApiBaseUrl}/por-producto/{productoId}");
        return lotes ?? new List<LoteDto>();
    }
    catch (HttpRequestException ex)
    {
        throw new Exception($"Error al obtener lotes del producto {productoId}: {ex.Message}", ex);
    }
}
    }
}
