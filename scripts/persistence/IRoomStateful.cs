using Godot;

public interface IRoomStateful
{
    // Unique within its room. RoomRoot fills this in automatically when empty or duplicated.
    string PersistentId { get; set; }

    // Capture the object's current state.
    Godot.Collections.Dictionary<string, Variant>
        CapturePersistentState();

    // Restore a previously captured state.
    void RestorePersistentState(
        Godot.Collections.Dictionary<string, Variant> state
    );
}