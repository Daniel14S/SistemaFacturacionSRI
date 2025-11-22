using SistemaFacturacionSRI.Application.DTOs.Cliente;
using SistemaFacturacionSRI.Application.DTOs.Common;

namespace SistemaFacturacionSRI.WebUI.Services
{
    /// <summary>
    /// Interfaz para el servicio HTTP de clientes.
    /// Define operaciones para consumir la API de clientes desde el frontend.
    /// </summary>
    public interface IClienteHttpService
    {
        /// <summary>
        /// Obtiene todos los clientes con paginación y filtros.
        /// </summary>
        Task<PagedResultDto<ClienteListDto>> ObtenerClientesAsync(FiltroClienteDto filtro);

        /// <summary>
        /// Obtiene un cliente por su ID.
        /// </summary>
        Task<ClienteDto?> ObtenerPorIdAsync(int clienteId);

        /// <summary>
        /// Crea un nuevo cliente.
        /// </summary>
        Task<ClienteDto> CrearAsync(CrearClienteDto dto);

        /// <summary>
        /// Actualiza un cliente existente.
        /// </summary>
        Task<ClienteDto> ActualizarAsync(ActualizarClienteDto dto);

        /// <summary>
        /// Cambia el estado (activo/inactivo) de un cliente.
        /// </summary>
        Task CambiarEstadoAsync(int clienteId, bool nuevoEstado);

        /// <summary>
        /// Busca un cliente por su identificación.
        /// </summary>
        Task<ClienteDto?> BuscarPorIdentificacionAsync(string identificacion);

        /// <summary>
        /// Busca clientes por nombre o identificación (búsqueda rápida).
        /// </summary>
        Task<List<ClienteListDto>> BuscarClientesAsync(string termino, int limite = 10);
    }
}