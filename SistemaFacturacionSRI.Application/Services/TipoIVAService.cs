using AutoMapper;
using SistemaFacturacionSRI.Domain.DTOs.TipoIVA;
using SistemaFacturacionSRI.Domain.Interfaces.Repositories;
using SistemaFacturacionSRI.Domain.Interfaces.Services;

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
