using System.Linq;

namespace SistemaFacturacionSRI.Domain.Entities
{
    public class Cliente
    {
        public int ClienteId { get; set; }
        public int TipoIdentificacionId { get; set; }
        public string Identificacion { get; set; } = string.Empty; // UNIQUE
        public string Nombre1 { get; set; } = string.Empty;
        public string? Nombre2 { get; set; }
        public string Apellido1 { get; set; } = string.Empty;
        public string? Apellido2 { get; set; }
        public string? Direccion { get; set; }
        public string? Telefono { get; set; }
        public string? Email { get; set; }
        public bool Estado { get; set; } = true;

        public TipoIdentificacion? TipoIdentificacion { get; set; }

        public string NombreCompleto()
        {
            var partes = new[] { Nombre1, Nombre2, Apellido1, Apellido2 }
                .Where(p => !string.IsNullOrWhiteSpace(p));
            return string.Join(" ", partes);
        }
    }
}
