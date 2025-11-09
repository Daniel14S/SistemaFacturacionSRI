using System;
using System.ComponentModel.DataAnnotations;

namespace SistemaFacturacionSRI.Application.DTOs.Lote
{
    /// <summary>
    /// DTO para crear un nuevo lote en el sistema.
    /// </summary>
    public class CrearLoteDto
    {
        [Required(ErrorMessage = "El producto es obligatorio")]
        [Range(1, int.MaxValue, ErrorMessage = "Seleccione un producto válido")]
        public int ProductoId { get; set; }

        [Required(ErrorMessage = "La fecha de compra es obligatoria")]
        public DateTime FechaCompra { get; set; }

        public DateTime? FechaExpiracion { get; set; }

    [Required(ErrorMessage = "El precio de costo es obligatorio")]
    [Range(0.01, double.MaxValue, ErrorMessage = "El precio de costo debe ser mayor a 0")]
        public decimal PrecioCosto { get; set; }

        [Required(ErrorMessage = "La cantidad inicial es obligatoria")]
        [Range(0, int.MaxValue, ErrorMessage = "La cantidad inicial no puede ser negativa")]
        public int CantidadInicial { get; set; }
    }
}
