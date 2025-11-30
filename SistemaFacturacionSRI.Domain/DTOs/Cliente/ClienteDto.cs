using System.Linq;

namespace SistemaFacturacionSRI.Domain.DTOs.Cliente
{
    /// <summary>
    /// DTO completo de Cliente para lectura.
    /// </summary>
    public class ClienteDto
    {
        public int ClienteId { get; set; }
        public int TipoIdentificacionId { get; set; }
        public string TipoIdentificacionNombre { get; set; } = string.Empty;
        public string Identificacion { get; set; } = string.Empty;
        public string Nombre1 { get; set; } = string.Empty;
        public string? Nombre2 { get; set; }
        public string Apellido1 { get; set; } = string.Empty;
        public string? Apellido2 { get; set; }
        public string NombreCompleto => string.Join(" ", new[] { Nombre1, Nombre2, Apellido1, Apellido2 }
            .Where(p => !string.IsNullOrWhiteSpace(p)));
        public string? Direccion { get; set; }
        public string? Telefono { get; set; }
        public string? Email { get; set; }
        public bool Estado { get; set; }
    }
}