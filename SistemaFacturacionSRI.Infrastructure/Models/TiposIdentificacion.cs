using System;
using System.Collections.Generic;

namespace SistemaFacturacionSRI.Infrastructure.Models;

public partial class TiposIdentificacion
{
    public int TipoIdentificacionId { get; set; }

    public string CodigoSri { get; set; } = null!;

    public string Nombre { get; set; } = null!;

    public virtual ICollection<Cliente> Clientes { get; set; } = new List<Cliente>();
}
