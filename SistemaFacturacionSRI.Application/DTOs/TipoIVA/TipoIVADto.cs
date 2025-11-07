namespace SistemaFacturacionSRI.Application.DTOs.TipoIVA
{
    public class TipoIVADto
    {
        public int TipoIVAId { get; set; }
        public string Descripcion { get; set; } = string.Empty;
        public decimal Porcentaje { get; set; }
    }
}
