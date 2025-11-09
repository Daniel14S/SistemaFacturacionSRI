using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SistemaFacturacionSRI.Domain.Entities;

namespace SistemaFacturacionSRI.Infrastructure.Data.Configurations
{
    public class LoteConfiguration : IEntityTypeConfiguration<Lote>
    {
        public void Configure(EntityTypeBuilder<Lote> builder)
        {
            builder.ToTable("Lotes");
            builder.HasKey(l => l.LoteId);

            builder.Property(l => l.PrecioCosto).IsRequired().HasColumnType("DECIMAL(18,2)");

            builder.HasOne(l => l.Producto)
                .WithMany(p => p.Lotes)
                .HasForeignKey(l => l.ProductoId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_Lote_Producto");
        }
    }
}
