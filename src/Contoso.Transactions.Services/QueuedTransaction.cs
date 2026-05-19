namespace Contoso.Transactions.Services
{
    public sealed record QueuedTransaction(
        Guid RequestId,
        decimal Amount,
        string? Description,
        DateTimeOffset RequestedAtUtc);
}
