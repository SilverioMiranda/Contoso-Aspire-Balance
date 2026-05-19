namespace Contoso.Web;

public class TransactionsApiClient(HttpClient httpClient)
{
    public async Task<ItemsResponse<Transaction>> GetTransactionsAsync(DateTime date, int page, int maxItems = 10, CancellationToken cancellationToken = default)
    {
        var formattedDate = date.ToString("yyyy-MM-dd");
        return await httpClient.GetFromJsonAsync<ItemsResponse<Transaction>>(
                $"/lancamentos/{formattedDate}?page={page}&limit={maxItems}",
                cancellationToken)
            ?? new ItemsResponse<Transaction>();
    }

    public sealed record Transaction
    {
        public int Id { get; init; }
        public DateTimeOffset CreatedAt { get; init; }
        public string? Description { get; init; }
        public decimal Value { get; init; }
    }

    public sealed record ItemsResponse<T>
    {
        public IReadOnlyList<T> Items { get; init; } = [];
        public int TotalCount { get; init; }
        public int TotalPages { get; init; }
    }
}
