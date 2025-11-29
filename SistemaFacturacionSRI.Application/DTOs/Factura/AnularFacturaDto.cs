// Application/DTOs/Factura/AnularFacturaDto.cs
using System.ComponentModel.DataAnnotations;

namespace SistemaFacturacionSRI.Application.DTOs.Factura;

/// <summary>
/// DTO para anular una factura
/// </summary>
public class AnularFacturaDto
{
    /// <summary>
    /// Motivo de la anulación (obligatorio)
    /// </summary>
    [Required(ErrorMessage = "El motivo de anulación es obligatorio")]
    [MaxLength(500, ErrorMessage = "El motivo no puede exceder 500 caracteres")]
    public string Motivo { get; set; } = string.Empty;
}