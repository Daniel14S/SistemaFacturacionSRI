using System.ComponentModel.DataAnnotations;

namespace SistemaFacturacionSRI.Domain.DTOs.Factura;

/// <summary>
/// DTO para información adicional de la factura
/// T-014: SPRINT 3 - DÍA 2
/// Ejemplo: Email, Teléfono, Dirección de entrega, etc.
/// </summary>
public class InfoAdicionalDto
{
    /// <summary>
    /// Nombre del campo (ej: "Email", "Teléfono")
    /// </summary>
    [Required(ErrorMessage = "El nombre del campo es obligatorio")]
    [MaxLength(100, ErrorMessage = "El nombre no puede exceder 100 caracteres")]
    public string Nombre { get; set; } = string.Empty;
    
    /// <summary>
    /// Valor del campo (ej: "cliente@email.com", "0987654321")
    /// </summary>
    [Required(ErrorMessage = "El valor del campo es obligatorio")]
    [MaxLength(300, ErrorMessage = "El valor no puede exceder 300 caracteres")]
    public string Valor { get; set; } = string.Empty;
}