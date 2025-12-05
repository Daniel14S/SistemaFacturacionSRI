using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using SistemaFacturacionSRI.Domain.DTOs.Factura;
using SistemaFacturacionSRI.Domain.DTOs.Common;

namespace SistemaFacturacionSRI.WebUI.Services;

public class FacturaHttpService : IFacturaHttpService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<FacturaHttpService> _logger;

    public FacturaHttpService(HttpClient httpClient, ILogger<FacturaHttpService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<PagedResultDto<FacturaDto>> ObtenerFacturasAsync(
        int pageNumber = 1,
        int pageSize = 10,
        string? numeroFactura = null,
        DateTime? fechaDesde = null,
        DateTime? fechaHasta = null,
        string? estado = null)
    {
        try
        {
            var query = $"?pageNumber={pageNumber}&pageSize={pageSize}";

            if (!string.IsNullOrWhiteSpace(numeroFactura))
                query += $"&numeroFactura={Uri.EscapeDataString(numeroFactura)}";

            if (fechaDesde.HasValue)
                query += $"&fechaDesde={fechaDesde.Value:yyyy-MM-dd}";

            if (fechaHasta.HasValue)
                query += $"&fechaHasta={fechaHasta.Value:yyyy-MM-dd}";

            if (!string.IsNullOrWhiteSpace(estado))
                query += $"&estado={Uri.EscapeDataString(estado)}";

            _logger.LogInformation("Llamando a API: /api/factura{Query}", query);

            var response = await _httpClient.GetAsync($"/api/factura{query}");

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<PagedResultDto<FacturaDto>>();
                return result ?? new PagedResultDto<FacturaDto>
                {
                    Items = new List<FacturaDto>(),
                    PageNumber = pageNumber,
                    PageSize = pageSize,
                    TotalItems = 0
                };
            }

            _logger.LogError("Error al obtener facturas: {StatusCode}", response.StatusCode);
            return new PagedResultDto<FacturaDto>
            {
                Items = new List<FacturaDto>(),
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalItems = 0
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Excepción al obtener facturas");
            return new PagedResultDto<FacturaDto>
            {
                Items = new List<FacturaDto>(),
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalItems = 0
            };
        }
    }

    public async Task<FacturaDto?> ObtenerPorIdAsync(int id)
    {
        try
        {
            _logger.LogInformation("Obteniendo factura ID: {Id}", id);
            var response = await _httpClient.GetAsync($"/api/factura/{id}");

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<FacturaDto>();
            }

            _logger.LogError("Error al obtener factura {Id}: {StatusCode}", id, response.StatusCode);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Excepción al obtener factura {Id}", id);
            return null;
        }
    }

    public async Task<byte[]?> DescargarXmlAsync(int id)
    {
        try
        {
            _logger.LogInformation("Descargando XML de factura ID: {Id}", id);
            var response = await _httpClient.GetAsync($"/api/factura/{id}/xml");

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadAsByteArrayAsync();
            }

            _logger.LogError("Error al descargar XML {Id}: {StatusCode}", id, response.StatusCode);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Excepción al descargar XML {Id}", id);
            return null;
        }
    }

    public async Task<bool> ReenviarSriAsync(int id)
    {
        try
        {
            _logger.LogInformation("Enviando factura ID {Id} al SRI", id);
            // Usamos el endpoint enviar-sri que soporta FIRMADA, DEVUELTA y NO_AUTORIZADA
            var response = await _httpClient.PostAsync($"/api/factura/{id}/enviar-sri", null);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Factura {Id} enviada exitosamente", id);
                return true;
            }

            var error = await response.Content.ReadAsStringAsync();
            _logger.LogError("Error al enviar factura {Id}: {Error}", id, error);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Excepción al reenviar factura {Id}", id);
            return false;
        }
    }

    public async Task<bool> AnularAsync(int id, string motivo)
    {
        try
        {
            _logger.LogInformation("Anulando factura ID {Id}", id);
            
            var dto = new { Motivo = motivo };
            var content = new StringContent(
                JsonSerializer.Serialize(dto),
                Encoding.UTF8,
                "application/json");

            var response = await _httpClient.PatchAsync($"/api/factura/{id}/anular", content);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Factura {Id} anulada exitosamente", id);
                return true;
            }

            var error = await response.Content.ReadAsStringAsync();
            _logger.LogError("Error al anular factura {Id}: {Error}", id, error);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Excepción al anular factura {Id}", id);
            return false;
        }
    }
// ========== MÉTODOS NUEVOS (T-105) ==========

/// <summary>
/// T-105: Crea una nueva factura en estado BORRADOR
/// </summary>
public async Task<CrearFacturaResponseDto?> CrearFacturaAsync(CrearFacturaDto dto)
{
    try
    {
        _logger.LogInformation("Creando nueva factura para cliente {ClienteId}", dto.ClienteId);
        
        var response = await _httpClient.PostAsJsonAsync("/api/factura", dto);

        if (response.IsSuccessStatusCode)
        {
            var result = await response.Content.ReadFromJsonAsync<CrearFacturaResponseDto>();
            
            _logger.LogInformation(
                "Factura creada exitosamente. ID: {Id}, Número: {Numero}", 
                result?.Factura?.Id, 
                result?.Factura?.NumeroFactura);
            
            return result;
        }

        var errorContent = await response.Content.ReadAsStringAsync();
        _logger.LogError(
            "Error al crear factura: {StatusCode} - {Error}", 
            response.StatusCode, 
            errorContent);
        
        return null;
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Excepción al crear factura");
        return null;
    }
}

/// <summary>
/// T-105: Firma electrónicamente una factura (T-065)
/// </summary>
public async Task<FirmarFacturaResponseDto?> FirmarFacturaAsync(int id)
{
    try
    {
        _logger.LogInformation("Firmando factura ID: {Id}", id);
        
        var response = await _httpClient.PostAsync($"/api/factura/{id}/firmar", null);

        if (response.IsSuccessStatusCode)
        {
            var result = await response.Content.ReadFromJsonAsync<FirmarFacturaResponseDto>();
            
            _logger.LogInformation(
                "Factura {Id} firmada exitosamente. Estado: {Estado}", 
                id, 
                result?.EstadoActual);
            
            return result;
        }

        var errorContent = await response.Content.ReadAsStringAsync();
        _logger.LogError(
            "Error al firmar factura {Id}: {StatusCode} - {Error}", 
            id, 
            response.StatusCode, 
            errorContent);
        
        return new FirmarFacturaResponseDto
        {
            Message = $"Error al firmar: {errorContent}",
            FacturaId = id
        };
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Excepción al firmar factura {Id}", id);
        return new FirmarFacturaResponseDto
        {
            Message = $"Excepción: {ex.Message}",
            FacturaId = id
        };
    }
}

/// <summary>
/// T-105: Descarga el PDF RIDE de una factura
/// </summary>
public async Task<byte[]?> DescargarPdfAsync(int id)
{
    try
    {
        _logger.LogInformation("Descargando PDF de factura ID: {Id}", id);
        
        var response = await _httpClient.GetAsync($"/api/factura/{id}/pdf");

        if (response.IsSuccessStatusCode)
        {
            _logger.LogInformation("PDF de factura {Id} descargado exitosamente", id);
            return await response.Content.ReadAsByteArrayAsync();
        }

        _logger.LogError(
            "Error al descargar PDF {Id}: {StatusCode}", 
            id, 
            response.StatusCode);
        
        return null;
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Excepción al descargar PDF {Id}", id);
        return null;
    }
}


    
}