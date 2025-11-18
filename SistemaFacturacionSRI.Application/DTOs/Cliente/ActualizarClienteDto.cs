using System.ComponentModel.DataAnnotations;

namespace SistemaFacturacionSRI.Application.DTOs.Cliente
{
    /// <summary>
    /// DTO para actualizar un cliente existente.
    /// </summary>
    public class ActualizarClienteDto
    {
        [Required(ErrorMessage = "El ID del cliente es obligatorio")]
        public int ClienteId { get; set; }

        [Required(ErrorMessage = "El tipo de identificación es obligatorio")]
        [Range(1, 10, ErrorMessage = "Tipo de identificación inválido")]
        public int TipoIdentificacionId { get; set; }

        [Required(ErrorMessage = "La identificación es obligatoria")]
        [StringLength(20, MinimumLength = 6, ErrorMessage = "La identificación debe tener entre 6 y 20 caracteres")]
        public string Identificacion { get; set; } = string.Empty;

        [Required(ErrorMessage = "Los nombres son obligatorios")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Los nombres deben tener entre 2 y 100 caracteres")]
        [RegularExpression(@"^[a-zA-ZáéíóúÁÉÍÓÚñÑ\s]+$", ErrorMessage = "Los nombres solo pueden contener letras")]
        public string Nombres { get; set; } = string.Empty;

        [Required(ErrorMessage = "Los apellidos son obligatorios")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Los apellidos deben tener entre 2 y 100 caracteres")]
        [RegularExpression(@"^[a-zA-ZáéíóúÁÉÍÓÚñÑ\s]+$", ErrorMessage = "Los apellidos solo pueden contener letras")]
        public string Apellidos { get; set; } = string.Empty;

        [StringLength(300, ErrorMessage = "La dirección no puede exceder 300 caracteres")]
        public string? Direccion { get; set; }

        [Phone(ErrorMessage = "El formato del teléfono no es válido")]
        [StringLength(20, ErrorMessage = "El teléfono no puede exceder 20 caracteres")]
        public string? Telefono { get; set; }

        [EmailAddress(ErrorMessage = "El formato del email no es válido")]
        [StringLength(100, ErrorMessage = "El email no puede exceder 100 caracteres")]
        public string? Email { get; set; }
    }
}