namespace SistemaFacturacionSRI.Domain.DTOs.Factura;

/// <summary>
/// DTO para mostrar detalles de una factura
/// T-014: SPRINT 3 - DÍA 2
/// </summary>
public class DetalleFacturaDto
{
    public int Id { get; set; }
    
    // ==================== PRODUCTO ====================
    
    /// <summary>
    /// ID del producto
    /// </summary>
    public int ProductoId { get; set; }
    
    /// <summary>
    /// Código principal del producto
    /// </summary>
    public string CodigoPrincipal { get; set; } = string.Empty;
    
    /// <summary>
    /// Código auxiliar del producto (opcional)
    /// </summary>
    public string? CodigoAuxiliar { get; set; }
    
    /// <summary>
    /// Descripción del producto
    /// </summary>
    public string Descripcion { get; set; } = string.Empty;
    
    // ==================== CANTIDADES Y PRECIOS ====================
    
    /// <summary>
    /// Cantidad del producto
    /// </summary>
    public decimal Cantidad { get; set; }
    
    /// <summary>
    /// Precio unitario sin IVA
    /// </summary>
    public decimal PrecioUnitario { get; set; }
    
    /// <summary>
    /// Descuento aplicado
    /// </summary>
    public decimal Descuento { get; set; }
    
    /// <summary>
    /// Subtotal sin IVA (PrecioUnitario * Cantidad - Descuento)
    /// </summary>
    public decimal Subtotal { get; set; }
    
    // ==================== IMPUESTOS ====================
    
    /// <summary>
    /// Tarifa de IVA aplicada (0, 12, 15)
    /// </summary>
    public int TarifaIVA { get; set; }
    
    /// <summary>
    /// Valor del IVA calculado
    /// </summary>
    public decimal ValorIVA { get; set; }
    
    /// <summary>
    /// Total con IVA incluido (Subtotal + ValorIVA)
    /// </summary>
    public decimal Total { get; set; }
    
    // ==================== INFORMACIÓN ADICIONAL ====================
    
    /// <summary>
    /// Información adicional del detalle
    /// </summary>
    public string? InformacionAdicional { get; set; }
}