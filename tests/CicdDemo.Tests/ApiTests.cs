using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace CicdDemo.Tests;

public class ApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public ApiTests(WebApplicationFactory<Program> factory)
        => _client = factory.CreateClient();

    [Fact]
    public async Task Health_returns_ok()
    {
        var res = await _client.GetAsync("/health");
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
    }

    [Fact]
    public async Task Root_returns_the_running_version()
    {
        var json = await _client.GetFromJsonAsync<JsonElement>("/");
        Assert.True(json.TryGetProperty("version", out _));
    }

    [Fact]
    public async Task Products_returns_a_list()
    {
        var products = await _client.GetFromJsonAsync<JsonElement[]>("/api/products");
        Assert.NotEmpty(products!);
    }

    [Fact]
    public async Task Unknown_product_returns_404()
    {
        var res = await _client.GetAsync("/api/products/999");
        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
    }
}
