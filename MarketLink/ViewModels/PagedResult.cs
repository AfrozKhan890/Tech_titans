namespace MarketLink.ViewModels
{
    /// <summary>
    /// Non-generic paging metadata, used by the shared _Pagination.cshtml
    /// partial so it can render page links for any PagedResult&lt;T&gt;
    /// without Razor's @model needing to match the item type.
    /// </summary>
    public class PagingMeta
    {
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public int TotalCount { get; set; }

        public int TotalPages => TotalCount == 0 ? 1 : (int)Math.Ceiling(TotalCount / (double)PageSize);
        public bool HasPreviousPage => PageNumber > 1;
        public bool HasNextPage => PageNumber < TotalPages;
    }

    /// <summary>
    /// Generic paging wrapper reused by every management list page
    /// (Farmers, Customers, Markets, Products, Orders, Reviews, ...).
    /// </summary>
    public class PagedResult<T>
    {
        public List<T> Items { get; set; } = new();
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public int TotalCount { get; set; }

        public int TotalPages => TotalCount == 0 ? 1 : (int)Math.Ceiling(TotalCount / (double)PageSize);
        public bool HasPreviousPage => PageNumber > 1;
        public bool HasNextPage => PageNumber < TotalPages;

        public PagingMeta ToPagingMeta() => new PagingMeta
        {
            PageNumber = PageNumber,
            PageSize = PageSize,
            TotalCount = TotalCount
        };
    }
}
