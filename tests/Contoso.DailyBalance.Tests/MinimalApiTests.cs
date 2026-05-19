namespace Contoso.DailyBalance.Tests
{
    using Contoso.DailyBalance.API;
    using Contoso.DailyBalance.Services;
    using Contoso.Data;
    using Contoso.Data.Entities;
    using Microsoft.AspNetCore.Mvc.Testing;
    using Microsoft.Data.SqlClient;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.DependencyInjection.Extensions;
    using Moq;
    using System.Net.Http.Json;
    using Testcontainers.MsSql;
    using Testcontainers.Redis;

    public class MinimalApiTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>, IAsyncLifetime
    {
        private static readonly DateTimeOffset s_seedTransactionTimestamp = new(2026, 5, 18, 12, 0, 0, TimeSpan.Zero);

        private readonly MsSqlContainer _msSqlContainer = new MsSqlBuilder().Build();
        private readonly RedisContainer _redisContainer = new RedisBuilder().Build();

        public async Task InitializeAsync()
        {
            await _msSqlContainer.StartAsync();
            await _redisContainer.StartAsync();
        }

        public Task DisposeAsync() =>
            Task.WhenAll(
                _msSqlContainer.DisposeAsync().AsTask(),
                _redisContainer.DisposeAsync().AsTask());

        [Fact]
        public async Task GetBalance_ReturnsBalanceAndUsesCache()
        {
            await using var connection = new SqlConnection(_msSqlContainer.GetConnectionString());
            await connection.OpenAsync();

            var mockTimeProvider = new Mock<TimeProvider>();
            var redisConnectionString = _redisContainer.GetConnectionString();
            var referenceDate = new DateTimeOffset(2026, 5, 18, 10, 0, 0, TimeSpan.Zero);

            var client = factory.WithWebHostBuilder(builder =>
            {
                builder.UseSetting("ConnectionStrings:cache", redisConnectionString);
                builder.ConfigureServices(services =>
                {
                    services.RemoveAll<DbContextOptions<ContosoDbContext>>();
                    services.AddDbContextPool<ContosoDbContext>(options => options.UseSqlServer(connection));

                    services.RemoveAll<TimeProvider>();
                    services.AddSingleton(mockTimeProvider.Object);

                    var serviceProvider = services.BuildServiceProvider();
                    using var scope = serviceProvider.CreateScope();
                    var db = scope.ServiceProvider.GetRequiredService<ContosoDbContext>();
                    db.Database.Migrate();
                    db.Transactions.AddRange(
                        new Transaction { CreatedAt = s_seedTransactionTimestamp, Value = 50 },
                        new Transaction { CreatedAt = s_seedTransactionTimestamp.AddHours(1), Value = 50 });
                    db.SaveChanges();
                });
            }).CreateClient();

            client.DefaultRequestHeaders.Add("X-API-KEY", "contoso");
            mockTimeProvider.Setup(tp => tp.GetUtcNow()).Returns(referenceDate);

            var response = await GetBalanceResponseAsync(client, referenceDate);

            Assert.NotNull(response);
            Assert.Equal(100, response.Balance);
            Assert.Equal(new DateTimeOffset(2026, 5, 18, 0, 0, 0, TimeSpan.Zero), response.BalanceDate);
            Assert.False(response.IsFromCache);

            using (var db = new ContosoDbContext(new DbContextOptionsBuilder<ContosoDbContext>().UseSqlServer(connection).Options))
            {
                db.Transactions.Add(new Transaction { CreatedAt = s_seedTransactionTimestamp.AddHours(2), Value = 100 });
                db.SaveChanges();
            }

            var cachedResponse = await GetBalanceResponseAsync(client, referenceDate);

            Assert.NotNull(cachedResponse);
            Assert.Equal(100, cachedResponse.Balance);
            Assert.True(cachedResponse.IsFromCache);
        }

        private static async Task<GetBalanceResponse?> GetBalanceResponseAsync(HttpClient client, DateTimeOffset date)
        {
            var formattedDate = date.ToString("yyyy-MM-dd");
            return await client.GetFromJsonAsync<GetBalanceResponse>($"/consolidado/{formattedDate}");
        }
    }
}
