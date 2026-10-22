using Godot;

public partial class Drawer : StaticBody2D, IRoomStateful
{
    [Export]
    public string ContentsMessage { get; set; } = "The drawer is empty.";

    [Export]
    public string PersistentId { get; set; } = "";
    private Area2D _interactionArea = null!;
    private Label _interactionPrompt = null!;

    private bool _playerInRange;
    private bool _isOpen;


    public override void _Ready()
    {
        _interactionArea = GetNode<Area2D>(
            "InteractionArea"
        );

        _interactionPrompt = GetNode<Label>(
            "InteractionPrompt"
        );

        _interactionPrompt.Visible = false;

        _interactionArea.BodyEntered += OnBodyEntered;
        _interactionArea.BodyExited += OnBodyExited;
    }


    private void OnBodyEntered(Node2D body)
    {
        if (!body.IsInGroup("player"))
            return;

        _playerInRange = true;
        _interactionPrompt.Visible = !_isOpen;
    }


    private void OnBodyExited(Node2D body)
    {
        if (!body.IsInGroup("player"))
            return;

        _playerInRange = false;
        _interactionPrompt.Visible = false;
    }


    public override void _UnhandledInput(InputEvent @event)
    {
        if (!_playerInRange || _isOpen)
            return;

        if (!@event.IsActionPressed("interact"))
            return;

        _isOpen = true;

        _interactionPrompt.Visible = false;

        GD.Print(ContentsMessage);

        GetViewport().SetInputAsHandled();
    }


    public Godot.Collections.Dictionary<string, Variant>
    CapturePersistentState()
    {
        var state =
            new Godot.Collections.Dictionary<string, Variant>();

        state["is_open"] = _isOpen;

        return state;
    }


    public void RestorePersistentState(
        Godot.Collections.Dictionary<string, Variant> state)
    {
        _isOpen =
            state.ContainsKey("is_open") &&
            state["is_open"].AsBool();

        _playerInRange = false;

        _interactionPrompt.Visible = false;
    }
}