using AutoMapper;
using SistemaFacturacionSRI.Application.DTOs.Categoria;
using SistemaFacturacionSRI.Application.Interfaces.Repositories;
using SistemaFacturacionSRI.Application.Interfaces.Services;

namespace SistemaFacturacionSRI.Application.Services
{
    public class CategoriaService : ICategoriaService
    {
        private readonly ICategoriaRepository _repo;
        private readonly IMapper _mapper;
        public CategoriaService(ICategoriaRepository repo, IMapper mapper)
        {
            _repo = repo; _mapper = mapper;
        }

        public async Task<List<CategoriaDto>> ObtenerTodasAsync()
        {
            var categorias = await _repo.ObtenerTodasAsync();
            return _mapper.Map<List<CategoriaDto>>(categorias);
        }
    }
}
