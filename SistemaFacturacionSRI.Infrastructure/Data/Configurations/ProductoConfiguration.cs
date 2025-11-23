using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SistemaFacturacionSRI.Domain.Entities;

namespace SistemaFacturacionSRI.Infrastructure.Data.Configurations
{
    /// <summary>
    /// Configuración de mapeo de la entidad Producto usando Fluent API.
    /// </summary>
    public class ProductoConfiguration : IEntityTypeConfiguration<Producto>
    {
        public void Configure(EntityTypeBuilder<Producto> builder)
        {
            builder.ToTable("Productos");
            builder.HasKey(p => p.Id);

            builder.Property(p => p.Codigo)
                .IsRequired()
                .HasMaxLength(50)
                .HasColumnType("NVARCHAR");

            builder.HasIndex(p => p.Codigo)
                .IsUnique()
                .HasDatabaseName("IX_Productos_Codigo");

            builder.Property(p => p.Nombre)
                .IsRequired()
                .HasMaxLength(200)
                .HasColumnType("NVARCHAR");

            builder.Property(p => p.Descripcion)
                .HasMaxLength(1000)
                .HasColumnType("NVARCHAR")
                .IsRequired();

            // UnidadMedida eliminada del modelo

            // Configuración de la relación con Categoría
            builder.HasOne(p => p.Categoria)
                .WithMany(c => c.Productos)
                .HasForeignKey(p => p.CategoriaId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.HasMany(p => p.Lotes)
                .WithOne(l => l.Producto)
                .HasForeignKey(l => l.ProductoId)
                .OnDelete(DeleteBehavior.Restrict);

            // Propiedades calculadas, no mapeadas en base de datos
            builder.Ignore(p => p.PrecioActual);
            builder.Ignore(p => p.StockDisponible);
            builder.Ignore(p => p.TieneStock);
            builder.Ignore(p => p.ValorInventario);
        }
    }
}