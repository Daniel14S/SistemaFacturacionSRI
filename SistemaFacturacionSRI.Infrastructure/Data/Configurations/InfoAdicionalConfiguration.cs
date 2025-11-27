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

            builder.Property(i => i.Nombre)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(i => i.Valor)
                .IsRequired()
                .HasMaxLength(500);

            builder.HasIndex(i => i.FacturaId);

            builder.HasOne(i => i.Factura)
                .WithMany(f => f.InformacionAdicional)
                .HasForeignKey(i => i.FacturaId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_InfoAdicional_Factura");
        }
    }
}
