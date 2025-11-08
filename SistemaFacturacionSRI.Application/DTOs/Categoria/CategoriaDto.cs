namespace SistemaFacturacionSRI.Application.DTOs.Categoria
{
    public class CategoriaDto
    {
        public int Id { get; set; }  // ← Cambiar de CategoriaId a Id
        public string Codigo { get; set; } = string.Empty;  // ← Agregar Codigo
        public string Nombre { get; set; } = string.Empty;
        public string? Descripcion { get; set; }  // ← Agregar Descripcion (opcional)
    }
}