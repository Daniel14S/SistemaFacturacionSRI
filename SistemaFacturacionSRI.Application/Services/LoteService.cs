using AutoMapper;
using SistemaFacturacionSRI.Application.DTOs.Lote;
using SistemaFacturacionSRI.Application.Interfaces.Repositories;
using SistemaFacturacionSRI.Application.Interfaces.Services;
using SistemaFacturacionSRI.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SistemaFacturacionSRI.Application.Services
{
    /// <summary>
    /// Servicio para consultar y gestionar lotes.
    /// Contiene la lógica de negocio asociada a lotes.
    /// </summary>
    public class LoteService : ILoteService
    {
        private readonly ILoteRepository _loteRepository;
        private readonly IProductoRepository _productoRepository;
        private readonly IMapper _mapper;

        /// <summary>
        /// Constructor con inyección de dependencias.
        /// </summary>
        public LoteService(
            ILoteRepository loteRepository,
            IProductoRepository productoRepository,
            IMapper mapper)
        {
            _loteRepository = loteRepository;
            _productoRepository = productoRepository;
            _mapper = mapper;
        }

        /// <summary>
        /// Obtiene todos los lotes existentes en el sistema.
        /// </summary>
        /// <returns>Lista de LoteDto.</returns>
        public async Task<IEnumerable<LoteDto>> ObtenerTodosAsync()
        {
            var lotes = await _loteRepository.ObtenerTodosAsync();
            return _mapper.Map<IEnumerable<LoteDto>>(lotes);
        }

        /// <summary>
        /// Crea un nuevo lote para un producto específico.
        /// Realiza validaciones de existencia del producto y fechas.
        /// </summary>
        /// <param name="dto">Datos del lote a crear.</param>
        /// <returns>Lote creado como LoteDto.</returns>
        public async Task<LoteDto> CrearAsync(CrearLoteDto dto)
        {
            if (dto == null)
                throw new ArgumentNullException(nameof(dto));

            // Verificar que el producto exista y esté activo
            var producto = await _productoRepository.ObtenerPorIdAsync(dto.ProductoId);
            if (producto == null)
                throw new KeyNotFoundException($"El producto con ID {dto.ProductoId} no existe o está inactivo.");

            // Validar fechas
            if (dto.FechaExpiracion.HasValue && dto.FechaExpiracion.Value.Date < dto.FechaCompra.Date)
                throw new InvalidOperationException("La fecha de expiración no puede ser anterior a la fecha de compra.");

            // Mapear DTO a entidad Lote
            var nuevoLote = _mapper.Map<Lote>(dto);
            nuevoLote.CantidadDisponible = dto.CantidadInicial;

            // Guardar en base de datos
            var loteCreado = await _loteRepository.CrearAsync(nuevoLote);

            // Mapear de vuelta a DTO y devolver
            return _mapper.Map<LoteDto>(loteCreado);
        }

        /// <summary>
        /// Actualiza la información de un lote existente.
        /// </summary>
        /// <param name="loteDto">Datos del lote a actualizar.</param>
        /// <returns>LoteDto actualizado.</returns>
        public async Task<LoteDto> ActualizarAsync(LoteDto loteDto)
        {
            if (loteDto == null)
                throw new ArgumentNullException(nameof(loteDto));

            var loteExistente = await _loteRepository.ObtenerPorIdAsync(loteDto.LoteId);
            if (loteExistente == null)
                throw new KeyNotFoundException($"No se encontró el lote con ID {loteDto.LoteId}.");

            // Validar producto
            var producto = await _productoRepository.ObtenerPorIdAsync(loteDto.ProductoId);
            if (producto == null)
                throw new KeyNotFoundException($"El producto con ID {loteDto.ProductoId} no existe o está inactivo.");

            // Validar fechas
            if (loteDto.FechaExpiracion.HasValue && loteDto.FechaExpiracion.Value.Date < loteDto.FechaCompra.Date)
                throw new InvalidOperationException("La fecha de expiración no puede ser anterior a la fecha de compra.");

            // Mapear cambios desde el DTO hacia la entidad existente
            loteExistente.PrecioCosto = loteDto.PrecioCosto;
            loteExistente.CantidadInicial = loteDto.CantidadInicial;
            loteExistente.FechaCompra = loteDto.FechaCompra;
            loteExistente.FechaExpiracion = loteDto.FechaExpiracion;
            loteExistente.ProductoId = loteDto.ProductoId;

            // Guardar los cambios
            await _loteRepository.ActualizarAsync(loteExistente);

            // Retornar el DTO actualizado
            return _mapper.Map<LoteDto>(loteExistente);
        }

        /// <summary>
        /// Elimina un lote existente por su ID.
        /// </summary>
        /// <param name="id">ID del lote a eliminar.</param>
        public async Task EliminarAsync(int id)
        {
            var loteExistente = await _loteRepository.ObtenerPorIdAsync(id);
            if (loteExistente == null)
                throw new KeyNotFoundException($"No se encontró el lote con ID {id}.");

            if (loteExistente.CantidadDisponible > 0)
                throw new InvalidOperationException(
                    $"No se puede eliminar el lote #{id} porque tiene {loteExistente.CantidadDisponible} unidades disponibles en stock. Solo se pueden eliminar lotes sin stock disponible.");

            await _loteRepository.EliminarAsync(id);
        }

        /// <summary>
        /// Obtiene el lote prioritario de un producto (el más próximo a expirar).
        /// </summary>
        /// <param name="idProducto">ID del producto.</param>
        /// <returns>LoteDto prioritario o null.</returns>
        public async Task<LoteDto?> ObtenerLotePrioritarioAsync(int idProducto)
        {   
            var lotes = await _loteRepository.ObtenerLotesPorProductoAsync(idProducto);

            var lotePrioritario = lotes
                .Where(l => l.CantidadDisponible > 0)
                .OrderBy(l => l.FechaExpiracion ?? DateTime.MaxValue)
                .FirstOrDefault();

            return lotePrioritario == null ? null : _mapper.Map<LoteDto>(lotePrioritario);
        }

        /// <summary>
        /// Obtiene todos los lotes asociados a un producto.
        /// </summary>
        /// <param name="productoId">ID del producto.</param>
        /// <returns>Lista de LoteDto.</returns>
        public async Task<IEnumerable<LoteDto>> ObtenerPorProductoAsync(int productoId)
        {
            var lotes = await _loteRepository.ObtenerPorProductoAsync(productoId);
            return _mapper.Map<IEnumerable<LoteDto>>(lotes);
        }
        // SistemaFacturacionSRI.Application/Services/LoteService.cs
        // Agregar estos métodos a la clase existente:

        public async Task<LoteDto?> ObtenerPorIdAsync(int loteId)
        {
            var lote = await _loteRepository.ObtenerPorIdAsync(loteId);
            return lote == null ? null : _mapper.Map<LoteDto>(lote);
        }

       public async Task<LoteDto> ActualizarAsync(ActualizarLoteDto dto)
{
    var lote = await _loteRepository.ObtenerPorIdAsync(dto.LoteId);
    if (lote == null)
        throw new KeyNotFoundException("No se encontró el lote.");

    lote.FechaExpiracion = dto.FechaExpiracion;
    lote.PrecioCosto = dto.PrecioCosto;
    lote.CantidadInicial = dto.CantidadInicial;
    lote.CantidadDisponible = dto.CantidadDisponible;

    await _loteRepository.ActualizarAsync(lote);

    return new LoteDto
    {
        LoteId = lote.LoteId,
        ProductoId = lote.ProductoId,
        ProductoNombre = lote.Producto?.Nombre ?? "",
        ProductoCodigo = lote.Producto?.Codigo ?? "",
        ProductoCategoria = lote.Producto?.Categoria?.Nombre ?? "",
        FechaCompra = lote.FechaCompra,
        FechaExpiracion = lote.FechaExpiracion,
        PrecioCosto = lote.PrecioCosto,
        CantidadInicial = lote.CantidadInicial,
        CantidadDisponible = lote.CantidadDisponible
    };
}

    }
    
}
