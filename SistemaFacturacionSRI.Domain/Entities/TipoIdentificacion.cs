namespace SistemaFacturacionSRI.Domain.Entities
{
    public class TipoIdentificacion
    {
        public int TipoIdentificacionId { get; set; }
        public string CodigoSRI { get; set; } = string.Empty; // NCHAR(2)
        public string Nombre { get; set; } = string.Empty;

        public ICollection<Cliente> Clientes { get; set; } = new List<Cliente>();
    }
}
