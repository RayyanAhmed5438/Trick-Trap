using Godot;

public partial class Drawer : StaticBody2D, IRoomStateful
{
    [Export]
    public string ContentsMessage { get; set; } = "The drawer is empty.";

    [Export]
    public string PersistentId { get; set; } = "";

    [Export(PropertyHint.Range, "1,10,1")]
    public int InventoryCapacity { get; set; } = 5;

    [Export]
    public Godot.Collections.Array<InventoryItemData> Contents { get; set; } = new();

    private Area2D _interactionArea = null!;
    private Label _interactionPrompt = null!;
    private bool _playerInRange;
    private bool _isOpen;

    public override void _Ready()
    {
        _interactionArea = GetNode<Area2D>("InteractionArea");
        _interactionPrompt = GetNode<Label>("InteractionPrompt");

        _interactionPrompt.Visible = false;
        InventoryCapacity = Mathf.Clamp(InventoryCapacity, 1, 10);

        if (Contents.Count > InventoryCapacity)
        {
            GD.PushWarning(
                $"{Name}: Contains {Contents.Count} items but its inventory " +
                $"capacity is {InventoryCapacity}. Only the first " +
                $"{InventoryCapacity} slots are displayed."
            );
        }

        _interactionArea.BodyEntered += OnBodyEntered;
        _interactionArea.BodyExited += OnBodyExited;
    }

    private void OnBodyEntered(Node2D body)
    {
        if (!body.IsInGroup("player"))
            return;

        _playerInRange = true;
        _interactionPrompt.Visible = true;
    }

    private void OnBodyExited(Node2D body)
    {
        if (!body.IsInGroup("player"))
            return;

        _playerInRange = false;
        _interactionPrompt.Visible = false;

        GetHud()?.HideDrawerInventory(this);
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (!_playerInRange || !@event.IsActionPressed("interact"))
            return;

        Hud? hud = GetHud();
        if (hud == null)
        {
            GD.PushWarning("Drawer: Could not find the HUD to display its inventory.");
            return;
        }

        bool inventoryVisible = hud.ToggleDrawerInventory(this);
        _isOpen = true;
        _interactionPrompt.Visible = !inventoryVisible;

        GetViewport().SetInputAsHandled();
    }

    private Hud? GetHud()
    {
        return GetNodeOrNull<Hud>("/root/Main/HUD");
    }

    public InventoryItemData? GetItemAt(int slotIndex)
    {
        if (slotIndex < 0 ||
            slotIndex >= InventoryCapacity ||
            slotIndex >= Contents.Count)
        {
            return null;
        }

        return Contents[slotIndex];
    }

    public bool TryTakeItemAt(int slotIndex)
    {
        InventoryItemData? item = GetItemAt(slotIndex);
        if (item == null)
            return false;

        GameState gameState = GetNode<GameState>("/root/GameState");

        // Do not remove the item from the drawer unless the player's
        // inventory successfully accepts it.
        if (!gameState.TryAddItem(item))
            return false;

        Contents.RemoveAt(slotIndex);
        return true;
    }


    public bool TryStorePlayerItemAt(int playerSlotIndex)
    {
        if (Contents.Count >= InventoryCapacity)
        {
            GD.Print($"{Name}: Drawer inventory is full.");
            return false;
        }

        GameState gameState = GetNode<GameState>("/root/GameState");

        // Remove the exact clicked slot, preserving correct behavior when
        // the player's inventory contains multiple copies of the same item.
        if (!gameState.TryRemoveItemAt(
                playerSlotIndex,
                out InventoryItemData? item) ||
            item == null)
        {
            return false;
        }

        Contents.Add(item);
        return true;
    }

    public Godot.Collections.Dictionary<string, Variant> CapturePersistentState()
    {
        var state = new Godot.Collections.Dictionary<string, Variant>();
        state["is_open"] = _isOpen;

        // Duplicate the array so later item transfers do not mutate the snapshot.
        state["contents"] = Contents.Duplicate();
        return state;
    }

    public void RestorePersistentState(
        Godot.Collections.Dictionary<string, Variant> state)
    {
        _isOpen = state.ContainsKey("is_open") && state["is_open"].AsBool();

        if (state.ContainsKey("contents"))
        {
            Contents.Clear();

            Godot.Collections.Array<InventoryItemData> savedContents =
                state["contents"].AsGodotArray<InventoryItemData>();

            foreach (InventoryItemData item in savedContents)
            {
                if (item != null)
                    Contents.Add(item);
            }
        }

        _playerInRange = false;
        _interactionPrompt.Visible = false;
    }

    public override void _ExitTree()
    {
        GetHud()?.HideDrawerInventory(this);
    }
}
