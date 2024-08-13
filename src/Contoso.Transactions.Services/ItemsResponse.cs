namespace Contoso.Transactions.Services
{
    public record ItemsResponse<T>
    {
        public required IEnumerable<T> Items { get; set; }
        public int TotalCount { get; set; }
        public int TotalPages { get; set; }
    }
}
