namespace SistemaFacturacionSRI.Application.DTOs.Cliente
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
        public string Nombres { get; set; } = string.Empty;
        public string Apellidos { get; set; } = string.Empty;
        public string NombreCompleto => $"{Nombres} {Apellidos}".Trim();
        public string? Direccion { get; set; }
        public string? Telefono { get; set; }
        public string? Email { get; set; }
    }
}