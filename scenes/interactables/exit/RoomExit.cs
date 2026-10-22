using Godot;

public partial class RoomExit : Area2D
{
    [Export(PropertyHint.File, "*.tscn")]
    public string DestinationRoomPath { get; set; } = "";

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
        if (!body.IsInGroup("player"))
            return;

        _playerInRange = false;
    }


    public override void _UnhandledInput(InputEvent @event)
    {
        if (!_playerInRange || _transitionPending)
            return;

        if (!@event.IsActionPressed("interact"))
            return;

        GetViewport().SetInputAsHandled();

        _transitionPending = true;

        // Defer the transition until input processing finishes.
        CallDeferred(nameof(PerformTransition));
    }


    private void PerformTransition()
    {
        if (string.IsNullOrWhiteSpace(DestinationRoomPath) ||
            !ResourceLoader.Exists(DestinationRoomPath))
        {
            GD.PushWarning(
                $"{Name}: Destination room path is empty or invalid: " +
                $"'{DestinationRoomPath}'."
            );

            _transitionPending = false;
            return;
        }

        PackedScene? destinationRoom =
            ResourceLoader.Load<PackedScene>(DestinationRoomPath);

        if (destinationRoom == null)
        {
            GD.PushWarning(
                $"{Name}: Failed to load room: {DestinationRoomPath}"
            );

            _transitionPending = false;
            return;
        }

        Main? main = GetNodeOrNull<Main>("/root/Main");

        if (main == null)
        {
            GD.PushError(
                "RoomExit: Could not find Main at /root/Main."
            );

            _transitionPending = false;
            return;
        }

        main.TransitionTo(
            destinationRoom,
            DestinationSpawnId
        );
    }
}