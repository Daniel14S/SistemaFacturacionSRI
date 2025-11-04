namespace SistemaFacturacionSRI.Domain.Entities
{
    public class Cliente
    {
        public int ClienteId { get; set; }
        public int TipoIdentificacionId { get; set; }
        public string Identificacion { get; set; } = string.Empty; // UNIQUE
        public string Nombres { get; set; } = string.Empty;
        public string Apellidos { get; set; } = string.Empty;
        public string? Direccion { get; set; }
        public string? Telefono { get; set; }
        public string? Email { get; set; }

        public TipoIdentificacion? TipoIdentificacion { get; set; }
    }
}
