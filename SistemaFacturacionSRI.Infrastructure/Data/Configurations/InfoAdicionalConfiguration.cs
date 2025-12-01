// SistemaFacturacionSRI.Infrastructure/Data/Configurations/InfoAdicionalConfiguration.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SistemaFacturacionSRI.Domain.Entities;

namespace SistemaFacturacionSRI.Infrastructure.Data.Configurations
{
    public class InfoAdicionalConfiguration : IEntityTypeConfiguration<InfoAdicional>
    {
        public void Configure(EntityTypeBuilder<InfoAdicional> builder)
        {
            builder.ToTable("InfoAdicional");
            builder.HasKey(i => i.Id);

            builder.HasIndex(i => new { i.FacturaId, i.Nombre })
                .HasDatabaseName("IX_InfoAdicional_FacturaId_Nombre");

            // CORREGIDO: Cambiar InformacionAdicional por InfoAdicional
            builder.HasOne(i => i.Factura)
                .WithMany(f => f.InfoAdicional)
                .HasForeignKey(i => i.FacturaId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Property(i => i.Nombre).IsRequired().HasMaxLength(100);
            builder.Property(i => i.Valor).IsRequired().HasMaxLength(500);
        }
    }
}