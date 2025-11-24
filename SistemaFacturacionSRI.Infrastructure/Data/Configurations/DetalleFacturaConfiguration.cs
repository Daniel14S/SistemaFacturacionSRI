using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SistemaFacturacionSRI.Domain.Entities;

namespace SistemaFacturacionSRI.Infrastructure.Data.Configurations
{
    public class DetalleFacturaConfiguration : IEntityTypeConfiguration<DetalleFactura>
    {
        public void Configure(EntityTypeBuilder<DetalleFactura> builder)
        {
            builder.ToTable("FacturaDetalles");
            builder.HasKey(d => d.Id);

            builder.Property(d => d.CodigoPrincipal)
                .IsRequired()
                .HasMaxLength(50);

            builder.Property(d => d.CodigoAuxiliar)
                .HasMaxLength(50);

            builder.Property(d => d.Descripcion)
                .IsRequired()
                .HasMaxLength(500);

            builder.Property(d => d.Cantidad)
                .HasColumnType("DECIMAL(18,4)");

            builder.Property(d => d.PrecioUnitario)
                .HasColumnType("DECIMAL(18,2)");

            builder.Property(d => d.Descuento)
                .HasColumnType("DECIMAL(18,2)");

            builder.Property(d => d.PrecioTotalSinImpuesto)
                .HasColumnType("DECIMAL(18,2)");

            builder.Property(d => d.CodigoPorcentajeIVA)
                .IsRequired()
                .HasMaxLength(10);

            builder.Property(d => d.Tarifa)
                .HasColumnType("DECIMAL(5,2)");

            builder.Property(d => d.BaseImponible)
                .HasColumnType("DECIMAL(18,2)");

            builder.Property(d => d.Valor)
                .HasColumnType("DECIMAL(18,2)");

            builder.Property(d => d.ValorTotal)
                .HasColumnType("DECIMAL(18,2)");

            builder.HasIndex(d => d.FacturaId);
            builder.HasIndex(d => d.ProductoId);

            builder.HasOne(d => d.Factura)
                .WithMany(f => f.Detalles)
                .HasForeignKey(d => d.FacturaId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_DetalleFactura_Factura");

            builder.HasOne(d => d.Producto)
                .WithMany()
                .HasForeignKey(d => d.ProductoId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_DetalleFactura_Producto");
        }
    }
}
