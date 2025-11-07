using Microsoft.EntityFrameworkCore;
using SistemaFacturacionSRI.Application.Interfaces.Repositories;
using SistemaFacturacionSRI.Domain.Entities;
using SistemaFacturacionSRI.Infrastructure.Data;

namespace SistemaFacturacionSRI.Infrastructure.Repositories
{
    public class TipoIVARepository : ITipoIVARepository
    {
        private readonly ApplicationDbContext _context;
        public TipoIVARepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<TipoIVACatalogo>> ObtenerTodosAsync()
        {
            return await _context.TiposIVA.AsNoTracking().OrderBy(t => t.Porcentaje).ToListAsync();
        }
    }
}
