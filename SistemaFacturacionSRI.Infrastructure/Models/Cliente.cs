using System;
using System.Collections.Generic;

namespace SistemaFacturacionSRI.Infrastructure.Models;

public partial class Cliente
{
    public int ClienteId { get; set; }

    public int TipoIdentificacionId { get; set; }

    public string Identificacion { get; set; } = null!;

    public string Nombre1 { get; set; } = null!;

    public string? Nombre2 { get; set; }

    public string Apellido1 { get; set; } = null!;

    public string? Apellido2 { get; set; }

    public string? Direccion { get; set; }

    public string? Telefono { get; set; }

    public string? Email { get; set; }

    public bool Estado { get; set; }

    public virtual ICollection<Factura> Facturas { get; set; } = new List<Factura>();

    public virtual TiposIdentificacion TipoIdentificacion { get; set; } = null!;
}
