using Godot;
using System.Collections.Generic;

public partial class GameState : Node
{
    // ---------------- CANDY SYSTEM ----------------

    [Signal]
    public delegate void CandyCountChangedEventHandler(int total);

    public int CandiesCollected { get; private set; } = 0;


    public void CollectCandy()
    {
        CandiesCollected++;

        EmitSignal(
            SignalName.CandyCountChanged,
            CandiesCollected
        );

        GD.Print($"Candies collected: {CandiesCollected}");
    }


    // ---------------- INVENTORY SYSTEM ----------------
    [Signal]
    public delegate void InventorySelectionChangedEventHandler(int slotIndex);

    public int SelectedInventoryIndex { get; private set; } = -1;

    public const int InventoryCapacity = 5;

    [Signal]
    public delegate void InventoryChangedEventHandler();

    private readonly List<InventoryItemData> _inventoryItems = new();

    public IReadOnlyList<InventoryItemData> InventoryItems
        => _inventoryItems;


    public bool TryAddItem(InventoryItemData item)
    {
        if (item == null)
        {
            GD.PushWarning(
                "Inventory: Cannot add an empty item definition."
            );

            return false;
        }

        if (string.IsNullOrWhiteSpace(item.ItemId))
        {
            GD.PushWarning(
                "Inventory: The item must have an ItemId."
            );

            return false;
        }

        if (_inventoryItems.Count >= InventoryCapacity)
        {
            GD.Print(
                $"Inventory full! Could not collect {item.DisplayName}."
            );

            return false;
        }

        _inventoryItems.Add(item);

        GD.Print($"Added to inventory: {item.DisplayName}");

        EmitSignal(SignalName.InventoryChanged);

        return true;
    }


    public bool HasItem(string itemId)
    {
        foreach (InventoryItemData item in _inventoryItems)
        {
            if (item.ItemId == itemId)
            {
                return true;
            }
        }

        return false;
    }

    public void SelectInventorySlot(int slotIndex)
    {
        // Empty slots and invalid indices clear the selection.
        if (slotIndex < 0 || slotIndex >= _inventoryItems.Count)
        {
            slotIndex = -1;
        }

        if (SelectedInventoryIndex == slotIndex)
            return;

        SelectedInventoryIndex = slotIndex;

        EmitSignal(
            SignalName.InventorySelectionChanged,
            SelectedInventoryIndex
        );
    }

    public bool TryRemoveItemAt(
        int slotIndex,
        out InventoryItemData? removedItem)
    {
        removedItem = null;

        if (slotIndex < 0 || slotIndex >= _inventoryItems.Count)
            return false;

        int previousSelection = SelectedInventoryIndex;
        removedItem = _inventoryItems[slotIndex];
        _inventoryItems.RemoveAt(slotIndex);

        if (SelectedInventoryIndex == slotIndex)
        {
            SelectedInventoryIndex = -1;
        }
        else if (SelectedInventoryIndex > slotIndex)
        {
            SelectedInventoryIndex--;
        }

        GD.Print($"Removed from inventory: {removedItem.DisplayName}");
        EmitSignal(SignalName.InventoryChanged);

        if (previousSelection != SelectedInventoryIndex)
        {
            EmitSignal(
                SignalName.InventorySelectionChanged,
                SelectedInventoryIndex
            );
        }

        return true;
    }


    public bool RemoveItem(string itemId)
    {
        for (int i = 0; i < _inventoryItems.Count; i++)
        {
            if (_inventoryItems[i].ItemId != itemId)
            {
                continue;
            }

            InventoryItemData removedItem = _inventoryItems[i];

            int previousSelection = SelectedInventoryIndex;

            _inventoryItems.RemoveAt(i);

            if (SelectedInventoryIndex == i)
            {
                // The selected item was removed.
                SelectedInventoryIndex = -1;
            }
            else if (SelectedInventoryIndex > i)
            {
                // Shift the selection because the remaining items moved.
                SelectedInventoryIndex--;
            }

            GD.Print($"Removed from inventory: {removedItem.DisplayName}");

            EmitSignal(SignalName.InventoryChanged);

            if (previousSelection != SelectedInventoryIndex)
            {
                EmitSignal(
                    SignalName.InventorySelectionChanged,
                    SelectedInventoryIndex
                );
            }

            return true;
        }

        return false;
    }

    // Room ID
    //   -> Object ID
    //       -> Object state
    private readonly Dictionary<
        string,
        Dictionary<
            string,
            Godot.Collections.Dictionary<string, Variant>
        >
    > _savedRoomStates = new();


    public void SaveRoomObjectStates(
        string roomId,
        Dictionary<string, Godot.Collections.Dictionary<string, Variant>>
            objectStates)
    {
        if (string.IsNullOrWhiteSpace(roomId))
        {
            GD.PushWarning(
                "GameState: Cannot save room state without a RoomId."
            );

            return;
        }

        // The RoomRoot creates a fresh state dictionary each time
        // it captures the room, so we can retain this snapshot.
        _savedRoomStates[roomId] = objectStates;

        GD.Print(
            $"Saved room '{roomId}' with {objectStates.Count} stateful objects."
        );
    }


    public Dictionary<
        string,
        Godot.Collections.Dictionary<string, Variant>
    > GetRoomObjectStates(string roomId)
    {
        if (_savedRoomStates.TryGetValue(
            roomId,
            out var savedStates))
        {
            return savedStates;
        }

        // An unseen room has no saved changes.
        return new Dictionary<
            string,
            Godot.Collections.Dictionary<string, Variant>
        >();
    }
}