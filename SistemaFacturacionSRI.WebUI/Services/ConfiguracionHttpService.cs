// SistemaFacturacionSRI.WebUI/Services/ConfiguracionHttpService.cs
// T-131: Implementación del servicio HTTP de configuración

using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using SistemaFacturacionSRI.Domain.DTOs.Configuracion;

namespace SistemaFacturacionSRI.WebUI.Services;

public class ConfiguracionHttpService : IConfiguracionHttpService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ConfiguracionHttpService> _logger;

    public ConfiguracionHttpService(
        HttpClient httpClient,
        ILogger<ConfiguracionHttpService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<ConfiguracionEmpresaDto?> ObtenerConfiguracionAsync()
    {
        try
        {
            _logger.LogInformation("Obteniendo configuración empresarial");

            var response = await _httpClient.GetAsync("/api/configuracion");

            if (response.IsSuccessStatusCode)
            {
                var config = await response.Content.ReadFromJsonAsync<ConfiguracionEmpresaDto>();
                _logger.LogInformation("Configuración obtenida exitosamente");
                return config;
            }

            _logger.LogWarning("Error al obtener configuración: {StatusCode}", response.StatusCode);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Excepción al obtener configuración");
            return null;
        }
    }

    public async Task<ConfiguracionEmpresaDto?> ActualizarConfiguracionAsync(ActualizarConfiguracionDto dto)
    {
        try
        {
            _logger.LogInformation("Actualizando configuración empresarial");

            var response = await _httpClient.PutAsJsonAsync("/api/configuracion", dto);

            if (response.IsSuccessStatusCode)
            {
                var configActualizada = await response.Content.ReadFromJsonAsync<ConfiguracionEmpresaDto>();
                _logger.LogInformation("Configuración actualizada exitosamente");
                return configActualizada;
            }

            var errorContent = await response.Content.ReadAsStringAsync();
            _logger.LogError("Error al actualizar configuración: {StatusCode} - {Error}", 
                response.StatusCode, errorContent);

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Excepción al actualizar configuración");
            return null;
        }
    }

    public async Task<(bool EsValido, string Mensaje)> ValidarCertificadoAsync()
    {
        try
        {
            _logger.LogInformation("Validando certificado digital");

            var response = await _httpClient.GetAsync("/api/configuracion/certificado/validar");

            if (response.IsSuccessStatusCode)
            {
                var resultado = await response.Content.ReadFromJsonAsync<ValidarCertificadoResponse>();
                return (resultado?.CertificadoValido ?? false, resultado?.Mensaje ?? "");
            }

            return (false, "Error al comunicarse con el servidor");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Excepción al validar certificado");
            return (false, $"Error: {ex.Message}");
        }
    }

    public async Task<InfoCertificadoDto?> ObtenerInfoCertificadoAsync()
    {
        try
        {
            _logger.LogInformation("Obteniendo información del certificado");

            var response = await _httpClient.GetAsync("/api/configuracion/certificado/info");

            if (response.IsSuccessStatusCode)
            {
                var info = await response.Content.ReadFromJsonAsync<InfoCertificadoDto>();
                return info;
            }

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                _logger.LogWarning("No se ha configurado un certificado digital");
                return null;
            }

            _logger.LogWarning("Error al obtener info certificado: {StatusCode}", response.StatusCode);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Excepción al obtener info certificado");
            return null;
        }
    }

    public async Task<bool> CambiarAmbienteSRIAsync(string nuevoAmbiente)
    {
        try
        {
            _logger.LogInformation("Cambiando ambiente SRI a: {Ambiente}", nuevoAmbiente);

            var dto = new { NuevoAmbiente = nuevoAmbiente };
            var response = await _httpClient.PatchAsJsonAsync("/api/configuracion/ambiente", dto);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Ambiente cambiado exitosamente");
                return true;
            }

            var error = await response.Content.ReadAsStringAsync();
            _logger.LogError("Error al cambiar ambiente: {Error}", error);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Excepción al cambiar ambiente SRI");
            return false;
        }
    }

    public async Task<(bool EstaCompleta, List<string> CamposFaltantes)> ValidarConfiguracionCompletaAsync()
    {
        try
        {
            _logger.LogInformation("Validando configuración completa");

            var response = await _httpClient.GetAsync("/api/configuracion/validar-completa");

            if (response.IsSuccessStatusCode)
            {
                var resultado = await response.Content.ReadFromJsonAsync<ValidarConfigResponse>();
                
                return (
                    resultado?.ConfiguracionCompleta ?? false,
                    resultado?.CamposFaltantes ?? new List<string>()
                );
            }

            return (false, new List<string> { "Error al comunicarse con el servidor" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Excepción al validar configuración completa");
            return (false, new List<string> { ex.Message });
        }
    }

    // ==================== SUBIDA DE ARCHIVOS (FUTURO) ====================

    public async Task<string?> SubirLogoAsync(byte[] archivoBytes, string nombreArchivo)
    {
        try
        {
            _logger.LogInformation("Subiendo logo de empresa");

            using var content = new MultipartFormDataContent();
            var fileContent = new ByteArrayContent(archivoBytes);
            fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/png");
            content.Add(fileContent, "logo", nombreArchivo);

            var response = await _httpClient.PostAsync("/api/configuracion/logo", content);

            if (response.IsSuccessStatusCode)
            {
                var resultado = await response.Content.ReadFromJsonAsync<SubirArchivoResponse>();
                _logger.LogInformation("Logo subido exitosamente: {Ruta}", resultado?.RutaArchivo);
                return resultado?.RutaArchivo;
            }

            _logger.LogError("Error al subir logo: {StatusCode}", response.StatusCode);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Excepción al subir logo");
            return null;
        }
    }

    public async Task<bool> SubirCertificadoAsync(byte[] archivoBytes, string nombreArchivo, string clave)
    {
        try
        {
            _logger.LogInformation("Subiendo certificado digital");

            using var content = new MultipartFormDataContent();
            var fileContent = new ByteArrayContent(archivoBytes);
            fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/x-pkcs12");
            content.Add(fileContent, "certificado", nombreArchivo);
            content.Add(new StringContent(clave), "clave");

            var response = await _httpClient.PostAsync("/api/configuracion/certificado", content);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Certificado subido exitosamente");
                return true;
            }

            var error = await response.Content.ReadAsStringAsync();
            _logger.LogError("Error al subir certificado: {Error}", error);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Excepción al subir certificado");
            return false;
        }
    }

    // ==================== CLASES AUXILIARES ====================

    private class ValidarCertificadoResponse
    {
        public bool CertificadoValido { get; set; }
        public string Mensaje { get; set; } = string.Empty;
    }

    private class ValidarConfigResponse
    {
        public bool ConfiguracionCompleta { get; set; }
        public List<string> CamposFaltantes { get; set; } = new();
        public string Mensaje { get; set; } = string.Empty;
    }

    private class SubirArchivoResponse
    {
        public string RutaArchivo { get; set; } = string.Empty;
        public string Mensaje { get; set; } = string.Empty;
    }
}