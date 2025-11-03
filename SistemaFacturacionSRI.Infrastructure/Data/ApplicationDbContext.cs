using Microsoft.EntityFrameworkCore;
using SistemaFacturacionSRI.Domain.Entities;
using SistemaFacturacionSRI.Infrastructure.Data.Configurations;

namespace SistemaFacturacionSRI.Infrastructure.Data
{
    /// <summary>
    /// Contexto principal de la base de datos.
    /// Es el puente entre el código C# y SQL Server.
    /// NOTA: En Onion Architecture, esto va en Infrastructure, NO en Domain.
    /// </summary>
    public class ApplicationDbContext : DbContext
    {
        /// <summary>
        /// Constructor que recibe las opciones de configuración.
        /// </summary>
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // DbSets - Cada DbSet<T> representa una tabla en la base de datos
        
        /// <summary>
        /// Tabla de Productos en la base de datos.
        /// </summary>
        public DbSet<Producto> Productos { get; set; }

        /// <summary>
        /// Configura el modelo de la base de datos.
        /// </summary>
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configuración global para EntidadBase
            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                if (typeof(EntidadBase).IsAssignableFrom(entityType.ClrType))
                {
                    modelBuilder.Entity(entityType.ClrType)
                        .Property<DateTime>("FechaCreacion")
                        .HasDefaultValueSql("GETDATE()");
                }
            }

            // Aplicar configuraciones Fluent API
            modelBuilder.ApplyConfiguration(new ProductoConfiguration());
        }

        /// <summary>
        /// Se ejecuta antes de guardar cambios.
        /// Establece automáticamente las fechas de auditoría.
        /// </summary>
        public override int SaveChanges()
        {
            var entradas = ChangeTracker.Entries<EntidadBase>();

            foreach (var entrada in entradas)
            {
                if (entrada.State == EntityState.Added)
                {
                    entrada.Entity.FechaCreacion = DateTime.Now;
                    entrada.Entity.Activo = true;
                }
                else if (entrada.State == EntityState.Modified)
                {
                    entrada.Entity.FechaModificacion = DateTime.Now;
                }
            }

            return base.SaveChanges();
        }

        /// <summary>
        /// Versión asíncrona de SaveChanges.
        /// </summary>
        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            var entradas = ChangeTracker.Entries<EntidadBase>();

            foreach (var entrada in entradas)
            {
                if (entrada.State == EntityState.Added)
                {
                    entrada.Entity.FechaCreacion = DateTime.Now;
                    entrada.Entity.Activo = true;
                }
                else if (entrada.State == EntityState.Modified)
                {
                    entrada.Entity.FechaModificacion = DateTime.Now;
                }
            }

            return await base.SaveChangesAsync(cancellationToken);
        }
    }
}