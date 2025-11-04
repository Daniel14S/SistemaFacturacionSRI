using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SistemaFacturacionSRI.Domain.Entities;

namespace SistemaFacturacionSRI.Infrastructure.Data.Configurations
{
    public class FacturaConfiguration : IEntityTypeConfiguration<Factura>
    {
        public void Configure(EntityTypeBuilder<Factura> builder)
        {
            builder.ToTable("Facturas");
            builder.HasKey(f => f.FacturaId);

            builder.Property(f => f.NumeroFactura).IsRequired().HasMaxLength(50);
            builder.Property(f => f.FechaEmision).HasDefaultValueSql("GETDATE()");
            builder.Property(f => f.ClaveAccesoSRI).HasMaxLength(49);
            builder.Property(f => f.EstadoSRI).IsRequired().HasMaxLength(30).HasDefaultValue("Pendiente");
            builder.Property(f => f.Estado).IsRequired().HasMaxLength(20).HasDefaultValue("Emitida");

            builder.Property(f => f.Subtotal).IsRequired().HasColumnType("DECIMAL(18,2)");
            builder.Property(f => f.TotalIVA).IsRequired().HasColumnType("DECIMAL(18,2)");
            builder.Property(f => f.Total).IsRequired().HasColumnType("DECIMAL(18,2)");

            builder.HasIndex(f => f.NumeroFactura).IsUnique();
            builder.HasIndex(f => f.ClaveAccesoSRI).IsUnique();

            builder.HasOne(f => f.Cliente)
                .WithMany()
                .HasForeignKey(f => f.ClienteId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_Factura_Cliente");

            builder.HasOne(f => f.Usuario)
                .WithMany()
                .HasForeignKey(f => f.UsuarioId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_Factura_Usuario");
        }
    }
}
