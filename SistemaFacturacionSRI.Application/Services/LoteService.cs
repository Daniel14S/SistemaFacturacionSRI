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

            var producto = await _productoRepository.ObtenerPorIdAsync(dto.ProductoId);
            if (producto == null)
                throw new KeyNotFoundException($"El producto con ID {dto.ProductoId} no existe o está inactivo.");

            if (dto.FechaExpiracion.HasValue && dto.FechaExpiracion.Value.Date < dto.FechaCompra.Date)
                throw new InvalidOperationException("La fecha de expiración no puede ser anterior a la fecha de compra.");

            var otrosLotes = await _loteRepository.ObtenerLotesPorProductoAsync(dto.ProductoId);
            var hayVariacionPVP = otrosLotes.Any(l => l.PVP != dto.PVP);

            if (hayVariacionPVP && !dto.ForzarActualizacionPVP)
            {
                throw new InvalidOperationException("PVP_VARIANCE_DETECTED");
            }

            var nuevoLote = _mapper.Map<Lote>(dto);
            nuevoLote.CantidadDisponible = dto.CantidadInicial;

            var loteCreado = await _loteRepository.CrearAsync(nuevoLote);

            if (hayVariacionPVP && dto.ForzarActualizacionPVP)
            {
                await _loteRepository.ActualizarPVPDeLotesPorProductoAsync(dto.ProductoId, dto.PVP, loteCreado.LoteId);
            }

            return _mapper.Map<LoteDto>(loteCreado);
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
        public async Task<LoteDto?> ObtenerPorIdAsync(int loteId)
        {
            var lote = await _loteRepository.ObtenerPorIdAsync(loteId);
            return lote == null ? null : _mapper.Map<LoteDto>(lote);
        }

        public async Task<LoteDto> ActualizarAsync(ActualizarLoteDto dto)
        {
            if (dto == null)
                throw new ArgumentNullException(nameof(dto));

            var lote = await _loteRepository.ObtenerPorIdAsync(dto.LoteId);
            if (lote == null)
                throw new KeyNotFoundException("No se encontró el lote.");

            if (dto.FechaExpiracion.HasValue && dto.FechaExpiracion.Value.Date < lote.FechaCompra.Date)
                throw new InvalidOperationException("La fecha de expiración no puede ser anterior a la fecha de compra.");

            var otrosLotes = await _loteRepository.ObtenerLotesPorProductoAsync(lote.ProductoId);
            var hayVariacionPVP = otrosLotes.Any(l => l.LoteId != dto.LoteId && l.PVP != dto.PVP);

            if (hayVariacionPVP && !dto.ForzarActualizacionPVP)
            {
                throw new InvalidOperationException("PVP_VARIANCE_DETECTED");
            }

            lote.FechaExpiracion = dto.FechaExpiracion;
            lote.PrecioCosto = dto.PrecioCosto;
            lote.PVP = dto.PVP;
            lote.CantidadDisponible = dto.CantidadDisponible;

            await _loteRepository.ActualizarAsync(lote);

            if (hayVariacionPVP && dto.ForzarActualizacionPVP)
            {
                await _loteRepository.ActualizarPVPDeLotesPorProductoAsync(lote.ProductoId, dto.PVP, dto.LoteId);
            }

            return _mapper.Map<LoteDto>(lote);
        }

    }
    
}
