using Godot;

public partial class PersistentProp : StaticBody2D, IRoomStateful
{
    [Export]
    public string PersistentId { get; set; } = "";


    /*
     * APPLY THIS SCRIPT TO ANY MOVABLE PROP THAT YOU WANT TO PERSIST ITS POSITION, ROTATION, AND VISIBILITY STATE ACROSS ROOM LOADS.[Export] 
     */


    public Godot.Collections.Dictionary<string, Variant>
        CapturePersistentState()
    {
        var state =
            new Godot.Collections.Dictionary<string, Variant>();

        state["position"] = Position;
        state["rotation"] = Rotation;
        state["visible"] = Visible;

        return state;
    }


    public void RestorePersistentState(
        Godot.Collections.Dictionary<string, Variant> state)
    {
        if (state.ContainsKey("position"))
            Position = state["position"].AsVector2();

        if (state.ContainsKey("rotation"))
            Rotation = state["rotation"].AsSingle();

        if (state.ContainsKey("visible"))
            Visible = state["visible"].AsBool();
    }
}