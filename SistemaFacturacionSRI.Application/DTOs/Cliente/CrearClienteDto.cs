using System.ComponentModel.DataAnnotations;

namespace SistemaFacturacionSRI.Application.DTOs.Cliente
{
    /// <summary>
    /// DTO para crear un nuevo cliente en el sistema.
    /// </summary>
    public class CrearClienteDto
    {
        /// <summary>
        /// Tipo de identificación del cliente.
        /// 1 = Cédula, 2 = RUC, 3 = Pasaporte (ajustar según tu BD)
        /// </summary>
        [Required(ErrorMessage = "El tipo de identificación es obligatorio")]
        [Range(1, 10, ErrorMessage = "Tipo de identificación inválido")]
        public int TipoIdentificacionId { get; set; }

        /// <summary>
        /// Número de identificación (Cédula, RUC o Pasaporte)
        /// </summary>
        [Required(ErrorMessage = "La identificación es obligatoria")]
        [StringLength(20, MinimumLength = 6, ErrorMessage = "La identificación debe tener entre 6 y 20 caracteres")]
        public string Identificacion { get; set; } = string.Empty;

    /// <summary>
    /// Primer nombre del cliente (obligatorio)
    /// </summary>
    [Required(ErrorMessage = "El primer nombre es obligatorio")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "El primer nombre debe tener entre 2 y 100 caracteres")]
    [RegularExpression(@"^[a-zA-ZáéíóúÁÉÍÓÚñÑ\s]+$", ErrorMessage = "El primer nombre solo puede contener letras")]
    public string Nombre1 { get; set; } = string.Empty;

    /// <summary>
    /// Segundo nombre del cliente (opcional)
    /// </summary>
    [StringLength(100, ErrorMessage = "El segundo nombre no puede exceder 100 caracteres")]
    [RegularExpression(@"^[a-zA-ZáéíóúÁÉÍÓÚñÑ\s]*$", ErrorMessage = "El segundo nombre solo puede contener letras")]
    public string? Nombre2 { get; set; }

    /// <summary>
    /// Primer apellido del cliente (obligatorio)
    /// </summary>
    [Required(ErrorMessage = "El primer apellido es obligatorio")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "El primer apellido debe tener entre 2 y 100 caracteres")]
    [RegularExpression(@"^[a-zA-ZáéíóúÁÉÍÓÚñÑ\s]+$", ErrorMessage = "El primer apellido solo puede contener letras")]
    public string Apellido1 { get; set; } = string.Empty;

    /// <summary>
    /// Segundo apellido del cliente (opcional)
    /// </summary>
    [StringLength(100, ErrorMessage = "El segundo apellido no puede exceder 100 caracteres")]
    [RegularExpression(@"^[a-zA-ZáéíóúÁÉÍÓÚñÑ\s]*$", ErrorMessage = "El segundo apellido solo puede contener letras")]
    public string? Apellido2 { get; set; }

        /// <summary>
        /// Dirección del cliente
        /// </summary>
        [StringLength(300, ErrorMessage = "La dirección no puede exceder 300 caracteres")]
        public string? Direccion { get; set; }

        /// <summary>
        /// Teléfono de contacto
        /// </summary>
        [Phone(ErrorMessage = "El formato del teléfono no es válido")]
        [StringLength(20, ErrorMessage = "El teléfono no puede exceder 20 caracteres")]
        public string? Telefono { get; set; }

        /// <summary>
        /// Correo electrónico
        /// </summary>
        [EmailAddress(ErrorMessage = "El formato del email no es válido")]
        [StringLength(100, ErrorMessage = "El email no puede exceder 100 caracteres")]
        public string? Email { get; set; }
    }
}