using System.ComponentModel.DataAnnotations;
using SistemaFacturacionSRI.Domain.Enums;

namespace SistemaFacturacionSRI.Application.DTOs.Usuario
{
    /// <summary>
    /// DTO para la creación de nuevos usuarios en el sistema.
    /// Contiene todos los campos necesarios para registrar un usuario.
    /// </summary>
    public class CrearUsuarioDto
    {
        // ========== DATOS DE AUTENTICACIÓN ==========
        
        [Required(ErrorMessage = "El nombre de usuario es obligatorio")]
        [StringLength(50, MinimumLength = 3, ErrorMessage = "El usuario debe tener entre 3 y 50 caracteres")]
        [RegularExpression(@"^[a-zA-Z0-9_.-]+$", ErrorMessage = "El usuario solo puede contener letras, números, guiones, puntos y guiones bajos")]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "La contraseña es obligatoria")]
        [StringLength(100, MinimumLength = 8, ErrorMessage = "La contraseña debe tener al menos 8 caracteres")]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]", 
            ErrorMessage = "La contraseña debe contener al menos una mayúscula, una minúscula, un número y un carácter especial")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "La confirmación de contraseña es obligatoria")]
        [Compare("Password", ErrorMessage = "Las contraseñas no coinciden")]
        public string ConfirmarPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "El email es obligatorio")]
        [EmailAddress(ErrorMessage = "El formato del email no es válido")]
        [StringLength(100, ErrorMessage = "El email no puede exceder 100 caracteres")]
        public string Email { get; set; } = string.Empty;

        // ========== DATOS PERSONALES ==========

        [Required(ErrorMessage = "El primer nombre es obligatorio")]
        [StringLength(50, MinimumLength = 2, ErrorMessage = "El nombre debe tener entre 2 y 50 caracteres")]
        [RegularExpression(@"^[a-zA-ZáéíóúÁÉÍÓÚñÑ\s]+$", ErrorMessage = "El nombre solo puede contener letras")]
        public string Nombre1 { get; set; } = string.Empty;

        [StringLength(50, ErrorMessage = "El segundo nombre no puede exceder 50 caracteres")]
        [RegularExpression(@"^[a-zA-ZáéíóúÁÉÍÓÚñÑ\s]*$", ErrorMessage = "El nombre solo puede contener letras")]
        public string? Nombre2 { get; set; }

        [Required(ErrorMessage = "El primer apellido es obligatorio")]
        [StringLength(50, MinimumLength = 2, ErrorMessage = "El apellido debe tener entre 2 y 50 caracteres")]
        [RegularExpression(@"^[a-zA-ZáéíóúÁÉÍÓÚñÑ\s]+$", ErrorMessage = "El apellido solo puede contener letras")]
        public string Apellido1 { get; set; } = string.Empty;

        [StringLength(50, ErrorMessage = "El segundo apellido no puede exceder 50 caracteres")]
        [RegularExpression(@"^[a-zA-ZáéíóúÁÉÍÓÚñÑ\s]*$", ErrorMessage = "El apellido solo puede contener letras")]
        public string? Apellido2 { get; set; }

        [StringLength(10, MinimumLength = 10, ErrorMessage = "La cédula debe tener exactamente 10 dígitos")]
        [RegularExpression(@"^\d{10}$", ErrorMessage = "La cédula debe contener solo números")]
        public string? Cedula { get; set; }

        [Phone(ErrorMessage = "El formato del teléfono no es válido")]
        [StringLength(15, ErrorMessage = "El teléfono no puede exceder 15 caracteres")]
        public string? Telefono { get; set; }

        [StringLength(200, ErrorMessage = "La dirección no puede exceder 200 caracteres")]
        public string? Direccion { get; set; }

        // ========== ROL Y ESTADO ==========

        [Required(ErrorMessage = "El rol es obligatorio")]
        [Range(1, 2, ErrorMessage = "El rol debe ser Administrador (1) o Vendedor (2)")]
        public int RolId { get; set; }

        /// <summary>
        /// Estado inicial del usuario (por defecto activo)
        /// </summary>
        public bool Estado { get; set; } = true;
    }
}