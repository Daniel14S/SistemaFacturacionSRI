namespace SistemaFacturacionSRI.Domain.Entities
{
    /// <summary>
    /// Representa un par nombre/valor adicional que se adjunta al XML de la factura.
    /// </summary>
    public class InfoAdicional
    {
        public int Id { get; set; }
        public int FacturaId { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Valor { get; set; } = string.Empty;

        public Factura? Factura { get; set; }
    }
}
