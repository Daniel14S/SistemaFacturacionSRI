namespace SistemaFacturacionSRI.Domain.DTOs.Factura;

/// <summary>
/// DTO para mostrar detalles de una factura (OUTPUT)
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
    /// Precio total sin impuestos (Cantidad * PrecioUnitario)
    /// </summary>
    public decimal PrecioTotalSinImpuesto { get; set; }
    
    /// <summary>
    /// Base imponible (PrecioTotalSinImpuesto - Descuento)
    /// </summary>
    public decimal BaseImponible { get; set; }
    
    // ==================== IMPUESTOS ====================
    
    /// <summary>
    /// Código del porcentaje de IVA (0, 2, 3, 6, 7)
    /// Necesario para el XML del SRI
    /// </summary>
    public int CodigoPorcentajeIVA { get; set; }
    
    /// <summary>
    /// Tarifa de IVA aplicada en decimal (0.00, 0.12, 0.15)
    /// </summary>
    public decimal Tarifa { get; set; }
    
    /// <summary>
    /// Valor del IVA calculado
    /// </summary>
    public decimal Valor { get; set; }
    
    /// <summary>
    /// Total del detalle (BaseImponible + Valor)
    /// </summary>
    public decimal ValorTotal { get; set; }
    
    // ==================== INFORMACIÓN ADICIONAL ====================
    
    /// <summary>
    /// Información adicional del detalle
    /// </summary>
    public string? InfoAdicional { get; set; }
}