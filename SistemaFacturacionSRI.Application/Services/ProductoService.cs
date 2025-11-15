using AutoMapper;
using SistemaFacturacionSRI.Application.DTOs.Producto;
using SistemaFacturacionSRI.Application.Interfaces.Repositories;
using SistemaFacturacionSRI.Application.Interfaces.Services;
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
            return producto == null ? null : _mapper.Map<ProductoDto>(producto);
        }

        /// <summary>
        /// Obtiene un producto por su código único.
        /// </summary>
        public async Task<ProductoDto?> ObtenerPorCodigoAsync(string codigo)
        {
            if (string.IsNullOrWhiteSpace(codigo))
                throw new ArgumentException("El código no puede estar vacío", nameof(codigo));

            var producto = await _productoRepository.ObtenerPorCodigoAsync(codigo);
            return producto == null ? null : _mapper.Map<ProductoDto>(producto);
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
    // Traer TODOS los productos (activos e inactivos) desde el repositorio base
    var productos = await _productoRepository.ObtenerTodosIncluyendoInactivosAsync();
    var productosDto = _mapper.Map<List<ProductoDto>>(productos);

    foreach (var producto in productosDto)
    {
        // Obtener el producto original para acceder al TipoIVACatalogo
        var productoOriginal = productos.First(p => p.Id == producto.Id);
        
        // Obtener todos los lotes con stock disponible
        var lotes = await _loteRepository.ObtenerLotesPorProductoAsync(producto.Id);
        var lotesDisponibles = lotes.Where(l => l.CantidadDisponible > 0).ToList();

        if (lotesDisponibles.Any())
        {
            // ===============================
            // 1️⃣ Lote prioritario (FEFO: vence primero)
            // ===============================
            var lotePrioritario = lotesDisponibles
                .OrderBy(l => l.FechaExpiracion ?? DateTime.MaxValue)
                .First();

            producto.LotePrioritario = lotePrioritario.LoteId.ToString();
            producto.FechaExpiracionLotePrioritario = lotePrioritario.FechaExpiracion;

            // ✅ Usar el PVP del lote prioritario como precio base para la vista de productos
            producto.Precio = lotePrioritario.PVP;

            // ✅ CALCULAR IVA Y PRECIO CON IVA DEL PVP DEL LOTE PRIORITARIO
            if (producto.Precio.HasValue && productoOriginal.TipoIVACatalogo != null)
            {
                decimal porcentajeIVA = productoOriginal.TipoIVACatalogo.Porcentaje;
                producto.ValorIVA = producto.Precio.Value * (porcentajeIVA / 100m);
                producto.PrecioConIVA = producto.Precio.Value + producto.ValorIVA.Value;
            }

            // ===============================
            // 2️⃣ Verificar variación de precios entre lotes
            // ===============================
            producto.TieneVariacionPrecios = lotesDisponibles
                .Select(l => l.PrecioCosto)
                .Distinct()
                .Count() > 1;

            // ===============================
            // 3️⃣ Precio promedio ponderado
            // ===============================
            var costoTotal = lotesDisponibles.Sum(l => l.PrecioCosto * l.CantidadDisponible);
            var cantidadTotal = lotesDisponibles.Sum(l => l.CantidadDisponible);
            producto.PrecioCostoPromedio = cantidadTotal > 0 ? costoTotal / cantidadTotal : 0;

            // ===============================
            // 4️⃣ Stock total sumando todos los lotes
            // ===============================
            producto.Stock = lotesDisponibles.Sum(l => l.CantidadDisponible);
            producto.TieneStock = producto.Stock > 0;
            
            // ✅ CALCULAR VALOR DEL INVENTARIO CON EL PVP DEL LOTE PRIORITARIO
            producto.ValorInventario = producto.Precio.HasValue 
                ? producto.Stock * producto.Precio.Value 
                : null;
        }
        else
        {
            // Si no hay lotes disponibles
            producto.Precio = null;
            producto.ValorIVA = null;
            producto.PrecioConIVA = null;
            producto.PrecioCostoPromedio = null;
            producto.TieneVariacionPrecios = false;
            producto.Stock = 0;
            producto.TieneStock = false;
            producto.LotePrioritario = null;
            producto.FechaExpiracionLotePrioritario = null;
            producto.ValorInventario = null;
        }
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
    }
}
