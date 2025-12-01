using System.ComponentModel.DataAnnotations;

namespace SistemaFacturacionSRI.Domain.DTOs.Factura;

/// <summary>
/// DTO para crear una nueva factura (POST)
/// T-014: SPRINT 3 - DÍA 2
/// </summary>
public class CrearFacturaDto
{
    // ==================== INFORMACIÓN BÁSICA ====================
    
    /// <summary>
    /// ID del cliente (obligatorio)
    /// </summary>
    [Required(ErrorMessage = "El cliente es obligatorio")]
    [Range(1, int.MaxValue, ErrorMessage = "Debe seleccionar un cliente válido")]
    public int ClienteId { get; set; }
    
    // ==================== DETALLES ====================
    
    /// <summary>
    /// Lista de productos/servicios (mínimo 1)
    /// </summary>
    [Required(ErrorMessage = "Debe agregar al menos un producto")]
    [MinLength(1, ErrorMessage = "La factura debe tener al menos un producto")]
    public List<CrearDetalleFacturaDto> Detalles { get; set; } = new();
    
    // ==================== INFORMACIÓN ADICIONAL ====================
    
    /// <summary>
    /// Observaciones opcionales
    /// </summary>
    [MaxLength(500, ErrorMessage = "Las observaciones no pueden exceder 500 caracteres")]
    public string? Observaciones { get; set; }
    
    /// <summary>
    /// Información adicional personalizada (opcional)
    /// </summary>
    public List<InfoAdicionalDto>? InfoAdicional { get; set; }
}

public class CrearDetalleFacturaDto
{
    /// <summary>
    /// ID del producto
    /// </summary>
    [Required(ErrorMessage = "Debe seleccionar un producto")]
    [Range(1, int.MaxValue, ErrorMessage = "Debe seleccionar un producto válido")]
    public int ProductoId { get; set; }

    /// <summary>
    /// Cantidad del producto
    /// </summary>
    [Required(ErrorMessage = "La cantidad es obligatoria")]
    [Range(0.01, 999999.99, ErrorMessage = "La cantidad debe ser mayor a 0")]
    public decimal Cantidad { get; set; }

    /// <summary>
    /// Precio unitario (se toma del producto, pero puede ser modificado)
    /// </summary>
    [Required(ErrorMessage = "El precio unitario es obligatorio")]
    [Range(0.01, 999999.99, ErrorMessage = "El precio debe ser mayor a 0")]
    public decimal PrecioUnitario { get; set; }

    /// <summary>
    /// Descuento aplicado a este detalle
    /// </summary>
    [Range(0, 999999.99, ErrorMessage = "El descuento no puede ser negativo")]
    public decimal Descuento { get; set; } = 0;

    /// <summary>
    /// Código del porcentaje de IVA aplicable según enum TipoIVA
    /// 0 = 0% (IVA_0), 12 = 12% (IVA_12), 15 = 15% (IVA_15)
    /// </summary>
    [Required(ErrorMessage = "Debe especificar el código de IVA")]
    public int CodigoPorcentajeIVA { get; set; } = 15; // ✅ Por defecto 12%

    /// <summary>
    /// ID del lote específico a usar (opcional, si no se especifica se usa FIFO)
    /// </summary>
    public int? LoteId { get; set; }

    /// <summary>
    /// Información adicional del detalle (opcional)
    /// </summary>
    [MaxLength(500, ErrorMessage = "La información adicional no puede exceder 500 caracteres")]
    public string? InfoAdicional { get; set; }
}