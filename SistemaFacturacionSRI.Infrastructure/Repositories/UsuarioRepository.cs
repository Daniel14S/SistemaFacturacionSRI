using Microsoft.EntityFrameworkCore;
using SistemaFacturacionSRI.Domain.Interfaces.Repositories;
using SistemaFacturacionSRI.Domain.Entities;
using SistemaFacturacionSRI.Infrastructure.Data;

namespace SistemaFacturacionSRI.Infrastructure.Repositories
{
    public class UsuarioRepository : IUsuarioRepository
    {
        private readonly ApplicationDbContext _context;

        public UsuarioRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Usuario?> ObtenerPorUsernameAsync(string username)
        {
            return await _context.Usuarios
                .Include(u => u.Rol)
                .FirstOrDefaultAsync(u => u.Username == username);
        }

        public async Task<Usuario?> ObtenerPorIdAsync(int usuarioId)
        {
            return await _context.Usuarios
                .Include(u => u.Rol)
                .FirstOrDefaultAsync(u => u.UsuarioId == usuarioId);
        }

        public async Task ActualizarAsync(Usuario usuario)
        {
            _context.Usuarios.Update(usuario);
            await _context.SaveChangesAsync();
        }

        public async Task CrearAsync(Usuario usuario)
        {
            await _context.Usuarios.AddAsync(usuario);
            await _context.SaveChangesAsync();
        }

        public async Task<Usuario?> ObtenerPorEmailAsync(string email)
        {
            return await _context.Usuarios
                .Include(u => u.Rol)
                .FirstOrDefaultAsync(u => u.Email == email.ToLower());
        }

        public async Task<Usuario?> ObtenerPorCedulaAsync(string cedula)
        {
            return await _context.Usuarios
                .Include(u => u.Rol)
                .FirstOrDefaultAsync(u => u.Cedula == cedula);
        }

        public async Task<(List<Usuario> Usuarios, int TotalRegistros)> ListarConFiltrosAsync(
            string? busqueda,
            int? rolId,
            bool? estado,
            bool? soloBloqueados,
            int pageNumber,
            int pageSize,
            string? orderBy,
            bool orderAscending)
        {
            // 1. Empezar con la consulta base
            var query = _context.Usuarios.Include(u => u.Rol).AsQueryable();

            // 2. FILTRO: Búsqueda por username, email o nombre
            if (!string.IsNullOrWhiteSpace(busqueda))
            {
                var busquedaLower = busqueda.ToLower();
                query = query.Where(u =>
                    u.Username.ToLower().Contains(busquedaLower) ||
                    u.Email.ToLower().Contains(busquedaLower) ||
                    u.Cedula.Contains(busquedaLower) ||
                    u.Nombre1.ToLower().Contains(busquedaLower) ||
                    (u.Nombre2 != null && u.Nombre2.ToLower().Contains(busquedaLower)) ||
                    u.Apellido1.ToLower().Contains(busquedaLower) ||
                    (u.Apellido2 != null && u.Apellido2.ToLower().Contains(busquedaLower))
                );
            }

            // 3. FILTRO: Por rol específico
            if (rolId.HasValue)
            {
                query = query.Where(u => u.RolId == rolId.Value);
            }

            // 4. FILTRO: Por estado (activo/inactivo)
            if (estado.HasValue)
            {
                query = query.Where(u => u.Estado == estado.Value);
            }

            // 5. FILTRO: Solo usuarios bloqueados
            if (soloBloqueados.HasValue && soloBloqueados.Value)
            {
                query = query.Where(u => u.IntentosLogin >= 5);
            }

            // 6. CONTAR TOTAL (antes de paginar)
            var totalRegistros = await query.CountAsync();

            // 7. ORDENAMIENTO
            query = orderBy?.ToLower() switch
            {
                "username" => orderAscending 
                    ? query.OrderBy(u => u.Username) 
                    : query.OrderByDescending(u => u.Username),
                "email" => orderAscending 
                    ? query.OrderBy(u => u.Email) 
                    : query.OrderByDescending(u => u.Email),
                "rol" => orderAscending 
                    ? query.OrderBy(u => u.Rol!.NombreRol) 
                    : query.OrderByDescending(u => u.Rol!.NombreRol),
                "fechacreacion" => orderAscending 
                    ? query.OrderBy(u => u.FechaCreacion) 
                    : query.OrderByDescending(u => u.FechaCreacion),
                _ => query.OrderByDescending(u => u.FechaCreacion) // Por defecto: más recientes primero
            };

            // 8. PAGINACIÓN
            var usuarios = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (usuarios, totalRegistros);
        }

        // TODO: Descomentar cuando se agregue Cedula a la BD
/*
public async Task<Usuario?> ObtenerPorCedulaAsync(string cedula)
{
    return await _context.Usuarios
        .Include(u => u.Rol)
        .FirstOrDefaultAsync(u => u.Cedula == cedula);
}
*/

        

    }
}
