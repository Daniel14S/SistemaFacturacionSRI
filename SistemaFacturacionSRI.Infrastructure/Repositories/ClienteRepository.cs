using Microsoft.EntityFrameworkCore;
using SistemaFacturacionSRI.Domain.Interfaces.Repositories;
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
            bool? estado,
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
                    c.Nombre1.ToLower().Contains(busquedaLower) ||
                    (c.Nombre2 != null && c.Nombre2.ToLower().Contains(busquedaLower)) ||
                    c.Apellido1.ToLower().Contains(busquedaLower) ||
                    (c.Apellido2 != null && c.Apellido2.ToLower().Contains(busquedaLower)) ||
                    (c.Email != null && c.Email.ToLower().Contains(busquedaLower))
                );
            }

            // Filtro por tipo de identificación
            if (tipoIdentificacionId.HasValue)
            {
                query = query.Where(c => c.TipoIdentificacionId == tipoIdentificacionId.Value);
            }

            if (estado.HasValue)
            {
                query = query.Where(c => c.Estado == estado.Value);
            }

            // Contar total
            var totalRegistros = await query.CountAsync();

            // Ordenamiento
            query = orderBy?.ToLower() switch
            {
                "nombres" => orderAscending
                    ? query.OrderBy(c => c.Nombre1)
                    : query.OrderByDescending(c => c.Nombre1),
                "identificacion" => orderAscending
                    ? query.OrderBy(c => c.Identificacion)
                    : query.OrderByDescending(c => c.Identificacion),
                _ => query.OrderBy(c => c.Nombre1) // Por defecto: orden alfabético
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
                    c.Nombre1.ToLower().Contains(terminoLower) ||
                    (c.Nombre2 != null && c.Nombre2.ToLower().Contains(terminoLower)) ||
                    c.Apellido1.ToLower().Contains(terminoLower) ||
                    (c.Apellido2 != null && c.Apellido2.ToLower().Contains(terminoLower)) ||
                    (c.Email != null && c.Email.ToLower().Contains(terminoLower))
                )
                .OrderBy(c => c.Nombre1)
                .ThenBy(c => c.Apellido1)
                .Take(limite)
                .ToListAsync();
        }

    }
}