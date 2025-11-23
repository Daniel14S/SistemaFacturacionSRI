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

    [Required(ErrorMessage = "El primer nombre es obligatorio")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "El primer nombre debe tener entre 2 y 100 caracteres")]
    [RegularExpression(@"^[a-zA-ZáéíóúÁÉÍÓÚñÑ\s]+$", ErrorMessage = "El primer nombre solo puede contener letras")]
    public string Nombre1 { get; set; } = string.Empty;

    [StringLength(100, ErrorMessage = "El segundo nombre no puede exceder 100 caracteres")]
    [RegularExpression(@"^[a-zA-ZáéíóúÁÉÍÓÚñÑ\s]*$", ErrorMessage = "El segundo nombre solo puede contener letras")]
    public string? Nombre2 { get; set; }

    [Required(ErrorMessage = "El primer apellido es obligatorio")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "El primer apellido debe tener entre 2 y 100 caracteres")]
    [RegularExpression(@"^[a-zA-ZáéíóúÁÉÍÓÚñÑ\s]+$", ErrorMessage = "El primer apellido solo puede contener letras")]
    public string Apellido1 { get; set; } = string.Empty;

    [StringLength(100, ErrorMessage = "El segundo apellido no puede exceder 100 caracteres")]
    [RegularExpression(@"^[a-zA-ZáéíóúÁÉÍÓÚñÑ\s]*$", ErrorMessage = "El segundo apellido solo puede contener letras")]
    public string? Apellido2 { get; set; }

        [StringLength(300, ErrorMessage = "La dirección no puede exceder 300 caracteres")]
        public string? Direccion { get; set; }

        [StringLength(10, MinimumLength = 10, ErrorMessage = "El teléfono debe tener 10 dígitos")]
        [RegularExpression(@"^\d{10}$", ErrorMessage = "El teléfono solo puede contener números")]
        public string? Telefono { get; set; }

        [EmailAddress(ErrorMessage = "El formato del email no es válido")]
        [StringLength(100, ErrorMessage = "El email no puede exceder 100 caracteres")]
        public string? Email { get; set; }
    }
}