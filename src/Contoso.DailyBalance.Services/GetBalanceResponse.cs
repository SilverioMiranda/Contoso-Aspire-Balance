namespace Contoso.DailyBalance.Services
{
    public class GetBalanceResponse
    {
        public decimal Balance { get; set; }
        public DateTimeOffset BalanceDate { get; set; }
        public bool? IsFromCache { get; set; } = true;
    }
}
