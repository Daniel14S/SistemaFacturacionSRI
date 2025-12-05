// SistemaFacturacionSRI.Domain/DTOs/Factura/EnviarSriResponseDto.cs
using SistemaFacturacionSRI.Domain.DTOs.SRI;
using SistemaFacturacionSRI.Domain.Interfaces.Services;

namespace SistemaFacturacionSRI.Domain.DTOs.Factura;

/// <summary>
/// DTO de respuesta del endpoint POST /api/factura/{id}/enviar-sri (T-084)
/// Mapea desde ResultadoIntegracionSri de Pedro
/// </summary>
public class EnviarSriResponseDto
{
    /// <summary>
    /// Indica si la operación fue exitosa (factura autorizada)
    /// </summary>
    public bool Success { get; set; }
    
    /// <summary>
    /// Mensaje descriptivo del resultado
    /// </summary>
    public string Message { get; set; } = string.Empty;
    
    /// <summary>
    /// ID de la factura procesada
    /// </summary>
    public int FacturaId { get; set; }
    
    /// <summary>
    /// Número de factura (001-001-000000001)
    /// </summary>
    public string? NumeroFactura { get; set; }
    
    /// <summary>
    /// Clave de acceso del comprobante (49 dígitos)
    /// </summary>
    public string ClaveAcceso { get; set; } = string.Empty;
    
    /// <summary>
    /// Estado final de la factura: AUTORIZADA, DEVUELTA, NO_AUTORIZADA, etc.
    /// </summary>
    public string EstadoActual { get; set; } = string.Empty;
    
    /// <summary>
    /// Número de autorización del SRI (si fue autorizada)
    /// </summary>
    public string? NumeroAutorizacion { get; set; }
    
    /// <summary>
    /// Fecha y hora de autorización del SRI (si fue autorizada)
    /// </summary>
    public DateTime? FechaAutorizacion { get; set; }
    
    /// <summary>
    /// Mensajes del SRI (informativos, errores, advertencias)
    /// </summary>
    public List<string> MensajesSRI { get; set; } = new List<string>();
    
    /// <summary>
    /// Errores específicos (si falló)
    /// </summary>
    public List<string>? Errores { get; set; }
    
    /// <summary>
    /// Tiempo total de procesamiento
    /// </summary>
    public TimeSpan? TiempoTotal { get; set; }
    
    /// <summary>
    /// Número de intentos realizados en el proceso
    /// </summary>
    public int TotalIntentos { get; set; }
    
    /// <summary>
    /// Mapea desde ResultadoIntegracionSri de Pedro
    /// </summary>
    public static EnviarSriResponseDto MapearDesde(ResultadoIntegracionSri resultado, string? numeroFactura = null)
    {
        var response = new EnviarSriResponseDto
        {
            Success = resultado.Exitoso,
            FacturaId = resultado.FacturaId,
            NumeroFactura = numeroFactura,
            ClaveAcceso = resultado.ClaveAcceso,
            EstadoActual = resultado.EstadoFinal,
            NumeroAutorizacion = resultado.NumeroAutorizacion,
            FechaAutorizacion = resultado.FechaAutorizacion,
            TiempoTotal = resultado.TiempoTotal,
            TotalIntentos = resultado.TotalIntentos
        };
        
        // Mapear mensajes del SRI
        if (resultado.Mensajes != null && resultado.Mensajes.Any())
        {
            response.MensajesSRI = resultado.Mensajes
                .Select(m => $"[{m.Tipo}] {m.Mensaje}")
                .ToList();
        }
        
        // Construir mensaje principal
        if (resultado.Exitoso)
        {
            response.Message = $"Factura autorizada exitosamente por el SRI. Número de autorización: {resultado.NumeroAutorizacion}";
        }
        else
        {
            response.Message = resultado.MensajeError ?? "La factura no pudo ser autorizada por el SRI";
            
            // Agregar errores específicos
            response.Errores = new List<string>();
            
            if (!string.IsNullOrEmpty(resultado.MensajeError))
            {
                response.Errores.Add(resultado.MensajeError);
            }
            
            // Agregar errores de etapas fallidas
            foreach (var etapa in resultado.Etapas.Values.Where(e => !e.Exitosa && !string.IsNullOrEmpty(e.Error)))
            {
                response.Errores.Add($"{etapa.Nombre}: {etapa.Error}");
            }
        }
        
        return response;
    }
}