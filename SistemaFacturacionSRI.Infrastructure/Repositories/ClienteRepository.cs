using Microsoft.EntityFrameworkCore;
using SistemaFacturacionSRI.Application.Interfaces.Repositories;
using SistemaFacturacionSRI.Domain.Entities;
using SistemaFacturacionSRI.Infrastructure.Data;

namespace SistemaFacturacionSRI.Infrastructure.Repositories
{
    public class ClienteRepository : IClienteRepository
    {
        private readonly ApplicationDbContext _context;

        public ClienteRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task CrearAsync(Cliente cliente)
        {
            await _context.Clientes.AddAsync(cliente);
            await _context.SaveChangesAsync();
        }

        public async Task<Cliente?> ObtenerPorIdAsync(int clienteId)
        {
            return await _context.Clientes
                .Include(c => c.TipoIdentificacion)
                .FirstOrDefaultAsync(c => c.ClienteId == clienteId);
        }

        public async Task<Cliente?> ObtenerPorIdentificacionAsync(string identificacion)
        {
            return await _context.Clientes
                .Include(c => c.TipoIdentificacion)
                .FirstOrDefaultAsync(c => c.Identificacion == identificacion);
        }

        public async Task ActualizarAsync(Cliente cliente)
        {
            _context.Clientes.Update(cliente);
            await _context.SaveChangesAsync();
        }

        public async Task<(List<Cliente> Clientes, int TotalRegistros)> ListarConFiltrosAsync(
            string? busqueda,
            int? tipoIdentificacionId,
            int pageNumber,
            int pageSize,
            string? orderBy,
            bool orderAscending)
        {
            var query = _context.Clientes
                .Include(c => c.TipoIdentificacion)
                .AsQueryable();

            // Filtro de búsqueda
            if (!string.IsNullOrWhiteSpace(busqueda))
            {
                var busquedaLower = busqueda.ToLower();
                query = query.Where(c =>
                    c.Identificacion.ToLower().Contains(busquedaLower) ||
                    c.Nombres.ToLower().Contains(busquedaLower) ||
                    c.Apellidos.ToLower().Contains(busquedaLower) ||
                    (c.Email != null && c.Email.ToLower().Contains(busquedaLower))
                );
            }

            // Filtro por tipo de identificación
            if (tipoIdentificacionId.HasValue)
            {
                query = query.Where(c => c.TipoIdentificacionId == tipoIdentificacionId.Value);
            }

            // Contar total
            var totalRegistros = await query.CountAsync();

            // Ordenamiento
            query = orderBy?.ToLower() switch
            {
                "nombres" => orderAscending
                    ? query.OrderBy(c => c.Nombres)
                    : query.OrderByDescending(c => c.Nombres),
                "identificacion" => orderAscending
                    ? query.OrderBy(c => c.Identificacion)
                    : query.OrderByDescending(c => c.Identificacion),
                _ => query.OrderBy(c => c.Nombres) // Por defecto: orden alfabético
            };

            // Paginación
            var clientes = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (clientes, totalRegistros);
        }


        public async Task<List<Cliente>> BuscarAsync(string termino, int limite)
{
    if (string.IsNullOrWhiteSpace(termino))
    {
        return new List<Cliente>();
    }

    var terminoLower = termino.ToLower().Trim();

    return await _context.Clientes
        .Include(c => c.TipoIdentificacion)
        .Where(c =>
            c.Identificacion.ToLower().Contains(terminoLower) ||
            c.Nombres.ToLower().Contains(terminoLower) ||
            c.Apellidos.ToLower().Contains(terminoLower) ||
            (c.Email != null && c.Email.ToLower().Contains(terminoLower))
        )
        .OrderBy(c => c.Nombres)
        .Take(limite)
        .ToListAsync();
}

    }
}