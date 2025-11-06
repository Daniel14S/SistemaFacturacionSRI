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

            builder.Property(p => p.Precio)
                .IsRequired()
                .HasColumnType("DECIMAL(18,2)");

            builder.Property(p => p.TipoIVA)
                .IsRequired()
                .HasConversion<int>();

            builder.Property(p => p.Stock)
                .IsRequired()
                .HasDefaultValue(0);

            builder.Property(p => p.UnidadMedida)
                .HasMaxLength(20)
                .HasColumnType("NVARCHAR")
                .HasDefaultValue("Unidad");

            // Relaciones: FK opcional a TiposIVA (catálogo) y Categorias
            builder.HasOne(p => p.TipoIVACatalogo)
                .WithMany(t => t.Productos)
                .HasForeignKey(p => p.TipoIVAId)
                .HasConstraintName("FK_Productos_TiposIVA")
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(p => p.Categoria)
                .WithMany(c => c.Productos)
                .HasForeignKey(p => p.CategoriaId)
                .HasConstraintName("FK_Productos_Categorias")
                .OnDelete(DeleteBehavior.SetNull);

            builder.Ignore(p => p.ValorIVA);
            builder.Ignore(p => p.PrecioConIVA);
            builder.Ignore(p => p.TieneStock);
            builder.Ignore(p => p.ValorInventario);
        }
    }
}