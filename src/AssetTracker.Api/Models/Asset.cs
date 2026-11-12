namespace AssetTracker.Api.Models;

public enum AssetType { Laptop, Desktop, Server, Switch, Other }

public enum AssetStatus { InStock, Assigned, InRepair, Retired }

public class Asset
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public AssetType Type { get; set; }
    public string SerialNumber { get; set; } = "";
    public AssetStatus Status { get; set; }
    public string? AssignedTo { get; set; }
    public DateOnly? PurchaseDate { get; set; }
}