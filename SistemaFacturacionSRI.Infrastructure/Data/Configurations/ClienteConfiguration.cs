using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SistemaFacturacionSRI.Domain.Entities;

namespace SistemaFacturacionSRI.Infrastructure.Data.Configurations
{
    public class ClienteConfiguration : IEntityTypeConfiguration<Cliente>
    {
        public void Configure(EntityTypeBuilder<Cliente> builder)
        {
            builder.ToTable("Clientes");
            builder.HasKey(c => c.ClienteId);

            builder.Property(c => c.Identificacion).IsRequired().HasMaxLength(20);
            builder.Property(c => c.Nombres).IsRequired().HasMaxLength(150);
            builder.Property(c => c.Apellidos).IsRequired().HasMaxLength(150);
            builder.Property(c => c.Direccion).HasMaxLength(500);
            builder.Property(c => c.Telefono).HasMaxLength(20);
            builder.Property(c => c.Email).HasMaxLength(100);

            builder.HasIndex(c => c.Identificacion).IsUnique();

            builder.HasOne(c => c.TipoIdentificacion)
                .WithMany(t => t.Clientes)
                .HasForeignKey(c => c.TipoIdentificacionId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_Cliente_TipoIdentificacion");
        }
    }
}
