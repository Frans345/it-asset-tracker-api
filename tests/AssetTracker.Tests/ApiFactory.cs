using Microsoft.AspNetCore.Mvc.Testing;

namespace AssetTracker.Tests;

// Starts the API inside the tests, with its own settings and its own temporary database
public class ApiFactory : WebApplicationFactory<Program>
{
    public const string AdminPassword = "Test-admin-pass-1";
    public const string ViewerPassword = "Test-viewer-pass-1";

    private readonly string _dbPath =
        Path.Combine(Path.GetTempPath(), $"assettracker-tests-{Guid.NewGuid()}.db");

    public ApiFactory()
    {
        Environment.SetEnvironmentVariable("Jwt__Key", "test-signing-key-that-is-at-least-32-characters-long");
        Environment.SetEnvironmentVariable("Seed__AdminPassword", AdminPassword);
        Environment.SetEnvironmentVariable("Seed__ViewerPassword", ViewerPassword);
        Environment.SetEnvironmentVariable("ConnectionStrings__Default", $"Data Source={_dbPath}");
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        try
        {
            if (File.Exists(_dbPath)) File.Delete(_dbPath);
        }
        catch (IOException)
        {
            // The temporary file is still in use; Windows cleans it up later
        }
    }
}