using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SistemaFacturacionSRI.Domain.Entities;

namespace SistemaFacturacionSRI.Infrastructure.Data.Configurations
{
    public class UsuarioConfiguration : IEntityTypeConfiguration<Usuario>
    {
        public void Configure(EntityTypeBuilder<Usuario> builder)
        {
            builder.ToTable("Usuarios");
            builder.HasKey(u => u.UsuarioId);

            builder.Property(u => u.Username).IsRequired().HasMaxLength(50);
            builder.Property(u => u.PasswordHash).IsRequired().HasMaxLength(256);
            builder.Property(u => u.Email).IsRequired().HasMaxLength(100);

            builder.Property(u => u.Nombre1).IsRequired().HasMaxLength(100);
            builder.Property(u => u.Nombre2).HasMaxLength(100);
            builder.Property(u => u.Apellido1).IsRequired().HasMaxLength(100);
            builder.Property(u => u.Apellido2).HasMaxLength(100);

            builder.Property(u => u.Estado).IsRequired();
            builder.Property(u => u.FechaCreacion).IsRequired();
            builder.Property(u => u.UltimoAcceso);
            builder.Property(u => u.IntentosLogin).HasDefaultValue(0);

            builder.HasIndex(u => u.Username).IsUnique();
            builder.HasIndex(u => u.Email).IsUnique();

            builder.HasOne(u => u.Rol)
                .WithMany(r => r.Usuarios)
                .HasForeignKey(u => u.RolId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_Usuario_Rol");
        }
    }
}
