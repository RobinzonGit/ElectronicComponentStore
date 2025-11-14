namespace ElectronicComponentStore.Models;

public class Component
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string CellNumber { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public DateTime DateOfChanges { get; set; }
    public string Datasheet { get; set; } = string.Empty;
}

public class ComponentType
{
    public string TypeName { get; set; } = string.Empty;
    public int Count { get; set; }
}