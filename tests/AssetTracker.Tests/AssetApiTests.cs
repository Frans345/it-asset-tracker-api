using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace AssetTracker.Tests;

public record LoginResponse(string Token);

public record AssetResponse(
    int Id, string Name, string Type, string SerialNumber, string Status, string? AssignedTo);

public class AssetApiTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private static object NewAsset(string name) => new
    {
        name,
        type = "Laptop",
        serialNumber = "SN-" + Guid.NewGuid().ToString("N")[..8],
        status = "InStock"
    };

    private async Task<HttpClient> LoginAsync(string username, string password)
    {
        var client = factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/auth/login", new { username, password });
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadFromJsonAsync<LoginResponse>();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", body!.Token);
        return client;
    }

    private Task<HttpClient> AdminAsync() => LoginAsync("admin", ApiFactory.AdminPassword);

    private Task<HttpClient> ViewerAsync() => LoginAsync("viewer", ApiFactory.ViewerPassword);

    [Fact]
    public async Task Login_WithWrongPassword_Returns401()
    {
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/login",
            new { username = "admin", password = "not-the-password" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_WithCorrectPassword_ReturnsToken()
    {
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/login",
            new { username = "admin", password = ApiFactory.AdminPassword });
        var body = await response.Content.ReadFromJsonAsync<LoginResponse>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.False(string.IsNullOrEmpty(body!.Token));
    }

    [Fact]
    public async Task GetAssets_WithoutToken_Returns401()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/assets");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetAssets_AsViewer_Returns200()
    {
        var client = await ViewerAsync();

        var response = await client.GetAsync("/api/assets");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task CreateAsset_AsViewer_Returns403()
    {
        var client = await ViewerAsync();

        var response = await client.PostAsJsonAsync("/api/assets", NewAsset("Viewer laptop"));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CreateAsset_AsAdmin_Returns201AndCanBeFetched()
    {
        var client = await AdminAsync();

        var createResponse = await client.PostAsJsonAsync("/api/assets", NewAsset("Dell Latitude 5440"));
        var created = await createResponse.Content.ReadFromJsonAsync<AssetResponse>();
        var fetched = await client.GetFromJsonAsync<AssetResponse>($"/api/assets/{created!.Id}");

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        Assert.Equal("Dell Latitude 5440", fetched!.Name);
        Assert.Equal("Laptop", fetched.Type);
    }

    [Fact]
    public async Task CreateAsset_WithoutName_Returns400()
    {
        var client = await AdminAsync();

        var response = await client.PostAsJsonAsync("/api/assets", NewAsset(""));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UpdateAsset_AsAdmin_ChangesStatus()
    {
        var client = await AdminAsync();
        var createResponse = await client.PostAsJsonAsync("/api/assets", NewAsset("HP ProLiant server"));
        var created = await createResponse.Content.ReadFromJsonAsync<AssetResponse>();

        var updateResponse = await client.PutAsJsonAsync($"/api/assets/{created!.Id}", new
        {
            name = created.Name,
            type = "Server",
            serialNumber = created.SerialNumber,
            status = "InRepair"
        });
        var updated = await updateResponse.Content.ReadFromJsonAsync<AssetResponse>();

        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
        Assert.Equal("InRepair", updated!.Status);
        Assert.Equal("Server", updated.Type);
    }

    [Fact]
    public async Task DeleteAsset_AsAdmin_RemovesIt()
    {
        var client = await AdminAsync();
        var createResponse = await client.PostAsJsonAsync("/api/assets", NewAsset("Old switch"));
        var created = await createResponse.Content.ReadFromJsonAsync<AssetResponse>();

        var deleteResponse = await client.DeleteAsync($"/api/assets/{created!.Id}");
        var getResponse = await client.GetAsync($"/api/assets/{created.Id}");

        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    [Fact]
    public async Task GetAsset_WithUnknownId_Returns404()
    {
        var client = await AdminAsync();

        var response = await client.GetAsync("/api/assets/999999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}