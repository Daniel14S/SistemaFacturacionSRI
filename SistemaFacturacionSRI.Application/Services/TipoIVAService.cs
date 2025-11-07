using AutoMapper;
using SistemaFacturacionSRI.Application.DTOs.TipoIVA;
using SistemaFacturacionSRI.Application.Interfaces.Repositories;
using SistemaFacturacionSRI.Application.Interfaces.Services;

namespace SistemaFacturacionSRI.Application.Services
{
    public class TipoIVAService : ITipoIVAService
    {
        private readonly ITipoIVARepository _repo;
        private readonly IMapper _mapper;
        public TipoIVAService(ITipoIVARepository repo, IMapper mapper)
        {
            _repo = repo; _mapper = mapper;
        }

        public async Task<List<TipoIVADto>> ObtenerTodosAsync()
        {
            var items = await _repo.ObtenerTodosAsync();
            return _mapper.Map<List<TipoIVADto>>(items);
        }
    }
}
