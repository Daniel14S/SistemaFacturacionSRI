namespace SistemaFacturacionSRI.Domain.DTOs.Common
{
    /// <summary>
    /// DTO genérico para manejar paginación en listados.
    /// </summary>
    public class PaginacionDto
    {
        private const int MaxPageSize = 100;
        private int _pageSize = 10;

        /// <summary>
        /// Número de página (base 1)
        /// </summary>
        public int PageNumber { get; set; } = 1;

        /// <summary>
        /// Tamaño de página (cantidad de registros por página)
        /// </summary>
        public int PageSize
        {
            get => _pageSize;
            set => _pageSize = (value > MaxPageSize) ? MaxPageSize : value;
        }

        /// <summary>
        /// Campo por el cual ordenar
        /// </summary>
        public string? OrderBy { get; set; }

        /// <summary>
        /// Orden ascendente (true) o descendente (false)
        /// </summary>
        public bool OrderAscending { get; set; } = true;
    }

    /// <summary>
    /// Resultado paginado genérico
    /// </summary>
    public class PagedResultDto<T>
    {
        public List<T> Items { get; set; } = new();
        public int TotalItems { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalPages => (int)Math.Ceiling(TotalItems / (double)PageSize);
        public bool HasPreviousPage => PageNumber > 1;
        public bool HasNextPage => PageNumber < TotalPages;
    }
}