using Microsoft.EntityFrameworkCore;
using SistemaFacturacionSRI.Domain.Entities;

namespace SistemaFacturacionSRI.Domain.Data
{
    /// <summary>
    /// Contexto principal de la base de datos.
    /// Es el puente entre el código C# y SQL Server.
    /// </summary>
    public class ApplicationDbContext : DbContext
    {
        /// <summary>
        /// Constructor que recibe las opciones de configuración.
        /// Estas opciones incluyen la cadena de conexión a SQL Server.
        /// </summary>
        /// <param name="options">Configuración del DbContext (connection string, etc.)</param>
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
            // base(options) pasa las opciones a la clase padre DbContext
        }

        // Aquí agregaremos los DbSet<T> para cada tabla
        // Ejemplo futuro: public DbSet<Producto> Productos { get; set; }
        // Cada DbSet<T> representa una tabla en la base de datos

        /// <summary>
        /// Configura el modelo de la base de datos.
        /// Se ejecuta cuando EF Core construye el esquema de la BD.
        /// </summary>
        /// <param name="modelBuilder">Constructor del modelo de base de datos</param>
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Aquí configuraremos:
            // - Nombres de tablas
            // - Relaciones entre tablas
            // - Índices
            // - Valores por defecto
            // - Restricciones (constraints)

            // Configuración global: Establecer valores por defecto para FechaCreacion
            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                // Para todas las entidades que hereden de EntidadBase
                if (typeof(EntidadBase).IsAssignableFrom(entityType.ClrType))
                {
                    // Establecer FechaCreacion automáticamente al crear
                    modelBuilder.Entity(entityType.ClrType)
                        .Property<DateTime>("FechaCreacion")
                        .HasDefaultValueSql("GETDATE()");
                    
                    // Nota: GETDATE() es una función de SQL Server que retorna la fecha/hora actual
                }
            }
        }

        /// <summary>
        /// Se ejecuta antes de guardar cambios en la base de datos.
        /// Aquí establecemos automáticamente las fechas de auditoría.
        /// </summary>
        public override int SaveChanges()
        {
            // Obtener todas las entidades que están siendo modificadas
            var entradas = ChangeTracker.Entries<EntidadBase>();

            foreach (var entrada in entradas)
            {
                // Si es una entidad nueva (se está insertando)
                if (entrada.State == EntityState.Added)
                {
                    entrada.Entity.FechaCreacion = DateTime.Now;
                    entrada.Entity.Activo = true;
                }
                // Si es una entidad que se está modificando
                else if (entrada.State == EntityState.Modified)
                {
                    entrada.Entity.FechaModificacion = DateTime.Now;
                }
            }

            // Ejecutar el guardado normal
            return base.SaveChanges();
        }

        /// <summary>
        /// Versión asíncrona de SaveChanges.
        /// Se usa para no bloquear el hilo principal mientras se guarda.
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