using System.ComponentModel.DataAnnotations;

namespace SistemaFacturacionSRI.Domain.DTOs.Factura;

/// <summary>
/// DTO para actualizar una factura existente (PUT)
/// T-014: SPRINT 3 - DÍA 2
/// Solo se pueden actualizar facturas en estado BORRADOR
/// </summary>
public class ActualizarFacturaDto
{
    [Required]
    public int Id { get; set; }
    
    /// <summary>
    /// ID del cliente
    /// </summary>
    [Required(ErrorMessage = "El cliente es obligatorio")]
    [Range(1, int.MaxValue, ErrorMessage = "Debe seleccionar un cliente válido")]
    public int ClienteId { get; set; }
    
    /// <summary>
    /// Lista de detalles actualizada
    /// </summary>
    [Required(ErrorMessage = "Debe agregar al menos un producto")]
    [MinLength(1, ErrorMessage = "La factura debe tener al menos un producto")]
    public List<CrearDetalleFacturaDto> Detalles { get; set; } = new();
    
    /// <summary>
    /// Observaciones
    /// </summary>
    [MaxLength(500, ErrorMessage = "Las observaciones no pueden exceder 500 caracteres")]
    public string? Observaciones { get; set; }
    
    /// <summary>
    /// Información adicional
    /// </summary>
    public List<InfoAdicionalDto>? InfoAdicional { get; set; }
}