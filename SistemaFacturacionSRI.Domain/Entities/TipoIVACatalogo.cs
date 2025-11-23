namespace SistemaFacturacionSRI.Domain.Entities
{
    // Catálogo de Tipos de IVA almacenado en BD (no confundir con enum TipoIVA)
    public class TipoIVACatalogo
    {
        public int TipoIVAId { get; set; }
        public string Descripcion { get; set; } = string.Empty;
        public decimal Porcentaje { get; set; }
    }
}
