using System;

namespace SistemaFacturacionSRI.Application.DTOs.Lote
{
    /// <summary>
    /// DTO para mostrar la información de los lotes y su producto asociado.
    /// </summary>
    public class LoteDto
    {
        public int LoteId { get; set; }
        public int ProductoId { get; set; }
        public string ProductoCodigo { get; set; } = string.Empty;
        public string ProductoNombre { get; set; } = string.Empty;
        public string ProductoCategoria { get; set; } = string.Empty;
        public DateTime FechaCompra { get; set; }
        public DateTime? FechaExpiracion { get; set; }
        public decimal PrecioCosto { get; set; }
        public decimal PVP { get; set; }
        public int CantidadInicial { get; set; }
        public int CantidadDisponible { get; set; }
    }
}
