namespace SistemaFacturacionSRI.Domain.Entities
{
    public class Categoria
    {
        public int CategoriaId { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string? Descripcion { get; set; }
        /// <summary>
        /// Productos asociados a esta categoría.
        /// </summary>
        public ICollection<Producto> Productos { get; set; } = new List<Producto>();
    }
}
