using AutoMapper;
using SistemaFacturacionSRI.Application.DTOs.Categoria;
using SistemaFacturacionSRI.Application.Interfaces.Repositories;
using SistemaFacturacionSRI.Application.Interfaces.Services;
using SistemaFacturacionSRI.Domain.Entities;

namespace SistemaFacturacionSRI.Application.Services
{
    public class CategoriaService : ICategoriaService
    {
        private readonly ICategoriaRepository _categoriaRepository;
        private readonly IMapper _mapper;

        public CategoriaService(ICategoriaRepository categoriaRepository, IMapper mapper)
        {
            _categoriaRepository = categoriaRepository;
            _mapper = mapper;
        }

        public async Task<IEnumerable<CategoriaDto>> ObtenerTodasAsync()
        {
            var categorias = await _categoriaRepository.ObtenerTodasAsync();
            return _mapper.Map<IEnumerable<CategoriaDto>>(categorias);
        }

        public async Task<IEnumerable<CategoriaDto>> ObtenerActivasAsync()
        {
            var todasLasCategorias = await _categoriaRepository.ObtenerTodasAsync();
            var categoriasActivas = todasLasCategorias.Where(c => c.Activo);
            return _mapper.Map<IEnumerable<CategoriaDto>>(categoriasActivas);
        }

        public async Task<CategoriaDto?> ObtenerPorIdAsync(int id)
        {
            var categoria = await _categoriaRepository.ObtenerPorIdAsync(id);
            return categoria != null ? _mapper.Map<CategoriaDto>(categoria) : null;
        }

        public async Task<CategoriaDto?> ObtenerPorCodigoAsync(string codigo)
        {
            var categoria = await _categoriaRepository.ObtenerPorCodigoAsync(codigo);
            return categoria != null ? _mapper.Map<CategoriaDto>(categoria) : null;
        }

        public async Task<CategoriaDto> CrearAsync(CreateCategoriaDto dto)
        {
            // Validar que el código no exista
            if (await ExisteCodigoAsync(dto.Codigo))
            {
                throw new InvalidOperationException($"Ya existe una categoría con el código '{dto.Codigo}'");
            }

            var categoria = _mapper.Map<Categoria>(dto);
            categoria.FechaCreacion = DateTime.Now;

            await _categoriaRepository.AgregarAsync(categoria);
            return _mapper.Map<CategoriaDto>(categoria);
        }

        public async Task<CategoriaDto?> ActualizarAsync(UpdateCategoriaDto dto)
        {
            var categoriaExistente = await _categoriaRepository.ObtenerPorIdAsync(dto.Id);
            if (categoriaExistente == null)
            {
                return null;
            }

            // Validar que el código no exista en otra categoría
            if (await ExisteCodigoAsync(dto.Codigo, dto.Id))
            {
                throw new InvalidOperationException($"Ya existe otra categoría con el código '{dto.Codigo}'");
            }

            _mapper.Map(dto, categoriaExistente);
            categoriaExistente.FechaModificacion = DateTime.Now;

            await _categoriaRepository.ActualizarAsync(categoriaExistente);
            return _mapper.Map<CategoriaDto>(categoriaExistente);
        }

        public async Task<bool> EliminarAsync(int id)
        {
            var categoria = await _categoriaRepository.ObtenerConProductosAsync(id);
            if (categoria == null)
            {
                return false;
            }

            // Verificar si tiene productos asociados
            if (categoria.Productos != null && categoria.Productos.Any())
            {
                throw new InvalidOperationException(
                    $"No se puede eliminar la categoría '{categoria.Nombre}' porque tiene {categoria.Productos.Count} productos asociados. " +
                    "Primero debe reasignar o eliminar los productos.");
            }

            await _categoriaRepository.EliminarAsync(id);
            return true;
        }

        public async Task<bool> ExisteCodigoAsync(string codigo, int? idExcluir = null)
        {
            var categoria = await _categoriaRepository.ObtenerPorCodigoAsync(codigo);
            
            if (categoria == null)
                return false;

            // Si existe pero es el mismo ID que estamos excluyendo, no cuenta como duplicado
            if (idExcluir.HasValue && categoria.Id == idExcluir.Value)
                return false;

            return true;
        }

        public async Task<IEnumerable<CategoriaDto>> BuscarAsync(string termino)
        {
            if (string.IsNullOrWhiteSpace(termino))
            {
                return await ObtenerTodasAsync();
            }

            // Buscar por nombre
            var categoriasPorNombre = await _categoriaRepository.BuscarPorNombreAsync(termino);
            
            // Buscar por código
            var categoriaPorCodigo = await _categoriaRepository.ObtenerPorCodigoAsync(termino);
            
            // Combinar resultados
            var resultado = categoriasPorNombre.ToList();
            
            if (categoriaPorCodigo != null && !resultado.Any(c => c.Id == categoriaPorCodigo.Id))
            {
                resultado.Add(categoriaPorCodigo);
            }

            return _mapper.Map<IEnumerable<CategoriaDto>>(resultado);
        }
    }
}