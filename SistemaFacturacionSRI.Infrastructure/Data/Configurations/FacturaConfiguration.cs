// SistemaFacturacionSRI.Infrastructure/Data/Configurations/FacturaConfiguration.cs
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

            // Índices
            builder.HasIndex(f => f.ClaveAcceso)
                .IsUnique()
                .HasDatabaseName("IX_Facturas_ClaveAcceso");

            builder.HasIndex(f => f.NumeroFactura)
                .IsUnique()
                .HasDatabaseName("IX_Facturas_NumeroFactura");

            builder.HasIndex(f => f.FechaEmision)
                .HasDatabaseName("IX_Facturas_FechaEmision");

            builder.HasIndex(f => f.Estado)
                .HasDatabaseName("IX_Facturas_Estado");

            // Relación con Cliente
            builder.HasOne(f => f.Cliente)
                .WithMany()
                .HasForeignKey(f => f.ClienteId)
                .OnDelete(DeleteBehavior.Restrict);

            // Relación con Usuario
            builder.HasOne(f => f.Usuario)
                .WithMany()
                .HasForeignKey(f => f.UsuarioId)
                .OnDelete(DeleteBehavior.Restrict);

            // Relación con DetalleFactura
            builder.HasMany(f => f.Detalles)
                .WithOne(d => d.Factura)
                .HasForeignKey(d => d.FacturaId)
                .OnDelete(DeleteBehavior.Cascade);

            // CORREGIDO: Cambiar InformacionAdicional por InfoAdicional
            builder.HasMany(f => f.InfoAdicional)
                .WithOne(i => i.Factura)
                .HasForeignKey(i => i.FacturaId)
                .OnDelete(DeleteBehavior.Cascade);

            // IMPORTANTE: Configurar ENUMs como strings en la BD
            builder.Property(f => f.Ambiente)
                .HasConversion<string>()
                .HasMaxLength(20)
                .IsRequired();

            builder.Property(f => f.TipoEmision)
                .HasConversion<string>()
                .HasMaxLength(20)
                .IsRequired();

            builder.Property(f => f.Estado)
                .HasConversion<string>()
                .HasMaxLength(20)
                .IsRequired()
                .HasDefaultValue(EstadoFactura.BORRADOR);

            // Precisión para decimales
            builder.Property(f => f.Subtotal0).HasPrecision(18, 2);
            builder.Property(f => f.Subtotal15).HasPrecision(18, 2);
            builder.Property(f => f.SubtotalNoObjetoIVA).HasPrecision(18, 2);
            builder.Property(f => f.SubtotalExentoIVA).HasPrecision(18, 2);
            builder.Property(f => f.SubtotalConDescuento).HasPrecision(18, 2);
            builder.Property(f => f.Descuento).HasPrecision(18, 2);
            builder.Property(f => f.IVA15).HasPrecision(18, 2);
            builder.Property(f => f.Propina).HasPrecision(18, 2);
            builder.Property(f => f.ImporteTotal).HasPrecision(18, 2);

            // Campos requeridos
            builder.Property(f => f.NumeroFactura).IsRequired().HasMaxLength(17);
            builder.Property(f => f.ClaveAcceso).IsRequired().HasMaxLength(49);

            // Campos opcionales
            builder.Property(f => f.Subtotal0).HasColumnType("DECIMAL(18,2)");
            builder.Property(f => f.Subtotal15).HasColumnType("DECIMAL(18,2)");
            builder.Property(f => f.SubtotalNoObjetoIVA).HasColumnType("DECIMAL(18,2)");
            builder.Property(f => f.SubtotalExentoIVA).HasColumnType("DECIMAL(18,2)");
            builder.Property(f => f.SubtotalConDescuento).HasColumnType("DECIMAL(18,2)");
            builder.Property(f => f.Descuento).HasColumnType("DECIMAL(18,2)");
            builder.Property(f => f.IVA15).HasColumnType("DECIMAL(18,2)");
            builder.Property(f => f.Propina).HasColumnType("DECIMAL(18,2)");
            builder.Property(f => f.ImporteTotal).HasColumnType("DECIMAL(18,2)");

            builder.Property(f => f.NumeroAutorizacion)
                .HasMaxLength(100);

            builder.Property(f => f.XmlPath).HasMaxLength(500);
            builder.Property(f => f.XmlFirmadoPath).HasMaxLength(500);
            builder.Property(f => f.PdfPath).HasMaxLength(500);
        }
    }
}