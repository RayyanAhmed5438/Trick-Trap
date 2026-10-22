using Godot;

public partial class RoomExit : Area2D
{
    [Export]
    public PackedScene DestinationRoom { get; set; } = null!;

    [Export]
    public string DestinationSpawnId { get; set; } = "PlayerSpawn";


    private bool _playerInRange = false;
    private bool _transitionPending = false;


    public override void _Ready()
    {
        BodyEntered += OnBodyEntered;
        BodyExited += OnBodyExited;
    }


    private void OnBodyEntered(Node2D body)
    {
        if (!body.IsInGroup("player"))
            return;

        _playerInRange = true;

        GD.Print("Press E to use the exit.");
    }


    private void OnBodyExited(Node2D body)
    {
        if (body.IsInGroup("player"))
            _playerInRange = false;
    }


    public override void _UnhandledInput(InputEvent @event)
    {
        if (!_playerInRange || _transitionPending)
            return;

        if (!@event.IsActionPressed("interact"))
            return;

        if (DestinationRoom == null)
        {
            GD.PushWarning(
                $"{Name}: Assign a DestinationRoom."
            );

            return;
        }

        _transitionPending = true;

        GetViewport().SetInputAsHandled();

        // Defer unloading this room until input processing finishes.
        CallDeferred(nameof(PerformTransition));
    }


    private void PerformTransition()
    {
        Main main = GetNode<Main>("/root/Main");

        main.TransitionTo(
            DestinationRoom,
            DestinationSpawnId
        );
    }
}