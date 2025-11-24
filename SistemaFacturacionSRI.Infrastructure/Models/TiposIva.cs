using System;
using System.Collections.Generic;

namespace SistemaFacturacionSRI.Infrastructure.Models;

public partial class TiposIva
{
    public int TipoIvaid { get; set; }

    public string Descripcion { get; set; } = null!;

    public decimal Porcentaje { get; set; }
}
