using SistemaFacturacionSRI.Domain.DTOs.Factura;
using SistemaFacturacionSRI.Domain.DTOs.Common;

namespace SistemaFacturacionSRI.WebUI.Services;

public interface IFacturaHttpService
{
    // ========== MÉTODOS EXISTENTES ==========
    Task<PagedResultDto<FacturaDto>> ObtenerFacturasAsync(
        int pageNumber = 1, 
        int pageSize = 10,
        string? numeroFactura = null,
        DateTime? fechaDesde = null,
        DateTime? fechaHasta = null,
        string? estado = null);
    
    Task<FacturaDto?> ObtenerPorIdAsync(int id);
    Task<byte[]?> DescargarXmlAsync(int id);
    Task<bool> ReenviarSriAsync(int id);
    Task<bool> AnularAsync(int id, string motivo);

    // ========== MÉTODOS NUEVOS (T-105) ==========
    
    /// <summary>
    /// T-105: Crea una nueva factura en estado BORRADOR
    /// </summary>
    Task<CrearFacturaResponseDto?> CrearFacturaAsync(CrearFacturaDto dto);
    
    /// <summary>
    /// T-105: Firma electrónicamente una factura (T-065)
    /// </summary>
    Task<FirmarFacturaResponseDto?> FirmarFacturaAsync(int id);
    
    /// <summary>
    /// T-105: Descarga el PDF RIDE de una factura
    /// </summary>
    Task<byte[]?> DescargarPdfAsync(int id);
    
    /// <summary>
    /// Envía la factura por correo al cliente (sin importar estado SRI)
    /// </summary>
    Task<EnviarCorreoResponseDto?> EnviarCorreoClienteAsync(int id);
    
    /// <summary>
    /// Cambia el estado de una factura DEVUELTA o NO_AUTORIZADA a PENDIENTE
    /// </summary>
    Task<CambiarEstadoPendienteResponseDto?> CambiarEstadoPendienteAsync(int id);
}

// ========== DTOs PARA RESPUESTAS (según tu API real) ==========

/// <summary>
/// Respuesta del endpoint POST /api/factura (crear)
/// </summary>
public class CrearFacturaResponseDto
{
    public string? Message { get; set; }
    public FacturaDto? Factura { get; set; }
    public EnlacesFacturaDto? Enlaces { get; set; }
}

public class EnlacesFacturaDto
{
    public string? VerDetalle { get; set; }
    public string? DescargarXml { get; set; }
    public string? EnviarSri { get; set; }
}

/// <summary>
/// Respuesta del endpoint POST /api/factura/{id}/firmar
/// </summary>
public class FirmarFacturaResponseDto
{
    public string? Message { get; set; }
    public int FacturaId { get; set; }
    public string? NumeroFactura { get; set; }
    public string? ClaveAcceso { get; set; }
    public string? EstadoAnterior { get; set; }
    public string? EstadoActual { get; set; }
    public ArchivosFacturaDto? Archivos { get; set; }
    public DateTime FechaFirma { get; set; }
    public SiguientePasoDto? SiguientePaso { get; set; }
}

public class ArchivosFacturaDto
{
    public string? XmlOriginal { get; set; }
    public string? XmlFirmado { get; set; }
    public string? UrlDescargarXml { get; set; }
    public string? UrlDescargarXmlFirmado { get; set; }
}

public class SiguientePasoDto
{
    public string? Accion { get; set; }
    public string? Endpoint { get; set; }
    public string? Descripcion { get; set; }
}

/// <summary>
/// Respuesta del endpoint POST /api/factura/{id}/enviar-correo
/// </summary>
public class EnviarCorreoResponseDto
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public int FacturaId { get; set; }
    public string? NumeroFactura { get; set; }
    public string? Destinatario { get; set; }
    public string? EstadoFactura { get; set; }
    public string? Aviso { get; set; }
}

/// <summary>
/// Respuesta del endpoint POST /api/factura/{id}/cambiar-estado-pendiente
/// </summary>
public class CambiarEstadoPendienteResponseDto
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public int FacturaId { get; set; }
    public string? NumeroFactura { get; set; }
    public string? EstadoAnterior { get; set; }
    public string? EstadoActual { get; set; }
    public string? Aviso { get; set; }
}