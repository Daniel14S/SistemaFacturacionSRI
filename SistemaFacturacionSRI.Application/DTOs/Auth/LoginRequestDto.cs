using System.ComponentModel.DataAnnotations;

namespace SistemaFacturacionSRI.Application.DTOs.Auth
{
    /// <summary>
    /// DTO para las credenciales de inicio de sesión
    /// </summary>
    public class LoginRequestDto
    {
        [Required(ErrorMessage = "El nombre de usuario es obligatorio")]
        [StringLength(50, MinimumLength = 3, ErrorMessage = "El usuario debe tener entre 3 y 50 caracteres")]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "La contraseña es obligatoria")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "La contraseña debe tener al menos 6 caracteres")]
        public string Password { get; set; } = string.Empty;
    }
}