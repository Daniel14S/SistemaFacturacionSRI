using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SistemaFacturacionSRI.Domain.Entities;
using SistemaFacturacionSRI.Domain.Enums;

namespace SistemaFacturacionSRI.Infrastructure.Data.Configurations
{
    public class FacturaConfiguration : IEntityTypeConfiguration<Factura>
    {
        public void Configure(EntityTypeBuilder<Factura> builder)
        {
            builder.ToTable("Facturas");
            builder.HasKey(f => f.Id);

            builder.Property(f => f.NumeroFactura)
                .IsRequired()
                .HasMaxLength(50);

            builder.Property(f => f.ClaveAcceso)
                .HasMaxLength(49);

            builder.HasIndex(f => f.NumeroFactura)
                .IsUnique();

            builder.HasIndex(f => f.ClaveAcceso)
                .IsUnique()
                .HasFilter("([ClaveAcceso] IS NOT NULL)");

            builder.Property(f => f.FechaEmision)
                .HasDefaultValueSql("GETDATE()");

            builder.Property(f => f.Ambiente)
                .HasConversion<string>()
                .HasMaxLength(20)
                .HasDefaultValue(Ambiente.PRUEBAS);

            builder.Property(f => f.TipoEmision)
                .HasConversion<string>()
                .HasMaxLength(20)
                .HasDefaultValue(TipoEmision.NORMAL);

            builder.Property(f => f.Estado)
                .HasConversion<string>()
                .HasMaxLength(20)
                .HasDefaultValue(EstadoFactura.BORRADOR);

            builder.Property(f => f.Subtotal0).HasColumnType("DECIMAL(18,2)");
            builder.Property(f => f.Subtotal12).HasColumnType("DECIMAL(18,2)");
            builder.Property(f => f.Subtotal15).HasColumnType("DECIMAL(18,2)");
            builder.Property(f => f.SubtotalNoObjetoIVA).HasColumnType("DECIMAL(18,2)");
            builder.Property(f => f.SubtotalExentoIVA).HasColumnType("DECIMAL(18,2)");
            builder.Property(f => f.SubtotalConDescuento).HasColumnType("DECIMAL(18,2)");
            builder.Property(f => f.Descuento).HasColumnType("DECIMAL(18,2)");
            builder.Property(f => f.IVA12).HasColumnType("DECIMAL(18,2)");
            builder.Property(f => f.IVA15).HasColumnType("DECIMAL(18,2)");
            builder.Property(f => f.Propina).HasColumnType("DECIMAL(18,2)");
            builder.Property(f => f.ImporteTotal).HasColumnType("DECIMAL(18,2)");

            builder.Property(f => f.NumeroAutorizacion)
                .HasMaxLength(100);

            builder.Property(f => f.XmlPath).HasMaxLength(500);
            builder.Property(f => f.XmlFirmadoPath).HasMaxLength(500);
            builder.Property(f => f.PdfPath).HasMaxLength(500);
            builder.Property(f => f.MensajesSRI).HasMaxLength(2000);
            builder.Property(f => f.Observaciones).HasMaxLength(2000);

            builder.Property(f => f.FechaCreacion)
                .HasDefaultValueSql("GETDATE()");

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

            builder.HasMany(f => f.Detalles)
                .WithOne(d => d.Factura!)
                .HasForeignKey(d => d.FacturaId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_Factura_Detalles");

            builder.HasMany(f => f.InformacionAdicional)
                .WithOne(i => i.Factura!)
                .HasForeignKey(i => i.FacturaId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_Factura_InfoAdicional");
        }
    }
}
