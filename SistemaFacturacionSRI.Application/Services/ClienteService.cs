using SistemaFacturacionSRI.Application.DTOs.Cliente;
using SistemaFacturacionSRI.Application.DTOs.Common;
using SistemaFacturacionSRI.Application.Interfaces.Repositories;
using SistemaFacturacionSRI.Application.Interfaces.Services;
using SistemaFacturacionSRI.Application.Validators;
using SistemaFacturacionSRI.Domain.Entities;

namespace SistemaFacturacionSRI.Application.Services
{
    /// <summary>
    /// Implementación del servicio de gestión de clientes.
    /// </summary>
    public class ClienteService : IClienteService
    {
        private readonly IClienteRepository _clienteRepository;

        public ClienteService(IClienteRepository clienteRepository)
        {
            _clienteRepository = clienteRepository;
        }

        /// <inheritdoc />
        public async Task<ClienteDto> CrearClienteAsync(CrearClienteDto dto)
        {
            // 1. VALIDAR QUE LA IDENTIFICACIÓN NO EXISTA
            var clienteExistente = await _clienteRepository.ObtenerPorIdentificacionAsync(dto.Identificacion);
            if (clienteExistente != null)
            {
                throw new InvalidOperationException($"Ya existe un cliente con la identificación {dto.Identificacion}");
            }

            // 2. OBTENER TIPO DE IDENTIFICACIÓN (para validar formato)
            // Nota: Necesitarías un repositorio de TipoIdentificacion o incluirlo en ClienteRepository
            // Por ahora asumimos que el TipoIdentificacionId es válido

            // 3. CREAR LA ENTIDAD CLIENTE
            var cliente = new Cliente
            {
                TipoIdentificacionId = dto.TipoIdentificacionId,
                Identificacion = dto.Identificacion.Trim(),
                Nombres = dto.Nombres.Trim(),
                Apellidos = dto.Apellidos.Trim(),
                Direccion = dto.Direccion?.Trim(),
                Telefono = dto.Telefono?.Trim(),
                Email = dto.Email?.Trim()?.ToLower()
            };

            // 4. GUARDAR EN BASE DE DATOS
            await _clienteRepository.CrearAsync(cliente);

            // 5. RECARGAR CON TIPOIDENTIFICACION
            var clienteCreado = await _clienteRepository.ObtenerPorIdAsync(cliente.ClienteId);

            if (clienteCreado == null)
            {
                throw new InvalidOperationException("Error al crear el cliente");
            }

            // 6. MAPEAR Y RETORNAR
            return MapearClienteDto(clienteCreado);
        }

        /// <inheritdoc />
        public Task<PagedResultDto<ClienteListDto>> ListarClientesAsync(FiltroClienteDto filtro)
        {
            throw new NotImplementedException("Se implementará en T-47");
        }

        /// <inheritdoc />
        public Task<ClienteDto?> ObtenerClientePorIdAsync(int clienteId)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public Task<ClienteDto> ActualizarClienteAsync(ActualizarClienteDto dto)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public async Task<ClienteDto?> BuscarPorIdentificacionAsync(string identificacion)
        {
            var cliente = await _clienteRepository.ObtenerPorIdentificacionAsync(identificacion);
            return cliente == null ? null : MapearClienteDto(cliente);
        }

        // ========== MÉTODOS AUXILIARES ==========

        private ClienteDto MapearClienteDto(Cliente cliente)
        {
            return new ClienteDto
            {
                ClienteId = cliente.ClienteId,
                TipoIdentificacionId = cliente.TipoIdentificacionId,
                TipoIdentificacionNombre = cliente.TipoIdentificacion?.Nombre ?? "Sin tipo",
                Identificacion = cliente.Identificacion,
                Nombres = cliente.Nombres,
                Apellidos = cliente.Apellidos,
                Direccion = cliente.Direccion,
                Telefono = cliente.Telefono,
                Email = cliente.Email
            };
        }
    }
}