namespace SistemaFacturacionSRI.Domain.Entities
{
    // Catálogo de Tipos de IVA almacenado en BD (no confundir con enum TipoIVA)
    public class TipoIVACatalogo
    {
        public int TipoIVAId { get; set; }
        public string CodigoSRI { get; set; } = string.Empty; // NCHAR(1)
        public string Descripcion { get; set; } = string.Empty;
        public decimal Porcentaje { get; set; }
        /// <summary>
        /// Productos asociados (cuando se usa relación catálogo además del enum).
        /// </summary>
        public ICollection<Producto> Productos { get; set; } = new List<Producto>();
    }
}
