namespace SistemaFacturacionSRI.Domain.Entities
{
    public class Usuario
    {
    public int UsuarioId { get; set; }

    // Relaciones
    public int RolId { get; set; }
    public Rol? Rol { get; set; }

    // Credenciales
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;

    // Datos personales (segundos nombres/apellidos opcionales)
    public string Nombre1 { get; set; } = string.Empty;
    public string? Nombre2 { get; set; }
    public string Apellido1 { get; set; } = string.Empty;
    public string? Apellido2 { get; set; }

    public string Email { get; set; } = string.Empty;

    // Estado y trazabilidad
    public bool Estado { get; set; } = true;
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
    public DateTime? UltimoAcceso { get; set; }
    public int IntentosLogin { get; set; }
    }
}
