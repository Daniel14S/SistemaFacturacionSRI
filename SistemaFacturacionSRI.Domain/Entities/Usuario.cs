namespace SistemaFacturacionSRI.Domain.Entities
{
    public class Usuario
    {
        public int UsuarioId { get; set; }
        public int RolId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string NombreCompleto { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public bool Activo { get; set; } = true;

        public Rol? Rol { get; set; }
    }
}
