using Godot;

[GlobalClass]
public partial class InventoryItemData : Resource
{
    [Export]
    public string ItemId { get; set; } = "";

    [Export]
    public string DisplayName { get; set; } = "Item";

    [Export]
    public Texture2D Icon { get; set; } = null!;

    [Export(PropertyHint.MultilineText)]
    public string Description { get; set; } = "";
}