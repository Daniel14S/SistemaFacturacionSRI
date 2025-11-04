using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SistemaFacturacionSRI.Domain.Entities;

namespace SistemaFacturacionSRI.Infrastructure.Data.Configurations
{
    public class FacturaDetalleConfiguration : IEntityTypeConfiguration<FacturaDetalle>
    {
        public void Configure(EntityTypeBuilder<FacturaDetalle> builder)
        {
            builder.ToTable("FacturaDetalles");
            builder.HasKey(d => d.FacturaDetalleId);

            builder.Property(d => d.PrecioUnitario).IsRequired().HasColumnType("DECIMAL(18,2)");
            builder.Property(d => d.SubtotalLinea).IsRequired().HasColumnType("DECIMAL(18,2)");
            builder.Property(d => d.IvaLinea).IsRequired().HasColumnType("DECIMAL(18,2)");
            builder.Property(d => d.TotalLinea).IsRequired().HasColumnType("DECIMAL(18,2)");

            builder.HasOne(d => d.Factura)
                .WithMany(f => f.Detalles)
                .HasForeignKey(d => d.FacturaId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_Detalle_Factura");

            builder.HasOne(d => d.Producto)
                .WithMany()
                .HasForeignKey(d => d.ProductoId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_Detalle_Producto");
        }
    }
}
