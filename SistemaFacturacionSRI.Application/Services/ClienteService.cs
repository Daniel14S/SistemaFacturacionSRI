using SistemaFacturacionSRI.Domain.DTOs.Cliente;
using SistemaFacturacionSRI.Domain.DTOs.Common;
using SistemaFacturacionSRI.Domain.Interfaces.Repositories;
using SistemaFacturacionSRI.Domain.Interfaces.Services;
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
            ValidarIdentificacionSegunTipo(dto.TipoIdentificacionId, dto.Identificacion);

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
                Nombre1 = dto.Nombre1.Trim(),
                Nombre2 = dto.Nombre2?.Trim(),
                Apellido1 = dto.Apellido1.Trim(),
                Apellido2 = dto.Apellido2?.Trim(),
                Direccion = dto.Direccion?.Trim(),
                Telefono = dto.Telefono?.Trim(),
                Email = dto.Email?.Trim()?.ToLower(),
                Estado = true
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
        /// <inheritdoc />
        public async Task<PagedResultDto<ClienteListDto>> ListarClientesAsync(FiltroClienteDto filtro)
        {
            // 1. VALIDAR PARÁMETROS DE PAGINACIÓN
            if (filtro.PageNumber < 1)
                filtro.PageNumber = 1;
            
            if (filtro.PageSize < 1)
                filtro.PageSize = 10;

            // 2. LLAMAR AL REPOSITORIO CON FILTROS
            var (clientes, totalRegistros) = await _clienteRepository.ListarConFiltrosAsync(
                filtro.Busqueda,
                filtro.TipoIdentificacionId,
                filtro.Estado,
                filtro.PageNumber,
                filtro.PageSize,
                filtro.OrderBy,
                filtro.OrderAscending
            );

            // 3. MAPEAR A DTOs
            var clientesDto = clientes.Select(c => MapearClienteListDto(c)).ToList();

            // 4. CREAR RESULTADO PAGINADO
            return new PagedResultDto<ClienteListDto>
            {
                Items = clientesDto,
                TotalItems = totalRegistros,
                PageNumber = filtro.PageNumber,
                PageSize = filtro.PageSize
            };
        }

        /// <inheritdoc />
        public async Task<ClienteDto?> ObtenerClientePorIdAsync(int clienteId)
        {
            var cliente = await _clienteRepository.ObtenerPorIdAsync(clienteId);
            return cliente == null ? null : MapearClienteDto(cliente);
        }

        /// <inheritdoc />
public async Task<ClienteDto> ActualizarClienteAsync(ActualizarClienteDto dto)
{
    ValidarIdentificacionSegunTipo(dto.TipoIdentificacionId, dto.Identificacion);

    // 1. VALIDAR QUE EL CLIENTE EXISTA
    var clienteExistente = await _clienteRepository.ObtenerPorIdAsync(dto.ClienteId);
    if (clienteExistente == null)
    {
        throw new KeyNotFoundException($"No se encontró el cliente con ID {dto.ClienteId}");
    }

    // 2. VALIDAR QUE LA IDENTIFICACIÓN NO ESTÉ EN USO POR OTRO CLIENTE
    // Solo validamos si cambió la identificación
    if (clienteExistente.Identificacion != dto.Identificacion.Trim())
    {
        var clienteConMismaIdentificacion = await _clienteRepository.ObtenerPorIdentificacionAsync(dto.Identificacion.Trim());
        
        if (clienteConMismaIdentificacion != null && clienteConMismaIdentificacion.ClienteId != dto.ClienteId)
        {
            throw new InvalidOperationException($"Ya existe otro cliente con la identificación {dto.Identificacion}");
        }
    }

    // 3. ACTUALIZAR LOS DATOS DEL CLIENTE
    clienteExistente.TipoIdentificacionId = dto.TipoIdentificacionId;
    clienteExistente.Identificacion = dto.Identificacion.Trim();
    clienteExistente.Nombre1 = dto.Nombre1.Trim();
    clienteExistente.Nombre2 = dto.Nombre2?.Trim();
    clienteExistente.Apellido1 = dto.Apellido1.Trim();
    clienteExistente.Apellido2 = dto.Apellido2?.Trim();
    clienteExistente.Direccion = dto.Direccion?.Trim();
    clienteExistente.Telefono = dto.Telefono?.Trim();
    clienteExistente.Email = dto.Email?.Trim()?.ToLower();

    // 4. GUARDAR CAMBIOS EN LA BASE DE DATOS
    await _clienteRepository.ActualizarAsync(clienteExistente);

    // 5. RECARGAR EL CLIENTE CON TIPOIDENTIFICACION INCLUIDO
    var clienteActualizado = await _clienteRepository.ObtenerPorIdAsync(dto.ClienteId);

    if (clienteActualizado == null)
    {
        throw new InvalidOperationException("Error al actualizar el cliente");
    }

    // 6. MAPEAR Y RETORNAR
    return MapearClienteDto(clienteActualizado);
}


        /// <inheritdoc />
        public async Task<ClienteDto?> BuscarPorIdentificacionAsync(string identificacion)
        {
            var cliente = await _clienteRepository.ObtenerPorIdentificacionAsync(identificacion);
            return cliente == null ? null : MapearClienteDto(cliente);
        }

        /// <inheritdoc />
public async Task<List<ClienteListDto>> BuscarClientesAsync(string termino, int limite = 10)
{
    // 1. VALIDAR PARÁMETROS
    if (string.IsNullOrWhiteSpace(termino))
    {
        return new List<ClienteListDto>();
    }

    if (limite < 1)
    {
        limite = 10;
    }

    if (limite > 50)
    {
        limite = 50; // Límite máximo para evitar consultas muy grandes
    }

    // 2. BUSCAR EN EL REPOSITORIO
    var clientes = await _clienteRepository.BuscarAsync(termino, limite);

    // 3. MAPEAR Y RETORNAR
    return clientes.Select(c => MapearClienteListDto(c)).ToList();
}

        /// <inheritdoc />
        public async Task CambiarEstadoClienteAsync(CambiarEstadoClienteDto dto)
        {
            var cliente = await _clienteRepository.ObtenerPorIdAsync(dto.ClienteId);

            if (cliente == null)
            {
                throw new KeyNotFoundException($"No se encontró el cliente con ID {dto.ClienteId}");
            }

            if (cliente.Estado != dto.Estado)
            {
                cliente.Estado = dto.Estado;
                await _clienteRepository.ActualizarAsync(cliente);
            }
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
                Nombre1 = cliente.Nombre1,
                Nombre2 = cliente.Nombre2,
                Apellido1 = cliente.Apellido1,
                Apellido2 = cliente.Apellido2,
                Direccion = cliente.Direccion,
                Telefono = cliente.Telefono,
                Email = cliente.Email,
                Estado = cliente.Estado
            };
        }

        private static void ValidarIdentificacionSegunTipo(int tipoIdentificacionId, string identificacion)
        {
            if (string.IsNullOrWhiteSpace(identificacion))
            {
                throw new InvalidOperationException("La identificación es obligatoria");
            }

            var (codigoSri, descripcionTipo) = tipoIdentificacionId switch
            {
                1 => ("05", "cédula"),
                2 => ("04", "RUC"),
                3 => ("06", "pasaporte"),
                _ => (null, "tipo de identificación")
            };

            if (codigoSri is null)
            {
                throw new InvalidOperationException("Tipo de identificación no soportado");
            }

            if (!IdentificacionValidator.ValidarFormato(codigoSri, identificacion.Trim()))
            {
                var mensaje = IdentificacionValidator.ObtenerMensajeError(codigoSri);
                throw new InvalidOperationException(mensaje ?? $"El {descripcionTipo} no es válido");
            }
        }

        /// <summary>
        /// Mapea una entidad Cliente a ClienteListDto (versión simplificada para listas).
        /// </summary>
        private ClienteListDto MapearClienteListDto(Cliente cliente)
        {
            return new ClienteListDto
            {
                ClienteId = cliente.ClienteId,
                TipoIdentificacion = cliente.TipoIdentificacion?.Nombre ?? "Sin tipo",
                Identificacion = cliente.Identificacion,
                NombreCompleto = cliente.NombreCompleto(),
                Email = cliente.Email,
                Telefono = cliente.Telefono,
                Estado = cliente.Estado
            };
        }

    }
}