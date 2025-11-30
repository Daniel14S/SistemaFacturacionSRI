namespace SistemaFacturacionSRI.Domain.DTOs.Factura;

/// <summary>
/// DTO simplificado para listados de facturas
/// T-014: SPRINT 3 - DÍA 2
/// </summary>
public class FacturaListDto
{
    public int Id { get; set; }
    
    /// <summary>
    /// Número de factura (001-001-000000001)
    /// </summary>
    public string NumeroFactura { get; set; } = string.Empty;
    
    /// <summary>
    /// Fecha de emisión
    /// </summary>
    public DateTime FechaEmision { get; set; }
    
    /// <summary>
    /// Estado de la factura
    /// </summary>
    public string Estado { get; set; } = string.Empty;
    
    /// <summary>
    /// Descripción del estado
    /// </summary>
    public string EstadoDescripcion { get; set; } = string.Empty;
    
    /// <summary>
    /// Identificación del cliente
    /// </summary>
    public string ClienteIdentificacion { get; set; } = string.Empty;
    
    /// <summary>
    /// Razón social o nombre del cliente
    /// </summary>
    public string ClienteNombre { get; set; } = string.Empty;
    
    /// <summary>
    /// Total de la factura
    /// </summary>
    public decimal Total { get; set; }
    
    /// <summary>
    /// Número de autorización del SRI (si está autorizada)
    /// </summary>
    public string? NumeroAutorizacion { get; set; }
    
    /// <summary>
    /// Fecha de autorización del SRI
    /// </summary>
    public DateTime? FechaAutorizacion { get; set; }
    
    /// <summary>
    /// Nombre del usuario que emitió la factura
    /// </summary>
    public string? UsuarioNombre { get; set; }
}