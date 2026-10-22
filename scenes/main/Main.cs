using Godot;

public partial class Main : Node
{
    [Export]
    public PackedScene StartingRoom { get; set; } = null!;

    [Export]
    public string StartingSpawnId { get; set; } = "PlayerSpawn";


    private Node2D _roomHost = null!;
    private RoomRoot? _currentRoom;


    public override void _Ready()
    {
        _roomHost = GetNode<Node2D>("RoomHost");

        if (StartingRoom == null)
        {
            GD.PushError(
                "Main: Assign a StartingRoom in the Inspector."
            );

            return;
        }

        TransitionTo(StartingRoom, StartingSpawnId);
    }


    public void TransitionTo(
        PackedScene destinationRoom,
        string destinationSpawnId)
    {
        if (destinationRoom == null)
        {
            GD.PushWarning(
                "Main: Cannot transition to an empty room scene."
            );

            return;
        }

        // Save the room before removing it.
        if (_currentRoom != null &&
            GodotObject.IsInstanceValid(_currentRoom))
        {
            _currentRoom.CapturePersistentState();

            _roomHost.RemoveChild(_currentRoom);
            _currentRoom.QueueFree();

            _currentRoom = null;
        }

        // Instantiate the destination room.
        RoomRoot nextRoom =
            destinationRoom.Instantiate<RoomRoot>();

        // Set this before AddChild, so RoomRoot sees it in _Ready.
        nextRoom.ArrivalSpawnId = destinationSpawnId;

        _roomHost.AddChild(nextRoom);

        _currentRoom = nextRoom;
    }
}