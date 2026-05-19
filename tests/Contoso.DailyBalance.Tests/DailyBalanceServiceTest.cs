using Contoso.CacheService;
using Contoso.DailyBalance.Services;
using Contoso.Data;
using Contoso.Data.Entities;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Moq;
using Testcontainers.MsSql;

namespace Contoso.DailyBalance.Tests
{
    public class DailyBalanceServiceTests : IAsyncLifetime
    {
        private static readonly DateTimeOffset s_seedTransactionTimestamp = new(2026, 5, 18, 12, 0, 0, TimeSpan.Zero);

        private readonly MsSqlContainer _msSqlContainer = new MsSqlBuilder().Build();
        private readonly Mock<IContosoCache> _cacheMock = new();
        private ContosoDbContext? _dbContext;

        public async Task InitializeAsync()
        {
            await _msSqlContainer.StartAsync();
            await using var connection = new SqlConnection(_msSqlContainer.GetConnectionString());
            await connection.OpenAsync();

            var options = new DbContextOptionsBuilder<ContosoDbContext>()
                .UseSqlServer(_msSqlContainer.GetConnectionString())
                .Options;

            _dbContext = new ContosoDbContext(options);
            await _dbContext.Database.MigrateAsync().ConfigureAwait(false);
            _dbContext.Transactions.AddRange(
                new Transaction { CreatedAt = s_seedTransactionTimestamp, Value = 50 },
                new Transaction { CreatedAt = s_seedTransactionTimestamp.AddHours(1), Value = 100 });
            await _dbContext.SaveChangesAsync().ConfigureAwait(false);
        }

        public Task DisposeAsync() => _msSqlContainer.DisposeAsync().AsTask();

        [Fact]
        public async Task GetBalanceAsync_ComputesBalanceAndCachesValue_WhenCacheMiss()
        {
            var referenceDate = new DateTimeOffset(2026, 5, 18, 10, 0, 0, TimeSpan.Zero);
            _cacheMock
                .Setup(cache => cache.GetBalanceAsync(DateOnly.FromDateTime(referenceDate.Date), It.IsAny<CancellationToken>()))
                .ReturnsAsync((decimal?)null);

            var service = new DailyBalanceService(_cacheMock.Object, _dbContext!);
            var response = await service.GetBalanceAsync(referenceDate.UtcDateTime, CancellationToken.None);

            Assert.Equal(150, response.Balance);
            Assert.False(response.IsFromCache);
            Assert.Equal(new DateTimeOffset(2026, 5, 18, 0, 0, 0, TimeSpan.Zero), response.BalanceDate);
            _cacheMock.Verify(
                cache => cache.SetBalanceAsync(
                    DateOnly.FromDateTime(referenceDate.Date),
                    150m,
                    TimeSpan.FromSeconds(DailyBalanceService.CacheExpirationInSeconds),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task GetBalanceAsync_ReturnsCachedBalance_WhenCacheHit()
        {
            var referenceDate = new DateTimeOffset(2026, 5, 18, 10, 0, 0, TimeSpan.Zero);
            _cacheMock
                .Setup(cache => cache.GetBalanceAsync(DateOnly.FromDateTime(referenceDate.Date), It.IsAny<CancellationToken>()))
                .ReturnsAsync(150m);

            var service = new DailyBalanceService(_cacheMock.Object, _dbContext!);
            var response = await service.GetBalanceAsync(referenceDate.UtcDateTime, CancellationToken.None);

            Assert.Equal(150, response.Balance);
            Assert.True(response.IsFromCache);
            _cacheMock.Verify(
                cache => cache.SetBalanceAsync(
                    It.IsAny<DateOnly>(),
                    It.IsAny<decimal>(),
                    It.IsAny<TimeSpan>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);
        }
    }
}
