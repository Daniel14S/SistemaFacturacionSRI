using System.ComponentModel.DataAnnotations;

namespace SistemaFacturacionSRI.Domain.DTOs.Cliente
{
    /// <summary>
    /// DTO para cambiar el estado (activo/inactivo) de un cliente.
    /// </summary>
    public class CambiarEstadoClienteDto
    {
        [Required(ErrorMessage = "El ID del cliente es obligatorio")]
        public int ClienteId { get; set; }

        [Required(ErrorMessage = "El estado es obligatorio")]
        public bool Estado { get; set; }
    }
}

