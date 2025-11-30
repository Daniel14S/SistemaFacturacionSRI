using SistemaFacturacionSRI.Domain.DTOs.Cliente;
using SistemaFacturacionSRI.Domain.DTOs.Common;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace SistemaFacturacionSRI.WebUI.Services
{
    /// <summary>
    /// Servicio HTTP para consumir la API de clientes.
    /// Implementa las operaciones CRUD mediante HttpClient.
    /// </summary>
    public class ClienteHttpService : IClienteHttpService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<ClienteHttpService> _logger;
        private const string API_BASE_URL = "/api/cliente";

        public ClienteHttpService(HttpClient httpClient, ILogger<ClienteHttpService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        /// <inheritdoc />
        public async Task<PagedResultDto<ClienteListDto>> ObtenerClientesAsync(FiltroClienteDto filtro)
        {
            try
            {
                // Construir query string con los filtros
                var queryParams = new List<string>();

                if (!string.IsNullOrWhiteSpace(filtro.Busqueda))
                    queryParams.Add($"busqueda={Uri.EscapeDataString(filtro.Busqueda)}");

                if (filtro.TipoIdentificacionId.HasValue)
                    queryParams.Add($"tipoIdentificacionId={filtro.TipoIdentificacionId.Value}");

                if (filtro.Estado.HasValue)
                    queryParams.Add($"estado={filtro.Estado.Value.ToString().ToLower()}");

                queryParams.Add($"pageNumber={filtro.PageNumber}");
                queryParams.Add($"pageSize={filtro.PageSize}");

                if (!string.IsNullOrWhiteSpace(filtro.OrderBy))
                    queryParams.Add($"orderBy={Uri.EscapeDataString(filtro.OrderBy)}");

                queryParams.Add($"orderAscending={filtro.OrderAscending.ToString().ToLower()}");

                var queryString = string.Join("&", queryParams);
                var url = $"{API_BASE_URL}?{queryString}";

                var response = await _httpClient.GetFromJsonAsync<PagedResultDto<ClienteListDto>>(url);
                return response ?? new PagedResultDto<ClienteListDto>();
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Error HTTP al obtener clientes");
                throw new Exception($"Error al obtener clientes: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener clientes");
                throw;
            }
        }

        /// <inheritdoc />
        public async Task<ClienteDto?> ObtenerPorIdAsync(int clienteId)
        {
            try
            {
                return await _httpClient.GetFromJsonAsync<ClienteDto>($"{API_BASE_URL}/{clienteId}");
            }
            catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener cliente {ClienteId}", clienteId);
                throw new Exception($"Error al obtener el cliente: {ex.Message}", ex);
            }
        }

        /// <inheritdoc />
        public async Task<ClienteDto> CrearAsync(CrearClienteDto dto)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync(API_BASE_URL, dto);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Error al crear cliente: {StatusCode} - {Error}", 
                        response.StatusCode, errorContent);
                    
                    throw new Exception($"Error al crear cliente: {response.StatusCode}");
                }

                var clienteCreado = await response.Content.ReadFromJsonAsync<ClienteDto>();
                return clienteCreado ?? throw new Exception("No se recibió respuesta del servidor");
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Error HTTP al crear cliente");
                throw new Exception($"Error de conexión al crear cliente: {ex.Message}", ex);
            }
        }

        /// <inheritdoc />
        public async Task<ClienteDto> ActualizarAsync(ActualizarClienteDto dto)
        {
            try
            {
                var response = await _httpClient.PutAsJsonAsync($"{API_BASE_URL}/{dto.ClienteId}", dto);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Error al actualizar cliente: {StatusCode} - {Error}", 
                        response.StatusCode, errorContent);
                    
                    throw new Exception($"Error al actualizar cliente: {response.StatusCode}");
                }

                var clienteActualizado = await response.Content.ReadFromJsonAsync<ClienteDto>();
                return clienteActualizado ?? throw new Exception("No se recibió respuesta del servidor");
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Error HTTP al actualizar cliente");
                throw new Exception($"Error de conexión al actualizar cliente: {ex.Message}", ex);
            }
        }

        /// <inheritdoc />
        public async Task CambiarEstadoAsync(int clienteId, bool nuevoEstado)
        {
            try
            {
                var dto = new CambiarEstadoClienteDto
                {
                    ClienteId = clienteId,
                    Estado = nuevoEstado
                };

                var response = await _httpClient.PatchAsync(
                    $"{API_BASE_URL}/{clienteId}/estado",
                    new StringContent(JsonSerializer.Serialize(dto), Encoding.UTF8, "application/json")
                );

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Error al cambiar estado del cliente: {StatusCode} - {Error}", 
                        response.StatusCode, errorContent);
                    
                    throw new Exception($"Error al cambiar estado: {response.StatusCode}");
                }
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Error HTTP al cambiar estado del cliente");
                throw new Exception($"Error de conexión al cambiar estado: {ex.Message}", ex);
            }
        }

        /// <inheritdoc />
        public async Task<ClienteDto?> BuscarPorIdentificacionAsync(string identificacion)
        {
            try
            {
                var url = $"{API_BASE_URL}/buscar/{Uri.EscapeDataString(identificacion)}";
                return await _httpClient.GetFromJsonAsync<ClienteDto>(url);
            }
            catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al buscar cliente por identificación: {Identificacion}", identificacion);
                throw new Exception($"Error al buscar cliente: {ex.Message}", ex);
            }
        }

        /// <inheritdoc />
        public async Task<List<ClienteListDto>> BuscarClientesAsync(string termino, int limite = 10)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(termino))
                {
                    return new List<ClienteListDto>();
                }

                var url = $"{API_BASE_URL}/buscar?termino={Uri.EscapeDataString(termino)}&limite={limite}";
                var clientes = await _httpClient.GetFromJsonAsync<List<ClienteListDto>>(url);
                return clientes ?? new List<ClienteListDto>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al buscar clientes con término: {Termino}", termino);
                throw new Exception($"Error al buscar clientes: {ex.Message}", ex);
            }
        }
    }
}