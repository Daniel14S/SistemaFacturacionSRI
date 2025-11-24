using System;
using System.Collections.Generic;

namespace SistemaFacturacionSRI.Infrastructure.Models;

public partial class Usuario
{
    public int UsuarioId { get; set; }

    public int RolId { get; set; }

    public string Username { get; set; } = null!;

    public string PasswordHash { get; set; } = null!;

    public string Nombre1 { get; set; } = null!;

    public string? Nombre2 { get; set; }

    public string Apellido1 { get; set; } = null!;

    public string? Apellido2 { get; set; }

    public string Email { get; set; } = null!;

    public string Cedula { get; set; } = null!;

    public string? Telefono { get; set; }

    public string? Direccion { get; set; }

    public bool Estado { get; set; }

    public DateTime FechaCreacion { get; set; }

    public DateTime? UltimoAcceso { get; set; }

    public int IntentosLogin { get; set; }

    public virtual ICollection<Factura> Facturas { get; set; } = new List<Factura>();

    public virtual Role Rol { get; set; } = null!;
}
