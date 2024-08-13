using Newtonsoft.Json;
using System.Collections.Concurrent;
using System.Net.Http.Json;
using System.Text;

namespace ContosoTest
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            decimal saldo = await GetSaldo();
            decimal saldoInicial = saldo;
            object saldoLock = new object();

            var httpClient = new HttpClient() { BaseAddress = new Uri("https://localhost:9001") };
            httpClient.DefaultRequestHeaders.Add("X-API-KEY", "contoso");

            int totalRequests = 10000;
            int successfulRequests = 0;
            int failedRequests = 0;

            var tasks = new ConcurrentBag<Task>();

            var startTime = DateTime.UtcNow;
            var results = new ConcurrentQueue<decimal>();
            Parallel.ForEach(
                Partitioner.Create(0, totalRequests),
                new ParallelOptions { MaxDegreeOfParallelism = totalRequests/50 },
                (range, state) =>
                {
                    for (int i = range.Item1; i < range.Item2; i++)
                    {
                        tasks.Add(Task.Run(async () =>
                        {
                            var val = new Random().Next(-10000, 10000);

                            lock (saldoLock)
                            {
                                saldo += val;
                            }
                            results.Enqueue(val);

                            var response = await httpClient.PostAsync("/", new StringContent(JsonConvert.SerializeObject(new Payload { Amount = val }), Encoding.UTF8, "application/json"));

                            if (response.IsSuccessStatusCode)
                            {
                                Interlocked.Increment(ref successfulRequests);
                                Console.WriteLine($"Success: {val}");
                            }
                            else
                            {
                                Interlocked.Increment(ref failedRequests);
                                Console.WriteLine($"Failed: {val}");
                            }
                        }));
                    }
                });

            await Task.WhenAll(tasks);

            var endTime = DateTime.UtcNow;
            var totalTimeInSeconds = (endTime - startTime).TotalSeconds;
            var requestsPerSecond = totalRequests / totalTimeInSeconds;

            Console.WriteLine($"Total Requests: {totalRequests}");
            Console.WriteLine($"Successful Requests: {successfulRequests}");
            Console.WriteLine($"Failed Requests: {failedRequests}");
            Console.WriteLine($"Requests per Second: {requestsPerSecond:F2}");

            Console.WriteLine($"Saldo experado: {saldo}");
            Console.WriteLine($"Saldo experado dictionary: {saldoInicial + results.Sum()}");

            var resultsCount = results.Count;
            if (resultsCount != totalRequests)
            {
                Console.WriteLine($"Erro : Results count: {resultsCount} != {totalRequests}");
            }
            await Task.Delay(7 * 1000);

            var saldoAtual = await GetSaldo();

            Console.WriteLine($"Saldo atual: {saldoAtual}");

            if (saldo == saldoAtual)
            {
                Console.WriteLine("Balances match");
            }
            else
            {
                Console.WriteLine("Balances do not match");
            }
        }

        private static async Task<decimal> GetSaldo()
        {
            var httpClient2 = new HttpClient() { BaseAddress = new Uri("https://localhost:9000") };
            httpClient2.DefaultRequestHeaders.Add("X-API-KEY", "contoso");
            var saldoResponse = await httpClient2.GetFromJsonAsync<BalanceR>("/");

            Console.WriteLine($"Saldo atual: {saldoResponse.Balance}, saldo real : {saldoResponse.Soma}, data do saldo : {saldoResponse.BalanceDate}");
            return saldoResponse.Balance;

        }
    }

    public class BalanceR
    {
        public decimal Balance { get; set; }
        public DateTimeOffset BalanceDate { get; set; }
        public decimal Soma { get; set; }
    }
    public class Payload
    {
        public decimal Amount { get; set; }
    }
}