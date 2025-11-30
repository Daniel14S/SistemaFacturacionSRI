using AutoMapper;
using SistemaFacturacionSRI.Domain.DTOs.Producto;
using SistemaFacturacionSRI.Domain.Interfaces.Repositories;
using SistemaFacturacionSRI.Domain.Interfaces.Services;
using SistemaFacturacionSRI.Domain.Entities;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace SistemaFacturacionSRI.Application.Services
{
    /// <summary>
    /// Implementación del servicio de Productos.
    /// Contiene toda la lógica de negocio relacionada con productos.
    /// </summary>
    public class ProductoService : IProductoService
    {
        private readonly IProductoRepository _productoRepository;
        private readonly ILoteRepository _loteRepository;
        private readonly ILoteService _loteService; // <- Agregado
        private readonly IMapper _mapper;

        /// <summary>
        /// Constructor con inyección de dependencias.
        /// </summary>
        public ProductoService(
            IProductoRepository productoRepository,
            ILoteRepository loteRepository,
            ILoteService loteService, // <- Inyectar servicio de lotes
            IMapper mapper)
        {
            _productoRepository = productoRepository;
            _loteRepository = loteRepository;
            _loteService = loteService; // <- Asignar
            _mapper = mapper;
        }

        /// <summary>
        /// Crea un nuevo producto en el sistema.
        /// Valida que el código sea único antes de crear.
        /// </summary>
        public async Task<ProductoDto> CrearAsync(CrearProductoDto dto)
        {
            var codigoExiste = await _productoRepository.ExisteAsync(p => p.Codigo == dto.Codigo);
            if (codigoExiste)
                throw new InvalidOperationException($"Ya existe un producto con el código '{dto.Codigo}'");

            var producto = _mapper.Map<Producto>(dto);
            var productoCreado = await _productoRepository.AgregarAsync(producto);
            return _mapper.Map<ProductoDto>(productoCreado);
        }

        /// <summary>
        /// Obtiene todos los productos activos del sistema.
        /// </summary>
        public async Task<IEnumerable<ProductoDto>> ObtenerTodosAsync()
        {
            var productos = await _productoRepository.ObtenerTodosAsync();
            return _mapper.Map<IEnumerable<ProductoDto>>(productos);
        }

        /// <summary>
        /// Obtiene un producto específico por su ID.
        /// </summary>
        public async Task<ProductoDto?> ObtenerPorIdAsync(int id)
        {
            var producto = await _productoRepository.ObtenerPorIdAsync(id);
            if (producto == null)
            {
                return null;
            }

            var dto = _mapper.Map<ProductoDto>(producto);
            await EnriquecerProductoConLotePrioritarioAsync(dto);
            return dto;
        }

        /// <summary>
        /// Obtiene un producto por su código único.
        /// </summary>
        public async Task<ProductoDto?> ObtenerPorCodigoAsync(string codigo)
        {
            if (string.IsNullOrWhiteSpace(codigo))
                throw new ArgumentException("El código no puede estar vacío", nameof(codigo));

            var producto = await _productoRepository.ObtenerPorCodigoAsync(codigo);
            if (producto == null)
            {
                return null;
            }

            var dto = _mapper.Map<ProductoDto>(producto);
            await EnriquecerProductoConLotePrioritarioAsync(dto);
            return dto;
        }

        /// <summary>
        /// Busca productos por nombre (búsqueda parcial).
        /// </summary>
        public async Task<IEnumerable<ProductoDto>> BuscarPorNombreAsync(string nombre)
        {
            if (string.IsNullOrWhiteSpace(nombre))
                throw new ArgumentException("El nombre no puede estar vacío", nameof(nombre));

            var productos = await _productoRepository.BuscarPorNombreAsync(nombre);
            return _mapper.Map<IEnumerable<ProductoDto>>(productos);
        }

        /// <summary>
        /// Obtiene productos que tienen stock disponible.
        /// </summary>
        public async Task<IEnumerable<ProductoDto>> ObtenerProductosConStockAsync()
        {
            var productos = await _productoRepository.ObtenerProductosConStockAsync();
            return _mapper.Map<IEnumerable<ProductoDto>>(productos);
        }

        /// <summary>
        /// Actualiza los datos de un producto existente.
        /// </summary>
        public async Task<ProductoDto> ActualizarAsync(ActualizarProductoDto dto)
        {
            var existente = await _productoRepository.ObtenerPorIdAsync(dto.Id);
            if (existente == null)
                throw new KeyNotFoundException($"No existe un producto con Id {dto.Id}");

            if (!string.Equals(existente.Codigo, dto.Codigo, StringComparison.OrdinalIgnoreCase))
            {
                var codigoOcupado = await _productoRepository.ExisteAsync(p => p.Codigo == dto.Codigo && p.Id != dto.Id);
                if (codigoOcupado)
                    throw new InvalidOperationException($"Ya existe un producto con el código '{dto.Codigo}'");
            }

            _mapper.Map(dto, existente);
            await _productoRepository.ActualizarAsync(existente);
            return _mapper.Map<ProductoDto>(existente);
        }

        /// <summary>
        /// Obtiene la cantidad total de lotes asociados a un producto.
        /// </summary>
        public async Task<int> ObtenerCantidadTotalLotesAsync(int idProducto)
        {
            var lotes = await _loteRepository.ObtenerLotesPorProductoAsync(idProducto);
            return lotes.Sum(l => l.CantidadDisponible);
        }

        /// <summary>
/// Obtiene todos los productos y les asigna su lote prioritario según fecha de expiración.
/// También calcula precio promedio y detecta variación de precios entre lotes.
/// </summary>

// En ProductoService.cs, reemplaza el método ObtenerTodosConLotePrioritarioAsync

public async Task<IEnumerable<ProductoDto>> ObtenerTodosConLotePrioritarioAsync()
{
    var productos = await _productoRepository.ObtenerTodosIncluyendoInactivosAsync();
    var productosDto = _mapper.Map<List<ProductoDto>>(productos);

    foreach (var producto in productosDto)
    {
        await EnriquecerProductoConLotePrioritarioAsync(producto);
    }

    return productosDto;
}

        /// <summary>
        /// Elimina un producto solo si no tiene stock disponible.
        /// </summary>
        public async Task EliminarAsync(int id)
{
    var producto = await _productoRepository.ObtenerPorIdAsync(id);
    if (producto == null)
        throw new KeyNotFoundException($"No existe un producto con Id {id}");

    var lotePrioritario = await _loteService.ObtenerLotePrioritarioAsync(id);
    if (lotePrioritario != null && lotePrioritario.CantidadDisponible > 0)
        throw new InvalidOperationException("No se puede eliminar un producto que tiene stock disponible.");

    await _productoRepository.EliminarAsync(id);
}

        public async Task<IEnumerable<ProductoDto>> SearchByCodeOrNameAsync(string term)
        {
            if (string.IsNullOrWhiteSpace(term))
                return new List<ProductoDto>();

            var productos = await _productoRepository.SearchByCodeOrNameAsync(term);
            return _mapper.Map<IEnumerable<ProductoDto>>(productos);
        }

        /// <summary>
        /// Reactiva un producto previamente inactivado (Activo = true).
        /// </summary>
        public async Task ReactivarAsync(int id)
        {
            // Necesitamos incluir también los inactivos; el repositorio base filtra por Activo,
            // por lo que aquí asumimos que existe un método específico o usamos un repositorio especializado.
            var producto = await _productoRepository.ObtenerPorIdIncluyendoInactivosAsync(id);
            if (producto == null)
                throw new KeyNotFoundException($"No existe un producto con Id {id}");

            producto.Activo = true;
            await _productoRepository.ActualizarAsync(producto);
        }

        private async Task EnriquecerProductoConLotePrioritarioAsync(ProductoDto producto)
        {
            if (producto == null)
            {
                return;
            }

            var lotes = (await _loteRepository.ObtenerLotesPorProductoAsync(producto.Id)).ToList();
            var lotesDisponibles = lotes.Where(l => l.CantidadDisponible > 0).ToList();

            if (lotesDisponibles.Any())
            {
                var lotePrioritario = lotesDisponibles
                    .OrderBy(l => l.FechaExpiracion ?? DateTime.MaxValue)
                    .ThenBy(l => l.FechaCompra)
                    .First();

                producto.LotePrioritario = lotePrioritario.LoteId.ToString();
                producto.FechaExpiracionLotePrioritario = lotePrioritario.FechaExpiracion;
                producto.Precio = lotePrioritario.PVP;

                producto.TieneVariacionPrecios = lotesDisponibles
                    .Select(l => l.PVP)
                    .Distinct()
                    .Count() > 1;

                var costoTotal = lotesDisponibles.Sum(l => l.PrecioCosto * l.CantidadDisponible);
                var stockTotal = lotesDisponibles.Sum(l => l.CantidadDisponible);

                producto.PrecioCostoPromedio = stockTotal > 0 ? costoTotal / stockTotal : null;
                producto.Stock = stockTotal;
                producto.TieneStock = producto.Stock > 0;
                producto.ValorInventario = producto.Precio.HasValue
                    ? producto.Stock * producto.Precio.Value
                    : null;
            }
            else
            {
                producto.Precio = null;
                producto.PrecioCostoPromedio = null;
                producto.TieneVariacionPrecios = false;
                producto.Stock = 0;
                producto.TieneStock = false;
                producto.LotePrioritario = null;
                producto.FechaExpiracionLotePrioritario = null;
                producto.ValorInventario = null;
            }
        }
    }
}
