namespace ProductManagement.Application.Responses
{
    public sealed class PagedResponse<T> where T : notnull
    {
        public IEnumerable<T> Items { get; init; } = Array.Empty<T>();
        public int PageNumber { get; init; }
        public int PageSize { get; init; }
        public long TotalItems { get; init; }
        public int TotalPages { get; init; }

        public static PagedResponse<T> Create(IEnumerable<T> items, int pageNumber, int pageSize, long totalItems)
        {
            return new PagedResponse<T>
            {
                Items = items,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalItems = totalItems,
                TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize)
            };
        }
    }
}
