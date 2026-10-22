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
        PositionPromptWithinViewport();

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

    public override void _Process(double delta)
    {
        // Reposition the prompt as the camera moves, keeping it inside the viewport.
        if (_playerInRange && _interactionPrompt.Visible)
            PositionPromptWithinViewport();
    }


    private void PositionPromptWithinViewport()
    {
        Transform2D canvasTransform = GetViewport().GetCanvasTransform();
        Vector2 viewportSize = GetViewportRect().Size;
        Vector2 screenPosition = canvasTransform * GlobalPosition;

        float screenScaleX = Mathf.Max(0.001f, canvasTransform.X.Length());
        float screenScaleY = Mathf.Max(0.001f, canvasTransform.Y.Length());

        // The prompt's horizontal offsets extend 62 world units from the drawer.
        bool spaceRight = screenPosition.X + 62.0f * screenScaleX
            <= viewportSize.X - 4.0f;
        bool spaceLeft = screenPosition.X - 62.0f * screenScaleX >= 4.0f;

        bool placeRight;
        if (spaceRight && spaceLeft)
            placeRight = screenPosition.X <= viewportSize.X * 0.5f;
        else
            placeRight = spaceRight || !spaceLeft;

        if (placeRight)
        {
            _interactionPrompt.OffsetLeft = 14.0f;
            _interactionPrompt.OffsetRight = 62.0f;
        }
        else
        {
            _interactionPrompt.OffsetLeft = -62.0f;
            _interactionPrompt.OffsetRight = -14.0f;
        }

        // If the drawer is near the top/bottom edge, put the prompt inward
        // rather than above/below the visible viewport.
        float edgeDistanceY = 20.0f * screenScaleY;
        if (screenPosition.Y < edgeDistanceY)
        {
            _interactionPrompt.OffsetTop = 4.0f;
            _interactionPrompt.OffsetBottom = 18.0f;
        }
        else if (screenPosition.Y > viewportSize.Y - edgeDistanceY)
        {
            _interactionPrompt.OffsetTop = -18.0f;
            _interactionPrompt.OffsetBottom = -4.0f;
        }
        else
        {
            _interactionPrompt.OffsetTop = -7.0f;
            _interactionPrompt.OffsetBottom = 7.0f;
        }
    }


    private void OnBodyEntered(Node2D body)
    {
        if (!body.IsInGroup("player"))
            return;

        _playerInRange = true;
        PositionPromptWithinViewport();
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
