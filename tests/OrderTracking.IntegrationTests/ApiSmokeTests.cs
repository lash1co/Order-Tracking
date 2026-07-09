using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;

namespace OrderTracking.IntegrationTests;

public sealed class ApiSmokeTests(OrderTrackingApiFactory factory) : IClassFixture<OrderTrackingApiFactory>
{
    [Fact]
    public async Task LiveHealthEndpointReturnsOk()
    {
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/health/live", CancellationToken.None);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task BusinessEndpointsRequireAuthentication()
    {
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/v1/orders/active", CancellationToken.None);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task SignalRHubRejectsAnonymousHttpRequests()
    {
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var response = await client.GetAsync("/hubs/tracking", CancellationToken.None);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
