using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json.Serialization;
using AssetTracker.Api.Data;
using AssetTracker.Api.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// The key used to sign login tokens (kept in user-secrets, not in the code)
var jwtKey = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException("Jwt:Key is not configured.");
var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));

builder.Services.AddOpenApi();
builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseSqlite(builder.Configuration.GetConnectionString("Default") ?? "Data Source=assets.db"));
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            IssuerSigningKey = signingKey
        };
    });
builder.Services.AddAuthorization();

var app = builder.Build();

// Create the database and the two demo users the first time the app runs
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();

    if (!db.Users.Any())
    {
        var hasher = new PasswordHasher<User>();
        var seedUsers = new[]
        {
            (Username: "admin", Role: "Admin", ConfigKey: "Seed:AdminPassword"),
            (Username: "viewer", Role: "Viewer", ConfigKey: "Seed:ViewerPassword")
        };

        foreach (var seed in seedUsers)
        {
            var password = app.Configuration[seed.ConfigKey];
            if (string.IsNullOrEmpty(password)) continue;

            var user = new User { Username = seed.Username, Role = seed.Role };
            user.PasswordHash = hasher.HashPassword(user, password);
            db.Users.Add(user);
        }

        db.SaveChanges();
    }
}

app.MapOpenApi();
app.UseSwaggerUI(options =>
    options.SwaggerEndpoint("/openapi/v1.json", "Asset Tracker API"));

app.UseAuthentication();
app.UseAuthorization();

// Log in and receive a token
app.MapPost("/api/auth/login", async (LoginRequest request, AppDbContext db) =>
{
    var user = await db.Users.SingleOrDefaultAsync(u => u.Username == request.Username);
    var hasher = new PasswordHasher<User>();

    if (user is null ||
        hasher.VerifyHashedPassword(user, user.PasswordHash, request.Password)
            == PasswordVerificationResult.Failed)
    {
        return Results.Unauthorized();
    }

    var token = new JwtSecurityToken(
        claims:
        [
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.Role, user.Role)
        ],
        expires: DateTime.UtcNow.AddHours(1),
        signingCredentials: new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256));

    return Results.Ok(new { token = new JwtSecurityTokenHandler().WriteToken(token) });
}).WithTags("Auth");

// Everything under /api/assets needs a valid login token
var assets = app.MapGroup("/api/assets").WithTags("Assets").RequireAuthorization();

// Get all assets (any logged-in user)
assets.MapGet("/", async (AppDbContext db) =>
    await db.Assets.ToListAsync());

// Get one asset by its id (any logged-in user)
assets.MapGet("/{id:int}", async (int id, AppDbContext db) =>
    await db.Assets.FindAsync(id) is Asset asset
        ? Results.Ok(asset)
        : Results.NotFound());

// Add a new asset (admin only)
assets.MapPost("/", async (Asset asset, AppDbContext db) =>
{
    if (string.IsNullOrWhiteSpace(asset.Name))
        return Results.BadRequest("Name is required.");

    asset.Id = 0;
    db.Assets.Add(asset);
    await db.SaveChangesAsync();
    return Results.Created($"/api/assets/{asset.Id}", asset);
}).RequireAuthorization(policy => policy.RequireRole("Admin"));

// Update an existing asset (admin only)
assets.MapPut("/{id:int}", async (int id, Asset updated, AppDbContext db) =>
{
    var asset = await db.Assets.FindAsync(id);
    if (asset is null) return Results.NotFound();

    asset.Name = updated.Name;
    asset.Type = updated.Type;
    asset.SerialNumber = updated.SerialNumber;
    asset.Status = updated.Status;
    asset.AssignedTo = updated.AssignedTo;
    asset.PurchaseDate = updated.PurchaseDate;
    await db.SaveChangesAsync();
    return Results.Ok(asset);
}).RequireAuthorization(policy => policy.RequireRole("Admin"));

// Delete an asset (admin only)
assets.MapDelete("/{id:int}", async (int id, AppDbContext db) =>
{
    var asset = await db.Assets.FindAsync(id);
    if (asset is null) return Results.NotFound();

    db.Assets.Remove(asset);
    await db.SaveChangesAsync();
    return Results.NoContent();
}).RequireAuthorization(policy => policy.RequireRole("Admin"));

app.Run();

record LoginRequest(string Username, string Password);
public partial class Program { }