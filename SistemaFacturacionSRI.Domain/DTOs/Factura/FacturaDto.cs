namespace SistemaFacturacionSRI.Domain.DTOs.Factura;

/// <summary>
/// DTO para lectura completa de una factura (GET)
/// T-014: SPRINT 3 - DÍA 2
/// </summary>
public class FacturaDto
{
    public int Id { get; set; }
    
    // ==================== INFORMACIÓN BÁSICA ====================
    
    /// <summary>
    /// Número completo de la factura (001-001-000000001)
    /// </summary>
    public string NumeroFactura { get; set; } = string.Empty;
    
    /// <summary>
    /// Clave de acceso de 48 dígitos
    /// </summary>
    public string ClaveAcceso { get; set; } = string.Empty;
    
    /// <summary>
    /// Fecha y hora de emisión de la factura
    /// </summary>
    public DateTime FechaEmision { get; set; }
    
    /// <summary>
    /// Estado actual de la factura
    /// </summary>
    public string Estado { get; set; } = string.Empty;
    
    /// <summary>
    /// Descripción del estado (para mostrar en UI)
    /// </summary>
    public string EstadoDescripcion { get; set; } = string.Empty;
    
    // ==================== CLIENTE ====================
    
    /// <summary>
    /// ID del cliente
    /// </summary>
    public int ClienteId { get; set; }
    
    /// <summary>
    /// Información del cliente (nested DTO)
    /// </summary>
    public ClienteFacturaDto? Cliente { get; set; }
    
    // ==================== USUARIO ====================
    
    /// <summary>
    /// ID del usuario que emitió la factura
    /// </summary>
    public int UsuarioId { get; set; }
    
    /// <summary>
    /// Nombre del usuario emisor
    /// </summary>
    public string? UsuarioNombre { get; set; }
    
    // ==================== DETALLES ====================
    
    /// <summary>
    /// Lista de productos/servicios de la factura
    /// </summary>
    public List<DetalleFacturaDto> Detalles { get; set; } = new();
    
    // ==================== TOTALES ====================
    
    /// <summary>
    /// Subtotal de productos con tarifa 0% IVA
    /// </summary>
    public decimal Subtotal0 { get; set; }
    
    /// <summary>
    /// Subtotal de productos con tarifa 15% IVA
    /// </summary>
    public decimal Subtotal15 { get; set; }
    
    /// <summary>
    /// Suma de todos los subtotales sin IVA
    /// </summary>
    public decimal SubtotalTotal { get; set; }
    
    /// <summary>
    /// Total de descuentos aplicados
    /// </summary>
    public decimal TotalDescuento { get; set; }
    
    /// <summary>
    /// Total de IVA calculado
    /// </summary>
    public decimal TotalIVA { get; set; }
    
    /// <summary>
    /// Total final a pagar (Subtotal - Descuento + IVA)
    /// </summary>
    public decimal Total { get; set; }
    
    // ==================== INFORMACIÓN ADICIONAL ====================
    
    /// <summary>
    /// Observaciones o notas de la factura
    /// </summary>
    public string? Observaciones { get; set; }
    
    /// <summary>
    /// Campos adicionales personalizados
    /// </summary>
    public List<InfoAdicionalDto>? InfoAdicional { get; set; }
    
    // ==================== ARCHIVOS ====================
    
    /// <summary>
    /// Ruta del archivo XML original
    /// </summary>
    public string? XmlPath { get; set; }
    
    /// <summary>
    /// Ruta del archivo XML firmado
    /// </summary>
    public string? XmlFirmadoPath { get; set; }
    
    /// <summary>
    /// Ruta del archivo PDF (RIDE)
    /// </summary>
    public string? PdfPath { get; set; }
    
    // ==================== CONTROL DE ENVÍO ====================
    
    /// <summary>
    /// Indica si el correo ya fue enviado al cliente
    /// </summary>
    public bool CorreoEnviado { get; set; }
    
    /// <summary>
    /// Fecha y hora del primer envío de correo
    /// </summary>
    public DateTime? FechaEnvioCorreo { get; set; }
    
    // ==================== AUTORIZACIÓN SRI ====================
    
    /// <summary>
    /// Número de autorización del SRI
    /// </summary>
    public string? NumeroAutorizacion { get; set; }
    
    /// <summary>
    /// Fecha y hora de autorización del SRI
    /// </summary>
    public DateTime? FechaAutorizacion { get; set; }
    
    /// <summary>
    /// Mensajes del SRI (errores o advertencias)
    /// </summary>
    public string? MensajesSRI { get; set; }
    
    // ==================== AUDITORÍA ====================
    
    /// <summary>
    /// Fecha de creación del registro
    /// </summary>
    public DateTime FechaCreacion { get; set; }
    
    /// <summary>
    /// Fecha de última modificación
    /// </summary>
    public DateTime? FechaModificacion { get; set; }
}

/// <summary>
/// DTO simplificado del cliente para facturas
/// </summary>
public class ClienteFacturaDto
{
    public int Id { get; set; }
    public string TipoIdentificacion { get; set; } = string.Empty;
    public string Identificacion { get; set; } = string.Empty;
    public string RazonSocial { get; set; } = string.Empty;
    public string? NombreComercial { get; set; }
    public string? Direccion { get; set; }
    public string? Telefono { get; set; }
    public string? Email { get; set; }
}