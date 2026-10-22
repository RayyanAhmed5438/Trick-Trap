using Godot;

public partial class Candy : Area2D, IRoomStateful
{
    [Export]
    public string PersistentId { get; set; } = "";


    private bool _collected = false;

    private CollisionShape2D _collisionShape = null!;


    public override void _Ready()
    {
        _collisionShape = GetNode<CollisionShape2D>(
            "CollisionShape2D"
        );

        BodyEntered += OnBodyEntered;

        ApplyCollectedState();
    }


    private void OnBodyEntered(Node2D body)
    {
        if (_collected || !body.IsInGroup("player"))
            return;

        GameState gameState = GetNode<GameState>(
            "/root/GameState"
        );

        gameState.CollectCandy();

        _collected = true;

        ApplyCollectedState();
    }


    private void ApplyCollectedState()
    {
        Visible = !_collected;

        SetDeferred("monitoring", !_collected);

        if (_collisionShape != null)
        {
            _collisionShape.SetDeferred(
                "disabled",
                _collected
            );
        }
    }


    public Godot.Collections.Dictionary<string, Variant>
        CapturePersistentState()
    {
        var state =
            new Godot.Collections.Dictionary<string, Variant>();

        state["collected"] = _collected;

        return state;
    }


    public void RestorePersistentState(
        Godot.Collections.Dictionary<string, Variant> state)
    {
        _collected =
            state.ContainsKey("collected") &&
            state["collected"].AsBool();

        ApplyCollectedState();
    }
}