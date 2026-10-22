using Godot;

public partial class Main : Node
{
    [Export]
    public PackedScene StartingRoom { get; set; } = null!;

    [Export]
    public string StartingSpawnId { get; set; } = "PlayerSpawn";

    [Export(PropertyHint.Range, "0.05,1.0,0.05")]
    public float FadeDuration { get; set; } = 0.25f;

    private Node2D _roomHost = null!;
    private ColorRect _fadeRect = null!;
    private RoomRoot? _currentRoom;
    private bool _transitionInProgress;

    public override void _Ready()
    {
        _roomHost = GetNode<Node2D>("RoomHost");
        CreateFadeOverlay();

        if (StartingRoom == null)
        {
            GD.PushError("Main: Assign a StartingRoom in the Inspector.");
            return;
        }

        // Begin with a black screen, create the first room, then reveal it.
        LoadRoom(StartingRoom, StartingSpawnId, false);

        Tween introTween = CreateTween();
        introTween.TweenProperty(
            _fadeRect, "modulate:a", 0.0f, GetFadeDuration());
    }

    public void TransitionTo(
        PackedScene destinationRoom,
        RoomExit.ExitDirectionType exitDirection,
        string destinationSpawnId = "")
    {
        if (_transitionInProgress)
            return;

        if (destinationRoom == null)
        {
            GD.PushWarning("Main: Cannot transition to an empty room scene.");
            return;
        }

        _transitionInProgress = true;

        Tween transitionTween = CreateTween();
        transitionTween.TweenProperty(
            _fadeRect, "modulate:a", 1.0f, GetFadeDuration());

        // Swap rooms only after the screen is fully black.
        transitionTween.TweenCallback(Callable.From(() =>
        {
            bool arriveAtPreviousExit =
                exitDirection == RoomExit.ExitDirectionType.PreviousLevel &&
                string.IsNullOrWhiteSpace(destinationSpawnId);

            LoadRoom(destinationRoom, destinationSpawnId, arriveAtPreviousExit);
        }));

        transitionTween.TweenProperty(
            _fadeRect, "modulate:a", 0.0f, GetFadeDuration());
        transitionTween.TweenCallback(Callable.From(() =>
        {
            _transitionInProgress = false;
        }));
    }

    private void LoadRoom(
        PackedScene roomScene,
        string spawnId,
        bool arriveAtPreviousExit)
    {
        if (_currentRoom != null && GodotObject.IsInstanceValid(_currentRoom))
        {
            _currentRoom.CapturePersistentState();
            _roomHost.RemoveChild(_currentRoom);
            _currentRoom.QueueFree();
            _currentRoom = null;
        }

        RoomRoot nextRoom = roomScene.Instantiate<RoomRoot>();
        nextRoom.ArrivalSpawnId = string.IsNullOrWhiteSpace(spawnId)
            ? "PlayerSpawn"
            : spawnId;
        nextRoom.ArriveAtNextLevelExit = arriveAtPreviousExit;

        _roomHost.AddChild(nextRoom);
        _currentRoom = nextRoom;
    }

    private void CreateFadeOverlay()
    {
        CanvasLayer fadeLayer = new CanvasLayer
        {
            Name = "TransitionFade",
            Layer = 100
        };
        AddChild(fadeLayer);

        _fadeRect = new ColorRect
        {
            Name = "FadeRect",
            Color = Colors.Black,
            MouseFilter = Control.MouseFilterEnum.Ignore,
            Modulate = new Color(1.0f, 1.0f, 1.0f, 1.0f)
        };
        _fadeRect.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        fadeLayer.AddChild(_fadeRect);
    }

    private float GetFadeDuration()
    {
        return Mathf.Max(0.01f, FadeDuration);
    }
}
