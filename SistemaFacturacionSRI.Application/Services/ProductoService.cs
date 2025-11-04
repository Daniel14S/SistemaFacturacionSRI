using AutoMapper;
using SistemaFacturacionSRI.Application.DTOs.Producto;
using SistemaFacturacionSRI.Application.Interfaces.Repositories;
using SistemaFacturacionSRI.Application.Interfaces.Services;
using SistemaFacturacionSRI.Domain.Entities;

namespace SistemaFacturacionSRI.Application.Services
{
    /// <summary>
    /// Implementación del servicio de Productos.
    /// Contiene toda la lógica de negocio relacionada con productos.
    /// </summary>
    public class ProductoService : IProductoService
    {
        private readonly IProductoRepository _productoRepository;
        private readonly IMapper _mapper;

        /// <summary>
        /// Constructor con inyección de dependencias.
        /// </summary>
        public ProductoService(
            IProductoRepository productoRepository,
            IMapper mapper)
        {
            _productoRepository = productoRepository;
            _mapper = mapper;
        }

        /// <summary>
        /// Crea un nuevo producto en el sistema.
        /// Valida que el código sea único antes de crear.
        /// </summary>
        public async Task<ProductoDto> CrearAsync(CrearProductoDto dto)
        {
            // 1. Validar que el código no exista
            var codigoExiste = await _productoRepository.ExisteAsync(p => p.Codigo == dto.Codigo);
            if (codigoExiste)
            {
                throw new InvalidOperationException($"Ya existe un producto con el código '{dto.Codigo}'");
            }

            // 2. Mapear DTO a Entidad
            var producto = _mapper.Map<Producto>(dto);

            // 3. Guardar en la base de datos
            var productoCreado = await _productoRepository.AgregarAsync(producto);

            // 4. Mapear Entidad a DTO y retornar
            return _mapper.Map<ProductoDto>(productoCreado);
        }

        /// <summary>
        /// Obtiene todos los productos activos del sistema.
        /// </summary>
        public async Task<IEnumerable<ProductoDto>> ObtenerTodosAsync()
        {
            // 1. Obtener todos los productos del repositorio
            var productos = await _productoRepository.ObtenerTodosAsync();

            // 2. Mapear colección de entidades a colección de DTOs
            return _mapper.Map<IEnumerable<ProductoDto>>(productos);
        }

        /// <summary>
        /// Obtiene un producto específico por su ID.
        /// </summary>
        public async Task<ProductoDto?> ObtenerPorIdAsync(int id)
        {
            // 1. Buscar el producto por ID
            var producto = await _productoRepository.ObtenerPorIdAsync(id);

            // 2. Si no existe, retornar null
            if (producto == null)
            {
                return null;
            }

            // 3. Mapear entidad a DTO y retornar
            return _mapper.Map<ProductoDto>(producto);
        }

        /// <summary>
        /// Obtiene un producto por su código único.
        /// </summary>
        public async Task<ProductoDto?> ObtenerPorCodigoAsync(string codigo)
        {
            // 1. Validar que el código no esté vacío
            if (string.IsNullOrWhiteSpace(codigo))
            {
                throw new ArgumentException("El código no puede estar vacío", nameof(codigo));
            }

            // 2. Buscar el producto por código
            var producto = await _productoRepository.ObtenerPorCodigoAsync(codigo);

            // 3. Si no existe, retornar null
            if (producto == null)
            {
                return null;
            }

            // 4. Mapear entidad a DTO y retornar
            return _mapper.Map<ProductoDto>(producto);
        }

        /// <summary>
        /// Busca productos por nombre (búsqueda parcial).
        /// </summary>
        public async Task<IEnumerable<ProductoDto>> BuscarPorNombreAsync(string nombre)
        {
            // 1. Validar que el nombre no esté vacío
            if (string.IsNullOrWhiteSpace(nombre))
            {
                throw new ArgumentException("El nombre no puede estar vacío", nameof(nombre));
            }

            // 2. Buscar productos que contengan el texto en el nombre
            var productos = await _productoRepository.BuscarPorNombreAsync(nombre);

            // 3. Mapear y retornar
            return _mapper.Map<IEnumerable<ProductoDto>>(productos);
        }

        /// <summary>
        /// Obtiene productos que tienen stock disponible.
        /// </summary>
        public async Task<IEnumerable<ProductoDto>> ObtenerProductosConStockAsync()
        {
            // 1. Obtener productos con stock > 0
            var productos = await _productoRepository.ObtenerProductosConStockAsync();

            // 2. Mapear y retornar
            return _mapper.Map<IEnumerable<ProductoDto>>(productos);
        }

        // Métodos que se implementarán en siguientes tareas
        public async Task<ProductoDto> ActualizarAsync(ActualizarProductoDto dto)
        {
            // Validar existencia
            var existente = await _productoRepository.ObtenerPorIdAsync(dto.Id);
            if (existente == null)
            {
                throw new KeyNotFoundException($"No existe un producto con Id {dto.Id}");
            }

            // Validar unicidad de código si cambió
            if (!string.Equals(existente.Codigo, dto.Codigo, StringComparison.OrdinalIgnoreCase))
            {
                var codigoOcupado = await _productoRepository.ExisteAsync(p => p.Codigo == dto.Codigo && p.Id != dto.Id);
                if (codigoOcupado)
                {
                    throw new InvalidOperationException($"Ya existe un producto con el código '{dto.Codigo}'");
                }
            }

            // Mapear cambios sobre la entidad existente
            _mapper.Map(dto, existente);

            // Actualizar y retornar
            await _productoRepository.ActualizarAsync(existente);
            return _mapper.Map<ProductoDto>(existente);
        }

        public async Task EliminarAsync(int id)
        {
            var existente = await _productoRepository.ObtenerPorIdAsync(id);
            if (existente == null)
            {
                throw new KeyNotFoundException($"No existe un producto con Id {id}");
            }

            await _productoRepository.EliminarAsync(id);
        }
    }
}