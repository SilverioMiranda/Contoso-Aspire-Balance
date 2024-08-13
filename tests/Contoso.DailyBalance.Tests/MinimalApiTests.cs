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
    using System;
    using System.Net.Http;
    using System.Net.Http.Json;
    using System.Threading.Tasks;
    using Testcontainers.MsSql;
    using Testcontainers.Redis;
    using Xunit;

    public class MinimalApiTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>, IAsyncLifetime
    {
        private readonly MsSqlContainer _msSqlContainer
            = new MsSqlBuilder().Build();
        // Configuração do Redis Testcontainer
        private readonly RedisContainer _redisContainer = new RedisBuilder().Build();

        public async Task InitializeAsync(){
            await _msSqlContainer.StartAsync();
            await _redisContainer.StartAsync();
        }

        public Task DisposeAsync(){
            return Task.WhenAll(
                _msSqlContainer.DisposeAsync().AsTask(),
                _redisContainer.DisposeAsync().AsTask()
            );
        }

        [Fact]
        public async Task GetBalance_ReturnsCorrectBalance()
        {
            await using var connection = new SqlConnection(_msSqlContainer.GetConnectionString());
            await connection.OpenAsync();

            // Mocking TimeProvider
            var mockTimeProvider = new Mock<TimeProvider>();

            var redisConnString = _redisContainer.GetConnectionString();
            // Arrange
            var client = factory.WithWebHostBuilder(builder =>
            {
                builder.UseSetting("ConnectionStrings:cache", redisConnString);
                builder.ConfigureServices(services =>
                {
                    services.RemoveAll<DbContextOptions<ContosoDbContext>>();
                    services.AddDbContextPool<ContosoDbContext>(options =>
                    {
                        options.UseSqlServer(connection);
                    });

                    services.RemoveAll<TimeProvider>();
                    // Replace TimeProvider with a mock
                    services.AddSingleton(mockTimeProvider.Object);

                    // Seed the database with test data
                    var serviceProvider = services.BuildServiceProvider();
                    using var scope = serviceProvider.CreateScope();
                    var scopedServices = scope.ServiceProvider;
                    var db = scopedServices.GetRequiredService<ContosoDbContext>();
                    db.Database.Migrate();
                    db.Transactions.AddRange(
                        new Transaction { Value = 50 },
                        new Transaction { Value = 50 }
                    );
                    db.SaveChanges();
                });
            }).CreateClient();

            client.DefaultRequestHeaders.Add("X-API-KEY", "contoso");

            // Set the TimeProvider to the current time
            var initialTime = DateTimeOffset.UtcNow;
            mockTimeProvider.Setup(tp => tp.GetUtcNow()).Returns(initialTime);

            // Act
            var response = await GetBalanceResponseAsync(client, initialTime);

            // Assert
            Assert.NotNull(response);
            Assert.Equal(100, response.Balance);
            Assert.Equal(initialTime, response.BalanceDate);
            Assert.False(response.IsFromCache);

            // Seed new data (should not affect cached response)
            {
                var db = new ContosoDbContext(new DbContextOptionsBuilder<ContosoDbContext>().UseSqlServer(connection).Options);
                db.Transactions.Add(new Transaction { Value = 100 });
                db.SaveChanges();
            }
            var response2 = await GetBalanceResponseAsync(client, initialTime);

            // Assert
            Assert.NotNull(response2);
            Assert.Equal(100, response2.Balance);  // Cached balance should still be 100
            Assert.Equal(response.BalanceDate, response2.BalanceDate);  // Dates should be the same since cache was used
            Assert.True(response2.IsFromCache);

            // Advance time beyond cache expiration
            mockTimeProvider.Setup(tp => tp.GetUtcNow()).Returns(initialTime.AddSeconds(DailyBalanceService.CacheExpirationInSeconds+1));

            var response3 = await GetBalanceResponseAsync(client, initialTime);

            // Assert
            Assert.NotNull(response3);
            Assert.False(response3.IsFromCache);
            Assert.Equal(200, response3.Balance);  // Cached balance should still be 100
            
        }

        private static async Task<GetBalanceResponse?> GetBalanceResponseAsync(HttpClient client, DateTimeOffset date)
        {
            string formattedDate = date.ToString("yyyy-MM-dd");
            var response = await client.GetFromJsonAsync<GetBalanceResponse>($"/consolidado/{formattedDate}");
            return response;
        }
    }
}