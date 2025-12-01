using Microsoft.EntityFrameworkCore;
using SistemaFacturacionSRI.Domain.Entities;
using SistemaFacturacionSRI.Infrastructure.Data.Configurations;
using SistemaFacturacionSRI.Infrastructure.Data.Seeds;

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
        /// SPRINT 1: Solo Productos está activo
        /// </summary>
        public DbSet<Producto> Productos { get; set; }
        
        public DbSet<Rol> Roles { get; set; }
        public DbSet<Usuario> Usuarios { get; set; }
        public DbSet<TipoIdentificacion> TiposIdentificacion { get; set; }
        public DbSet<Cliente> Clientes { get; set; }
        public DbSet<TipoIVACatalogo> TiposIVA { get; set; }
        public DbSet<Categoria> Categorias { get; set; }
        public DbSet<Lote> Lotes { get; set; }
        public DbSet<Factura> Facturas { get; set; }
        public DbSet<DetalleFactura> DetalleFacturas { get; set; }
        public DbSet<InfoAdicional> InfoAdicional { get; set; }
        public DbSet<SecuenciaFactura> SecuenciasFactura { get; set; }
        
        /// <summary>
        /// Tabla de configuración de la empresa emisora
        /// </summary>
        public DbSet<ConfiguracionEmpresa> ConfiguracionEmpresa { get; set; }

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

            // Registra todas las configuraciones Fluent API ubicadas en el ensamblado Infrastructure
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

            // Restricción de unicidad para la identificación de clientes
            modelBuilder.Entity<Cliente>()
                .HasIndex(c => c.Identificacion)
                .IsUnique();

            modelBuilder.Entity<Factura>(entity =>
            {
                entity.ToTable("Facturas");
                entity.HasKey(f => f.Id);
                
                // Índices
                entity.HasIndex(f => f.ClaveAcceso)
                    .IsUnique()
                    .HasDatabaseName("IX_Facturas_ClaveAcceso");
                
                entity.HasIndex(f => f.NumeroFactura)
                    .IsUnique()
                    .HasDatabaseName("IX_Facturas_NumeroFactura");
                
                entity.HasIndex(f => f.FechaEmision)
                    .HasDatabaseName("IX_Facturas_FechaEmision");
                
                entity.HasIndex(f => f.Estado)
                    .HasDatabaseName("IX_Facturas_Estado");
                
                // Relación con Cliente
                entity.HasOne(f => f.Cliente)
                    .WithMany()
                    .HasForeignKey(f => f.ClienteId)
                    .OnDelete(DeleteBehavior.Restrict);
                
                // Relación con Usuario
                entity.HasOne(f => f.Usuario)
                    .WithMany()
                    .HasForeignKey(f => f.UsuarioId)
                    .OnDelete(DeleteBehavior.Restrict);
                
                // Relación con DetalleFactura (UNO A MUCHOS)
                entity.HasMany(f => f.Detalles)
                    .WithOne(d => d.Factura)
                    .HasForeignKey(d => d.FacturaId)
                    .OnDelete(DeleteBehavior.Cascade);  // Si eliminas factura, elimina detalles
                
                // Relación con InfoAdicional
                entity.HasMany(f => f.InfoAdicional)
                    .WithOne(i => i.Factura)
                    .HasForeignKey(i => i.FacturaId)
                    .OnDelete(DeleteBehavior.Cascade);
                
                // Precisión para decimales
                entity.Property(f => f.Subtotal0).HasPrecision(18, 2);
                entity.Property(f => f.Subtotal12).HasPrecision(18, 2);
                entity.Property(f => f.Subtotal15).HasPrecision(18, 2);
                entity.Property(f => f.SubtotalNoObjetoIVA).HasPrecision(18, 2);
                entity.Property(f => f.SubtotalExentoIVA).HasPrecision(18, 2);
                entity.Property(f => f.SubtotalConDescuento).HasPrecision(18, 2);
                entity.Property(f => f.Descuento).HasPrecision(18, 2);
                entity.Property(f => f.IVA12).HasPrecision(18, 2);
                entity.Property(f => f.IVA15).HasPrecision(18, 2);
                entity.Property(f => f.Propina).HasPrecision(18, 2);
                entity.Property(f => f.ImporteTotal).HasPrecision(18, 2);
                
                // Campos requeridos
                entity.Property(f => f.NumeroFactura).IsRequired().HasMaxLength(17);
                entity.Property(f => f.ClaveAcceso).IsRequired().HasMaxLength(49);
                entity.Property(f => f.Ambiente).IsRequired().HasMaxLength(20);
                entity.Property(f => f.TipoEmision).IsRequired().HasMaxLength(20);
                entity.Property(f => f.Estado).IsRequired().HasMaxLength(20);
            });

            modelBuilder.Entity<DetalleFactura>(entity =>
            {
                entity.ToTable("FacturaDetalles");
                entity.HasKey(d => d.Id);
                
                // Índice compuesto
                entity.HasIndex(d => new { d.FacturaId, d.ProductoId })
                    .HasDatabaseName("IX_FacturaDetalles_FacturaId_ProductoId");
                
                // Relación con Factura (YA CONFIGURADA ARRIBA, PERO SE PUEDE ESPECIFICAR AQUÍ TAMBIÉN)
                entity.HasOne(d => d.Factura)
                    .WithMany(f => f.Detalles)
                    .HasForeignKey(d => d.FacturaId)
                    .OnDelete(DeleteBehavior.Cascade);
                
                // Relación con Producto (opcional)
                entity.HasOne(d => d.Producto)
                    .WithMany()
                    .HasForeignKey(d => d.ProductoId)
                    .OnDelete(DeleteBehavior.Restrict)
                    .IsRequired(false);
                
                // Precisión para decimales
                entity.Property(d => d.Cantidad).HasPrecision(18, 2);
                entity.Property(d => d.PrecioUnitario).HasPrecision(18, 2);
                entity.Property(d => d.Descuento).HasPrecision(18, 2);
                entity.Property(d => d.PrecioTotalSinImpuesto).HasPrecision(18, 2);
                entity.Property(d => d.Tarifa).HasPrecision(5, 2);
                entity.Property(d => d.BaseImponible).HasPrecision(18, 2);
                entity.Property(d => d.Valor).HasPrecision(18, 2);
                entity.Property(d => d.ValorTotal).HasPrecision(18, 2);
                
                // Campos requeridos
                entity.Property(d => d.CodigoPrincipal).IsRequired().HasMaxLength(50);
                entity.Property(d => d.Descripcion).IsRequired().HasMaxLength(300);
                entity.Property(d => d.CodigoAuxiliar).HasMaxLength(50);
            });
            
            modelBuilder.Entity<InfoAdicional>(entity =>
            {
                entity.ToTable("InfoAdicional");
                entity.HasKey(i => i.Id);
                
                entity.HasIndex(i => new { i.FacturaId, i.Nombre })
                    .HasDatabaseName("IX_InfoAdicional_FacturaId_Nombre");
                
                entity.HasOne(i => i.Factura)
                    .WithMany(f => f.InfoAdicional)
                    .HasForeignKey(i => i.FacturaId)
                    .OnDelete(DeleteBehavior.Cascade);
                
                entity.Property(i => i.Nombre).IsRequired().HasMaxLength(100);
                entity.Property(i => i.Valor).IsRequired().HasMaxLength(500);
            });
            
            ConfiguracionEmpresaSeed.Seed(modelBuilder);
            SecuenciaFacturaSeed.Seed(modelBuilder);
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