using System.Text.Json.Serialization;
using AssetTracker.Api.Data;
using AssetTracker.Api.Models;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite("Data Source=assets.db"));
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

var app = builder.Build();

// Create the database file the first time the app runs
using (var scope = app.Services.CreateScope())
{
    scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.EnsureCreated();
}

app.MapOpenApi();
app.UseSwaggerUI(options =>
    options.SwaggerEndpoint("/openapi/v1.json", "Asset Tracker API"));

var assets = app.MapGroup("/api/assets").WithTags("Assets");

// Get all assets
assets.MapGet("/", async (AppDbContext db) =>
    await db.Assets.ToListAsync());

// Get one asset by its id
assets.MapGet("/{id:int}", async (int id, AppDbContext db) =>
    await db.Assets.FindAsync(id) is Asset asset
        ? Results.Ok(asset)
        : Results.NotFound());

// Add a new asset
assets.MapPost("/", async (Asset asset, AppDbContext db) =>
{
    if (string.IsNullOrWhiteSpace(asset.Name))
        return Results.BadRequest("Name is required.");

    asset.Id = 0;
    db.Assets.Add(asset);
    await db.SaveChangesAsync();
    return Results.Created($"/api/assets/{asset.Id}", asset);
});

// Update an existing asset
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
});

// Delete an asset
assets.MapDelete("/{id:int}", async (int id, AppDbContext db) =>
{
    var asset = await db.Assets.FindAsync(id);
    if (asset is null) return Results.NotFound();

    db.Assets.Remove(asset);
    await db.SaveChangesAsync();
    return Results.NoContent();
});

app.Run();