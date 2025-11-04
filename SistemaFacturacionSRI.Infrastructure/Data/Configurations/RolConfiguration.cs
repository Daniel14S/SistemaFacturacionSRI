using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SistemaFacturacionSRI.Domain.Entities;

namespace SistemaFacturacionSRI.Infrastructure.Data.Configurations
{
    public class RolConfiguration : IEntityTypeConfiguration<Rol>
    {
        public void Configure(EntityTypeBuilder<Rol> builder)
        {
            builder.ToTable("Roles");
            builder.HasKey(r => r.RolId);

            builder.Property(r => r.NombreRol)
                .IsRequired()
                .HasMaxLength(50);

            builder.HasIndex(r => r.NombreRol).IsUnique();
        }
    }
}
