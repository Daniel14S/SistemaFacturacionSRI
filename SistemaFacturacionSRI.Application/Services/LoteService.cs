using AutoMapper;
using SistemaFacturacionSRI.Application.DTOs.Lote;
using SistemaFacturacionSRI.Application.Interfaces.Repositories;
using SistemaFacturacionSRI.Application.Interfaces.Services;
using SistemaFacturacionSRI.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SistemaFacturacionSRI.Application.Services
{
    /// <summary>
    /// Servicio para consultar lotes y mapearlos a DTOs.
    /// </summary>
    public class LoteService : ILoteService
    {
        private readonly ILoteRepository _loteRepository;
        private readonly IProductoRepository _productoRepository;
        private readonly IMapper _mapper;

        public LoteService(ILoteRepository loteRepository, IProductoRepository productoRepository, IMapper mapper)
        {
            _loteRepository = loteRepository;
            _productoRepository = productoRepository;
            _mapper = mapper;
        }

        public async Task<IEnumerable<LoteDto>> ObtenerTodosAsync()
        {
            var lotes = await _loteRepository.ObtenerTodosAsync();
            return _mapper.Map<IEnumerable<LoteDto>>(lotes);
        }

        public async Task<LoteDto> CrearAsync(CrearLoteDto dto)
        {
            if (dto == null)
            {
                throw new ArgumentNullException(nameof(dto));
            }

            var producto = await _productoRepository.ObtenerPorIdAsync(dto.ProductoId);
            if (producto == null)
            {
                throw new KeyNotFoundException($"El producto con ID {dto.ProductoId} no existe o está inactivo.");
            }

            if (dto.FechaExpiracion.HasValue && dto.FechaExpiracion.Value.Date < dto.FechaCompra.Date)
            {
                throw new InvalidOperationException("La fecha de expiración no puede ser anterior a la fecha de compra.");
            }

            var nuevoLote = _mapper.Map<Lote>(dto);
            nuevoLote.CantidadDisponible = dto.CantidadInicial;

            var loteCreado = await _loteRepository.CrearAsync(nuevoLote);
            return _mapper.Map<LoteDto>(loteCreado);
        }
    }
}
