using Contoso.Web;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Contoso.Tests;

public class WebTests
{
    [Fact]
    public async Task GetWebResourceRootReturnsOkStatusCode()
    {
        await using var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseSetting("ConnectionStrings:cache", "localhost:6379");
            });

        var httpClient = factory.CreateClient();
        var response = await httpClient.GetAsync("/");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
