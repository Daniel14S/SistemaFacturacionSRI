using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using SistemaFacturacionSRI.Infrastructure.Models;

namespace SistemaFacturacionSRI.Infrastructure.Contexts;

public partial class SistemaContext : DbContext
{
    public SistemaContext()
    {
    }

    public SistemaContext(DbContextOptions<SistemaContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Categoria> Categorias { get; set; }

    public virtual DbSet<Cliente> Clientes { get; set; }

    public virtual DbSet<Factura> Facturas { get; set; }

    public virtual DbSet<FacturaDetalle> FacturaDetalles { get; set; }

    public virtual DbSet<Lote> Lotes { get; set; }

    public virtual DbSet<Producto> Productos { get; set; }

    public virtual DbSet<Role> Roles { get; set; }

    public virtual DbSet<TiposIdentificacion> TiposIdentificacions { get; set; }

    public virtual DbSet<TiposIva> TiposIvas { get; set; }

    public virtual DbSet<Usuario> Usuarios { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        // La configuración de la conexión se maneja desde Program.cs
        // No es necesario configurar aquí si ya se inyecta el DbContext
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Categoria>(entity =>
        {
            entity.HasIndex(e => e.Codigo, "IX_Categorias_Codigo").IsUnique();

            entity.Property(e => e.Codigo).HasMaxLength(50);
            entity.Property(e => e.Descripcion).HasMaxLength(250);
            entity.Property(e => e.FechaCreacion).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.Nombre).HasMaxLength(100);
        });

        modelBuilder.Entity<Cliente>(entity =>
        {
            entity.HasIndex(e => e.Identificacion, "IX_Clientes_Identificacion").IsUnique();

            entity.HasIndex(e => e.TipoIdentificacionId, "IX_Clientes_TipoIdentificacionId");

            entity.Property(e => e.Apellido1).HasMaxLength(100);
            entity.Property(e => e.Apellido2).HasMaxLength(100);
            entity.Property(e => e.Direccion).HasMaxLength(500);
            entity.Property(e => e.Email).HasMaxLength(100);
            entity.Property(e => e.Estado).HasDefaultValue(true);
            entity.Property(e => e.Identificacion).HasMaxLength(20);
            entity.Property(e => e.Nombre1).HasMaxLength(100);
            entity.Property(e => e.Nombre2).HasMaxLength(100);
            entity.Property(e => e.Telefono).HasMaxLength(10);

            entity.HasOne(d => d.TipoIdentificacion).WithMany(p => p.Clientes)
                .HasForeignKey(d => d.TipoIdentificacionId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Cliente_TipoIdentificacion");
        });

        modelBuilder.Entity<Factura>(entity =>
        {
            entity.HasIndex(e => e.ClaveAccesoSri, "IX_Facturas_ClaveAccesoSRI")
                .IsUnique()
                .HasFilter("([ClaveAccesoSRI] IS NOT NULL)");

            entity.HasIndex(e => e.ClienteId, "IX_Facturas_ClienteId");

            entity.HasIndex(e => e.NumeroFactura, "IX_Facturas_NumeroFactura").IsUnique();

            entity.HasIndex(e => e.UsuarioId, "IX_Facturas_UsuarioId");

            entity.Property(e => e.ClaveAccesoSri)
                .HasMaxLength(49)
                .HasColumnName("ClaveAccesoSRI");
            entity.Property(e => e.Estado)
                .HasMaxLength(20)
                .HasDefaultValue("Emitida");
            entity.Property(e => e.EstadoSri)
                .HasMaxLength(30)
                .HasDefaultValue("Pendiente")
                .HasColumnName("EstadoSRI");
            entity.Property(e => e.FechaEmision).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.NumeroFactura).HasMaxLength(50);
            entity.Property(e => e.Subtotal).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.Total).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.TotalIva)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("TotalIVA");

            entity.HasOne(d => d.Cliente).WithMany(p => p.Facturas)
                .HasForeignKey(d => d.ClienteId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Factura_Cliente");

            entity.HasOne(d => d.Usuario).WithMany(p => p.Facturas)
                .HasForeignKey(d => d.UsuarioId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Factura_Usuario");
        });

        modelBuilder.Entity<FacturaDetalle>(entity =>
        {
            entity.HasIndex(e => e.FacturaId, "IX_FacturaDetalles_FacturaId");

            entity.HasIndex(e => e.ProductoId, "IX_FacturaDetalles_ProductoId");

            entity.Property(e => e.Descuento).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.IvaLinea).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.PrecioUnitario).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.SubtotalLinea).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.TotalLinea).HasColumnType("decimal(18, 2)");

            entity.HasOne(d => d.Factura).WithMany(p => p.FacturaDetalles)
                .HasForeignKey(d => d.FacturaId)
                .HasConstraintName("FK_Detalle_Factura");

            entity.HasOne(d => d.Producto).WithMany(p => p.FacturaDetalles)
                .HasForeignKey(d => d.ProductoId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Detalle_Producto");
        });

        modelBuilder.Entity<Lote>(entity =>
        {
            entity.HasIndex(e => e.ProductoId, "IX_Lotes_ProductoId");

            entity.Property(e => e.PrecioCosto).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.Pvp)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("PVP");

            entity.HasOne(d => d.Producto).WithMany(p => p.Lotes)
                .HasForeignKey(d => d.ProductoId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Lote_Producto");
        });

        modelBuilder.Entity<Producto>(entity =>
        {
            entity.HasIndex(e => e.CategoriaId, "IX_Productos_CategoriaId");

            entity.HasIndex(e => e.Codigo, "IX_Productos_Codigo").IsUnique();

            entity.Property(e => e.Codigo).HasMaxLength(50);
            entity.Property(e => e.Descripcion).HasMaxLength(1000);
            entity.Property(e => e.FechaCreacion).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.Nombre).HasMaxLength(200);

            entity.HasOne(d => d.Categoria).WithMany(p => p.Productos)
                .HasForeignKey(d => d.CategoriaId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(e => e.RolId);

            entity.HasIndex(e => e.NombreRol, "IX_Roles_NombreRol").IsUnique();

            entity.Property(e => e.NombreRol).HasMaxLength(50);
        });

        modelBuilder.Entity<TiposIdentificacion>(entity =>
        {
            entity.HasKey(e => e.TipoIdentificacionId);

            entity.ToTable("TiposIdentificacion");

            entity.Property(e => e.CodigoSri)
                .HasMaxLength(2)
                .IsFixedLength()
                .HasColumnName("CodigoSRI");
            entity.Property(e => e.Nombre).HasMaxLength(100);
        });

        modelBuilder.Entity<TiposIva>(entity =>
        {
            entity.HasKey(e => e.TipoIvaid);

            entity.ToTable("TiposIVA");

            entity.Property(e => e.TipoIvaid).HasColumnName("TipoIVAId");
            entity.Property(e => e.Descripcion).HasMaxLength(100);
            entity.Property(e => e.Porcentaje).HasColumnType("decimal(5, 2)");
        });

        modelBuilder.Entity<Usuario>(entity =>
        {
            entity.HasIndex(e => e.Cedula, "IX_Usuarios_Cedula").IsUnique();

            entity.HasIndex(e => e.Email, "IX_Usuarios_Email").IsUnique();

            entity.HasIndex(e => e.RolId, "IX_Usuarios_RolId");

            entity.HasIndex(e => e.Username, "IX_Usuarios_Username").IsUnique();

            entity.Property(e => e.Apellido1).HasMaxLength(100);
            entity.Property(e => e.Apellido2).HasMaxLength(100);
            entity.Property(e => e.Cedula).HasMaxLength(10);
            entity.Property(e => e.Direccion).HasMaxLength(200);
            entity.Property(e => e.Email).HasMaxLength(100);
            entity.Property(e => e.Nombre1).HasMaxLength(100);
            entity.Property(e => e.Nombre2).HasMaxLength(100);
            entity.Property(e => e.PasswordHash).HasMaxLength(256);
            entity.Property(e => e.Telefono).HasMaxLength(15);
            entity.Property(e => e.Username).HasMaxLength(50);

            entity.HasOne(d => d.Rol).WithMany(p => p.Usuarios)
                .HasForeignKey(d => d.RolId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Usuario_Rol");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
