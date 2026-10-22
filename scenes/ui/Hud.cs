using Godot;
using System.Collections.Generic;

public partial class Hud : CanvasLayer
{
    private Label _candyCounter = null!;

    private GameState _gameState = null!;

    private readonly List<PanelContainer> _inventorySlots = new();

    private readonly List<TextureRect> _inventoryIcons = new();

    private readonly List<StyleBox> _normalSlotStyles = new();

    private readonly List<StyleBoxFlat?> _selectedSlotStyles = new();

    private bool _isConnected = false;


    public override void _Ready()
    {
        _candyCounter = GetNode<Label>("CandyCounter");

        _gameState = GetNode<GameState>("/root/GameState");

        // Connect to the existing candy counter.
        _gameState.CandyCountChanged += OnCandyCountChanged;

        // Connect to inventory changes.
        _gameState.InventoryChanged += RefreshInventory;

        _gameState.InventorySelectionChanged += OnInventorySelectionChanged;

        _isConnected = true;

        // Display the current candy count.
        OnCandyCountChanged(_gameState.CandiesCollected);

        // Find the five inventory slots.
        for (int i = 1; i <= GameState.InventoryCapacity; i++)
        {
            string slotPath = $"InventoryBar/Slots/Slot{i}";

            PanelContainer slot = GetNode<PanelContainer>(
                slotPath
            );

            // Preserve the slot's original appearance.
            StyleBox normalStyle = slot.GetThemeStylebox("panel");

            _normalSlotStyles.Add(normalStyle);

            // Create a separate style for the selected slot.
            if (normalStyle is StyleBoxFlat flatStyle)
            {
                StyleBoxFlat selectedStyle =
                    (StyleBoxFlat)flatStyle.Duplicate();

                selectedStyle.SetBorderWidthAll(2);
                selectedStyle.BorderColor = Colors.White;

                _selectedSlotStyles.Add(selectedStyle);
            }
            else
            {
                _selectedSlotStyles.Add(null);

                GD.PushWarning(
                    $"{slotPath}: The panel style is not a StyleBoxFlat."
                );
            }

            Control center = GetNode<Control>(
                $"{slotPath}/CenterContainer"
            );

            TextureRect icon = GetNode<TextureRect>(
                $"{slotPath}/CenterContainer/Icon"
            );

            // Allow mouse input to reach the slot underneath.
            center.MouseFilter = Control.MouseFilterEnum.Ignore;
            icon.MouseFilter = Control.MouseFilterEnum.Ignore;

            _inventorySlots.Add(slot);
            _inventoryIcons.Add(icon);

            int slotIndex = i - 1;

            slot.MouseFilter = Control.MouseFilterEnum.Stop;

            slot.GuiInput += (InputEvent @event) =>
            {
                if (@event is InputEventMouseButton mouseEvent
                    && mouseEvent.Pressed
                    && mouseEvent.ButtonIndex == MouseButton.Left)
                {
                    _gameState.SelectInventorySlot(slotIndex);
                    slot.AcceptEvent();
                }
            };
        }

        RefreshInventory();
    }

    private void OnInventorySelectionChanged(int selectedSlot)
    {
        for (int i = 0; i < _inventorySlots.Count; i++)
        {
            PanelContainer slot = _inventorySlots[i];

            if (i == selectedSlot &&
                _selectedSlotStyles[i] != null)
            {
                // Selected: original background with a white outline.
                slot.AddThemeStyleboxOverride(
                    "panel",
                    _selectedSlotStyles[i]!
                );
            }
            else
            {
                // Unselected: restore the original appearance.
                slot.AddThemeStyleboxOverride(
                    "panel",
                    _normalSlotStyles[i]
                );
            }
        }
    }
    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is not InputEventKey keyEvent)
            return;

        if (!keyEvent.Pressed || keyEvent.Echo)
            return;

        int slotIndex = keyEvent.Keycode switch
        {
            Key.Key1 => 0,
            Key.Key2 => 1,
            Key.Key3 => 2,
            Key.Key4 => 3,
            Key.Key5 => 4,
            _ => -1
        };

        if (slotIndex < 0)
            return;

        _gameState.SelectInventorySlot(slotIndex);

        GetViewport().SetInputAsHandled();
    }

    private void OnCandyCountChanged(int total)
    {
        _candyCounter.Text = $"Candies: {total}";
    }


    private void RefreshInventory()
    {
        for (int i = 0; i < GameState.InventoryCapacity; i++)
        {
            TextureRect icon = _inventoryIcons[i];

            PanelContainer slot = _inventorySlots[i];

            if (i < _gameState.InventoryItems.Count)
            {
                InventoryItemData item =
                    _gameState.InventoryItems[i];

                icon.Texture = item.Icon;

                icon.Visible = item.Icon != null;

                slot.TooltipText = string.IsNullOrEmpty(item.Description)
                    ? item.DisplayName
                    : $"{item.DisplayName}\n{item.Description}";
            }
            else
            {
                icon.Texture = null;

                icon.Visible = false;

                slot.TooltipText = $"Empty slot {i + 1}";
            }
        }
        OnInventorySelectionChanged(_gameState.SelectedInventoryIndex);
    }


    public override void _ExitTree()
    {
        if (!_isConnected ||
            !GodotObject.IsInstanceValid(_gameState))
        {
            return;
        }

        _gameState.CandyCountChanged -= OnCandyCountChanged;

        _gameState.InventoryChanged -= RefreshInventory;

        _gameState.InventorySelectionChanged -= OnInventorySelectionChanged;
    }
}