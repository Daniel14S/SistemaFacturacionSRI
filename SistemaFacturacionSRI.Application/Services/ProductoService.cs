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
        /// <param name="productoRepository">Repositorio de productos</param>
        /// <param name="mapper">AutoMapper para conversión de objetos</param>
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

        // Los demás métodos los implementaremos en las siguientes tareas
        public Task<IEnumerable<ProductoDto>> ObtenerTodosAsync()
        {
            throw new NotImplementedException("Se implementará en T-20");
        }

        public Task<ProductoDto?> ObtenerPorIdAsync(int id)
        {
            throw new NotImplementedException("Se implementará en T-20");
        }

        public Task<ProductoDto?> ObtenerPorCodigoAsync(string codigo)
        {
            throw new NotImplementedException("Se implementará en T-20");
        }

        public Task<ProductoDto> ActualizarAsync(ActualizarProductoDto dto)
        {
            throw new NotImplementedException("Se implementará en T-21");
        }

        public Task EliminarAsync(int id)
        {
            throw new NotImplementedException("Se implementará en T-22");
        }

        public Task<IEnumerable<ProductoDto>> BuscarPorNombreAsync(string nombre)
        {
            throw new NotImplementedException("Se implementará después");
        }

        public Task<IEnumerable<ProductoDto>> ObtenerProductosConStockAsync()
        {
            throw new NotImplementedException("Se implementará después");
        }
    }
}