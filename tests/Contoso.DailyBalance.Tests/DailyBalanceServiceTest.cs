using Contoso.DailyBalance.Services;
using Contoso.Data;
using Contoso.Data.Entities;
using Docker.DotNet.Models;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Testcontainers.MsSql;
using Testcontainers.Redis;

namespace Contoso.DailyBalance.Tests
{
    public class DailyBalanceServiceTests : IAsyncLifetime
    {
        private readonly RedisContainer _redisContainer;
        private readonly Mock<ILogger<DailyBalanceService>> _mockLogger;
        private readonly Mock<TimeProvider> _mockTimeProvider;
        private ContosoDbContext? _dbContext;

        public DailyBalanceServiceTests()
        {
            _redisContainer = new RedisBuilder().Build();
            _mockLogger = new Mock<ILogger<DailyBalanceService>>();
            _mockTimeProvider = new Mock<TimeProvider>();


        }

        private readonly MsSqlContainer _msSqlContainer
            = new MsSqlBuilder().Build();

        public async Task InitializeAsync()
        {
            await _msSqlContainer.StartAsync();
            await _redisContainer.StartAsync();
            await using var connection = new SqlConnection(_msSqlContainer.GetConnectionString());
            await connection.OpenAsync();

            var options = new DbContextOptionsBuilder<ContosoDbContext>()
             .UseSqlServer(_msSqlContainer.GetConnectionString())
               .Options;

            _dbContext = new ContosoDbContext(options);
            await _dbContext.Database.MigrateAsync().ConfigureAwait(false);
            // Seed de dados no banco de dados
            _dbContext.Transactions.AddRange(
                new Transaction { Value = 50 },
                new Transaction { Value = 100 }
            );
            await _dbContext.SaveChangesAsync();
        }

        public Task DisposeAsync()
        {
            return Task.WhenAll(
                _msSqlContainer.DisposeAsync().AsTask(),
                _redisContainer.DisposeAsync().AsTask()
            );
        }

        [Fact]
        public async Task GetBalanceAsync_ReturnsCachedBalance_WhenCacheIsValid()
        {
            // Arrange
            var cacheOptions = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(DailyBalanceService.CacheExpirationInSeconds)
            };

            var cache = new RedisCache(new RedisCacheOptions
            {
                Configuration = _redisContainer.GetConnectionString()
            });

            var initialTime = DateTime.UtcNow;
            _mockTimeProvider.Setup(tp => tp.GetUtcNow()).Returns(initialTime);

            var service = new DailyBalanceService(cache, _dbContext, _mockLogger.Object, _mockTimeProvider.Object);

            // Act - Primeiro cálculo para preencher o cache
            var response1 = await service.GetBalanceAsync(initialTime,CancellationToken.None);

            // Assert - Verifica se o primeiro cálculo está correto
            Assert.NotNull(response1);
            Assert.Equal(150, response1.Balance);
            Assert.False(response1.IsFromCache);

            // Simula tempo dentro do limite de expiração do cache
            _mockTimeProvider.Setup(tp => tp.GetUtcNow()).Returns(initialTime.AddSeconds(DailyBalanceService.CacheExpirationInSeconds));

            // Act - Segunda chamada que deve retornar do cache
            var response2 = await service.GetBalanceAsync(initialTime, CancellationToken.None);

            // Assert - Verifica se o valor foi obtido do cache
            Assert.NotNull(response2);
            Assert.Equal(150, response2.Balance);
            Assert.True(response2.IsFromCache);

            // Simula tempo além do limite de expiração do cache
            _mockTimeProvider.Setup(tp => tp.GetUtcNow()).Returns(initialTime.AddSeconds(70));

            // Act - Terceira chamada que deve recalcular o saldo após expiração do cache
            var response3 = await service.GetBalanceAsync(initialTime, CancellationToken.None);

            // Assert - Verifica se o cache foi expirado e o saldo recalculado
            Assert.NotNull(response3);
            Assert.Equal(150, response3.Balance);
            Assert.False(response3.IsFromCache); // Deve indicar que o valor não veio do cache
        }
    }
}
