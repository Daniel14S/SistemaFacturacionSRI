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
        /// Obtiene el lote prioritario de un producto según la fecha de expiración más cercana.
        /// </summary>
        /// <param name="idProducto">ID del producto.</param>
        /// <returns>LoteDto del lote prioritario o null si no hay lotes disponibles.</returns>
        public async Task<LoteDto?> ObtenerLotePrioritarioAsync(int idProducto)
        {
            var lotes = await _loteRepository.ObtenerLotesPorProductoAsync(idProducto);

            var lotePrioritario = lotes
                .Where(l => l.CantidadDisponible > 0)
                .OrderBy(l => l.FechaExpiracion ?? DateTime.MaxValue)
                .FirstOrDefault();

            if (lotePrioritario == null)
                return null;

            // Mapear entidad a DTO
            return _mapper.Map<LoteDto>(lotePrioritario);
        }

        public async Task<List<LoteDto>> ObtenerLotesPorProductoAsync(int productoId)
        {
            var lotes = await _loteRepository.ObtenerLotesPorProductoAsync(productoId);
            return _mapper.Map<List<LoteDto>>(lotes);
        }

        public async Task<IEnumerable<LoteDto>> ObtenerPorProductoAsync(int productoId)
{
    var lotes = await _loteRepository.ObtenerPorProductoAsync(productoId);
    return _mapper.Map<IEnumerable<LoteDto>>(lotes);
}


    }
}
