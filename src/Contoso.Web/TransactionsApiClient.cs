using System.ComponentModel.DataAnnotations.Schema;

namespace Contoso.Web;

public class TransactionsApiClient(HttpClient httpClient)
{
    public async Task<ItemsResponse<Transaction>> GetTransactionsAsync(DateTime date,int page,int maxItems = 10, CancellationToken cancellationToken = default)
    {
        string formattedDate = date.ToString("yyyy-MM-dd");
        return await httpClient.GetFromJsonAsync<ItemsResponse<Transaction>>($"/lancamentos/{formattedDate}?limit={maxItems}", cancellationToken) ?? new ItemsResponse<Transaction>
        {
            Items = [],
            TotalCount = 0,
            TotalPages = 0
        };
    }

    public record Transaction
    {
        public int Id { get; set; }
        public DateTimeOffset ReceivedAt { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public string? Description { get; set; }
        public decimal Value { get; set; }
    }


    public record ItemsResponse<T>
    {
        public IEnumerable<T> Items { get; set; }
        public int TotalCount { get; set; }
        public int TotalPages { get; set; }
    }
}
