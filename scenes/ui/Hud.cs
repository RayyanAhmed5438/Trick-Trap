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

    private PanelContainer _drawerInventoryPanel = null!;
    private HBoxContainer _drawerInventorySlots = null!;
    private readonly List<PanelContainer> _drawerSlots = new();
    private readonly List<TextureRect> _drawerIcons = new();
    private Drawer? _activeDrawer;
    private int _drawerSlotCapacity = -1;

    private bool _isConnected;

    public override void _Ready()
    {
        _candyCounter = GetNode<Label>("CandyCounter");
        _gameState = GetNode<GameState>("/root/GameState");

        _gameState.CandyCountChanged += OnCandyCountChanged;
        _gameState.InventoryChanged += RefreshInventory;
        _gameState.InventorySelectionChanged += OnInventorySelectionChanged;
        _isConnected = true;

        OnCandyCountChanged(_gameState.CandiesCollected);

        for (int i = 1; i <= GameState.InventoryCapacity; i++)
        {
            string slotPath = $"InventoryBar/Slots/Slot{i}";
            PanelContainer slot = GetNode<PanelContainer>(slotPath);

            StyleBox normalStyle = slot.GetThemeStylebox("panel");
            _normalSlotStyles.Add(normalStyle);

            if (normalStyle is StyleBoxFlat flatStyle)
            {
                StyleBoxFlat selectedStyle = (StyleBoxFlat)flatStyle.Duplicate();
                selectedStyle.SetBorderWidthAll(2);
                selectedStyle.BorderColor = Colors.White;
                _selectedSlotStyles.Add(selectedStyle);
            }
            else
            {
                _selectedSlotStyles.Add(null);
                GD.PushWarning($"{slotPath}: The panel style is not a StyleBoxFlat.");
            }

            Control center = GetNode<Control>($"{slotPath}/CenterContainer");
            TextureRect icon = GetNode<TextureRect>($"{slotPath}/CenterContainer/Icon");

            center.MouseFilter = Control.MouseFilterEnum.Ignore;
            icon.MouseFilter = Control.MouseFilterEnum.Ignore;

            _inventorySlots.Add(slot);
            _inventoryIcons.Add(icon);

            int slotIndex = i - 1;
            slot.MouseFilter = Control.MouseFilterEnum.Stop;
            slot.GuiInput += (InputEvent @event) =>
            {
                if (@event is InputEventMouseButton mouseEvent &&
                    mouseEvent.Pressed &&
                    mouseEvent.ButtonIndex == MouseButton.Left)
                {
                    if (_activeDrawer != null && _drawerInventoryPanel.Visible)
                    {
                        if (_activeDrawer.TryStorePlayerItemAt(slotIndex))
                            RefreshDrawerInventory();
                    }
                    else
                    {
                        _gameState.SelectInventorySlot(slotIndex);
                    }

                    slot.AcceptEvent();
                }
            };
        }

        CreateDrawerInventoryUi();
        RefreshInventory();
    }

    private void CreateDrawerInventoryUi()
    {
        _drawerInventoryPanel = new PanelContainer
        {
            Name = "DrawerInventoryBar",
            Visible = false,
            MouseFilter = Control.MouseFilterEnum.Stop
        };

        // A solid black box with a brown pixel-style frame.
        var frameStyle = new StyleBoxFlat
        {
            BgColor = Colors.Black,
            BorderColor = new Color(0.42f, 0.27f, 0.15f)
        };
        frameStyle.SetBorderWidthAll(2);
        frameStyle.SetContentMarginAll(5);
        _drawerInventoryPanel.AddThemeStyleboxOverride("panel", frameStyle);

        _drawerInventoryPanel.AnchorLeft = 0.5f;
        _drawerInventoryPanel.AnchorRight = 0.5f;
        _drawerInventoryPanel.AnchorTop = 1.0f;
        _drawerInventoryPanel.AnchorBottom = 1.0f;
        // The framed drawer inventory sits just above the player's existing bar.
        _drawerInventoryPanel.OffsetTop = -154.0f;
        _drawerInventoryPanel.OffsetBottom = -82.0f;

        _drawerInventorySlots = new HBoxContainer
        {
            Name = "Slots",
            MouseFilter = Control.MouseFilterEnum.Ignore
        };
        _drawerInventorySlots.AddThemeConstantOverride("separation", 0);
        _drawerInventoryPanel.AddChild(_drawerInventorySlots);
        AddChild(_drawerInventoryPanel);

        BuildDrawerInventorySlots(GameState.InventoryCapacity);
    }

    private void BuildDrawerInventorySlots(int requestedCapacity)
    {
        int capacity = Mathf.Clamp(requestedCapacity, 1, 10);
        if (_drawerSlotCapacity == capacity)
            return;

        foreach (Node child in _drawerInventorySlots.GetChildren())
        {
            _drawerInventorySlots.RemoveChild(child);
            child.Free();
        }

        _drawerSlots.Clear();
        _drawerIcons.Clear();
        _drawerSlotCapacity = capacity;

        for (int i = 0; i < capacity; i++)
        {
            int slotIndex = i;
            var slot = new PanelContainer
            {
                Name = $"DrawerSlot{i + 1}",
                CustomMinimumSize = new Vector2(56, 56),
                MouseFilter = Control.MouseFilterEnum.Stop
            };

            // Keep the slot interior black and free from extra outlines.
            var slotStyle = new StyleBoxFlat
            {
                BgColor = Colors.Black
            };
            slot.AddThemeStyleboxOverride("panel", slotStyle);

            var center = new CenterContainer
            {
                MouseFilter = Control.MouseFilterEnum.Ignore
            };

            var icon = new TextureRect
            {
                Name = "Icon",
                CustomMinimumSize = new Vector2(40, 40),
                Visible = false,
                MouseFilter = Control.MouseFilterEnum.Ignore,
                StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
                TextureFilter = CanvasItem.TextureFilterEnum.Nearest
            };

            center.AddChild(icon);
            slot.AddChild(center);
            _drawerInventorySlots.AddChild(slot);

            slot.GuiInput += (InputEvent @event) =>
                OnDrawerSlotGuiInput(slotIndex, @event);

            _drawerSlots.Add(slot);
            _drawerIcons.Add(icon);

            if (i < capacity - 1)
            {
                // Thin grey dividers distinguish adjacent inventory slots.
                var separator = new ColorRect
                {
                    Name = $"Separator{i + 1}",
                    Color = new Color(0.42f, 0.42f, 0.42f),
                    CustomMinimumSize = new Vector2(1, 56),
                    MouseFilter = Control.MouseFilterEnum.Ignore
                };
                _drawerInventorySlots.AddChild(separator);
            }
        }

        float contentWidth = capacity * 56.0f + (capacity - 1) * 1.0f;
        float totalWidth = contentWidth + 20.0f;
        _drawerInventoryPanel.OffsetLeft = -totalWidth / 2.0f;
        _drawerInventoryPanel.OffsetRight = totalWidth / 2.0f;
    }

    public bool ToggleDrawerInventory(Drawer drawer)
    {
        if (_activeDrawer == drawer && _drawerInventoryPanel.Visible)
        {
            HideDrawerInventory(drawer);
            return false;
        }

        _activeDrawer = drawer;
        BuildDrawerInventorySlots(drawer.InventoryCapacity);
        RefreshDrawerInventory();
        _drawerInventoryPanel.Visible = true;
        return true;
    }

    public void HideDrawerInventory(Drawer drawer)
    {
        if (_activeDrawer != drawer)
            return;

        _drawerInventoryPanel.Visible = false;
        _activeDrawer = null;
    }

    private void OnDrawerSlotGuiInput(int slotIndex, InputEvent @event)
    {
        if (@event is not InputEventMouseButton mouseEvent ||
            !mouseEvent.Pressed ||
            mouseEvent.ButtonIndex != MouseButton.Left)
        {
            return;
        }

        if (_activeDrawer != null && _activeDrawer.TryTakeItemAt(slotIndex))
            RefreshDrawerInventory();

        _drawerSlots[slotIndex].AcceptEvent();
    }

    private void RefreshDrawerInventory()
    {
        for (int i = 0; i < _drawerIcons.Count; i++)
        {
            TextureRect icon = _drawerIcons[i];
            PanelContainer slot = _drawerSlots[i];
            InventoryItemData? item = _activeDrawer?.GetItemAt(i);

            if (item != null)
            {
                icon.Texture = item.Icon;
                icon.Visible = item.Icon != null;
                slot.TooltipText = string.IsNullOrWhiteSpace(item.Description)
                    ? item.DisplayName
                    : $"{item.DisplayName}\n{item.Description}";
            }
            else
            {
                icon.Texture = null;
                icon.Visible = false;
                slot.TooltipText = $"Empty drawer slot {i + 1}";
            }
        }
    }

    private void OnInventorySelectionChanged(int selectedSlot)
    {
        for (int i = 0; i < _inventorySlots.Count; i++)
        {
            PanelContainer slot = _inventorySlots[i];

            if (i == selectedSlot && _selectedSlotStyles[i] != null)
            {
                slot.AddThemeStyleboxOverride("panel", _selectedSlotStyles[i]!);
            }
            else
            {
                slot.AddThemeStyleboxOverride("panel", _normalSlotStyles[i]);
            }
        }
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is not InputEventKey keyEvent ||
            !keyEvent.Pressed ||
            keyEvent.Echo)
        {
            return;
        }

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
                InventoryItemData item = _gameState.InventoryItems[i];
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
        RefreshDrawerInventory();
    }

    public override void _ExitTree()
    {
        if (!_isConnected || !GodotObject.IsInstanceValid(_gameState))
            return;

        _gameState.CandyCountChanged -= OnCandyCountChanged;
        _gameState.InventoryChanged -= RefreshInventory;
        _gameState.InventorySelectionChanged -= OnInventorySelectionChanged;
    }
}
