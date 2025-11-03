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
                .IsRequired(false);

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

            builder.Ignore(p => p.ValorIVA);
            builder.Ignore(p => p.PrecioConIVA);
            builder.Ignore(p => p.TieneStock);
            builder.Ignore(p => p.ValorInventario);
        }
    }
}