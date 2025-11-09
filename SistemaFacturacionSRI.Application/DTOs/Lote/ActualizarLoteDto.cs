using System;
using System.ComponentModel.DataAnnotations;

namespace SistemaFacturacionSRI.Application.DTOs.Lote
{
    public class ActualizarLoteDto
    {
        [Required(ErrorMessage = "El ID del lote es obligatorio")]
        public int LoteId { get; set; }

        public DateTime? FechaExpiracion { get; set; }

        [Required(ErrorMessage = "El precio de costo es obligatorio")]
        [Range(0.01, double.MaxValue, ErrorMessage = "El precio de costo debe ser mayor a 0")]
        public decimal PrecioCosto { get; set; }

        [Required(ErrorMessage = "La cantidad inicial es obligatoria")]
        [Range(0, int.MaxValue, ErrorMessage = "La cantidad inicial no puede ser negativa")]
        public int CantidadInicial { get; set; }

        [Required(ErrorMessage = "La cantidad disponible es obligatoria")]
        [Range(0, int.MaxValue, ErrorMessage = "La cantidad disponible no puede ser negativa")]
        public int CantidadDisponible { get; set; }
    }
}
