using System;
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

            builder.HasData(
                new Usuario
                {
                    UsuarioId = 1,
                    RolId = 1,
                    Username = "admin",
                    PasswordHash = "ClvD40JDLxutkv/VG3hTQ+xykGzbpqJhMQYLAI54ZlY=",
                    Nombre1 = "Admin",
                    Nombre2 = "Sistema",
                    Apellido1 = "Principal",
                    Apellido2 = null,
                    Email = "admin@sistemafacturacion.com",
                    Estado = true,
                    FechaCreacion = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    UltimoAcceso = null,
                    IntentosLogin = 0
                },
                new Usuario
                {
                    UsuarioId = 2,
                    RolId = 2,
                    Username = "vendedor1",
                    PasswordHash = "K6FhJ3pkkLQo6VCZ1hH4zmlxqxgAYleOSmCmjrxdWa8=",
                    Nombre1 = "Vendedor",
                    Nombre2 = "Demo",
                    Apellido1 = "Principal",
                    Apellido2 = null,
                    Email = "vendedor@sistemafacturacion.com",
                    Estado = true,
                    FechaCreacion = new DateTime(2024, 1, 2, 0, 0, 0, DateTimeKind.Utc),
                    UltimoAcceso = null,
                    IntentosLogin = 0
                }
            );
        }
    }
}
