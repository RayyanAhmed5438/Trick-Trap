using Godot;
using System.Collections.Generic;
public partial class RoomRoot : Node2D
{
    [Export]
    public float FollowZoom { get; set; } = 3.0f;

    [Export]
    public float MaximumRoomZoom { get; set; } = 3.0f;

    [Export]
    public float CameraSmoothingSpeed { get; set; } = 8.0f;

    private CharacterBody2D _player = null!;
    private Camera2D _camera = null!;

    private bool _followPlayer = false;


    [Export]
    public string RoomId { get; set; } = "";


    // Set by Main before the room enters the scene tree.
    public string ArrivalSpawnId { get; set; } = "PlayerSpawn";

    // Used when returning to a room through its previous-level exit.
    // In that case, the player appears at this room's next-level exit.
    public bool ArriveAtNextLevelExit { get; set; } = false;

    public override void _Ready()
    {
        // Get the player and the room's camera.
        _player = GetNode<CharacterBody2D>(
            "WorldYSort/Player"
        );

        _camera = GetNode<Camera2D>(
            "Camera2D"
        );

        // Restore saved object states before gameplay continues.
        RestorePersistentState();

        // A previous-level exit returns the player to the exit used to leave this room.
        bool playerPlaced = false;
        if (ArriveAtNextLevelExit)
        {
            RoomExit? nextExit = FindNextLevelExit(this);
            if (nextExit != null)
            {
                _player.GlobalPosition = nextExit.GlobalPosition;
                playerPlaced = true;
            }
            else
            {
                GD.PushWarning(
                    "RoomRoot: No NextLevel exit found in room '" + RoomId +
                    "'. Falling back to PlayerSpawn."
                );
            }
        }

        if (!playerPlaced)
        {
            string spawnId = string.IsNullOrWhiteSpace(ArrivalSpawnId)
                ? "PlayerSpawn"
                : ArrivalSpawnId;

            Marker2D? playerSpawn = GetNodeOrNull<Marker2D>(
                "SpawnPoints/" + spawnId
            ) ?? GetNodeOrNull<Marker2D>(
                "SpawnPoints/PlayerSpawn"
            );

            if (playerSpawn != null)
            {
                _player.GlobalPosition = playerSpawn.GlobalPosition;
            }
            else
            {
                GD.PushWarning(
                    "Room '" + RoomId + "' has no valid player spawn marker."
                );
            }
        }

        // Calculate the room's outer bounds.
        Rect2? roomBounds = GetLayerWorldBounds("WallsBehind");

        // If there are no wall tiles, use the floor instead.
        roomBounds ??= GetLayerWorldBounds("Floor");

        if (roomBounds == null)
        {
            GD.PushWarning(
                "RoomRoot: No floor or wall tiles found. " +
                "Camera configuration was skipped."
            );

            return;
        }

        ConfigureCamera(roomBounds.Value);
    }


    public override void _Process(double delta)
    {
        // Large rooms follow the player.
        if (_followPlayer && IsInstanceValid(_player))
        {
            _camera.GlobalPosition = _player.GlobalPosition;
        }
    }


    private RoomExit? FindNextLevelExit(Node parent)
    {
        if (parent is RoomExit exit &&
            exit.ExitDirection == RoomExit.ExitDirectionType.NextLevel)
        {
            return exit;
        }

        foreach (Node child in parent.GetChildren())
        {
            RoomExit? found = FindNextLevelExit(child);
            if (found != null)
                return found;
        }

        return null;
    }

    private Rect2? GetLayerWorldBounds(string layerName)
    {
        if (!HasNode(layerName))
        {
            return null;
        }

        TileMapLayer layer = GetNode<TileMapLayer>(
            layerName
        );

        // Find the rectangle enclosing all painted tiles.
        Rect2I usedRect = layer.GetUsedRect();

        if (usedRect.Size.X <= 0 ||
            usedRect.Size.Y <= 0 ||
            layer.TileSet == null)
        {
            return null;
        }

        // Calculate the dimensions of one tile.
        Vector2I tileSizeI = layer.TileSet.TileSize;

        Vector2 tileSize = new Vector2(
            tileSizeI.X,
            tileSizeI.Y
        );

        Vector2 halfTile = tileSize / 2.0f;

        // Get the outer edges of the used tile rectangle.
        Vector2 localTopLeft =
            layer.MapToLocal(usedRect.Position) - halfTile;

        Vector2 localBottomRight =
            layer.MapToLocal(usedRect.End) - halfTile;

        // Convert these positions into world coordinates.
        Vector2 worldTopLeft =
            layer.ToGlobal(localTopLeft);

        Vector2 worldBottomRight =
            layer.ToGlobal(localBottomRight);

        return new Rect2(
            worldTopLeft,
            worldBottomRight - worldTopLeft
        );
    }


    private void ConfigureCamera(Rect2 bounds)
    {
        Vector2 viewportSize = GetViewportRect().Size;

        if (bounds.Size.X <= 0 ||
            bounds.Size.Y <= 0 ||
            viewportSize.X <= 0 ||
            viewportSize.Y <= 0)
        {
            GD.PushWarning(
                "RoomRoot: Invalid room or viewport dimensions."
            );

            return;
        }

        // Calculate the zoom required to fit the room.
        float zoomX = viewportSize.X / bounds.Size.X;
        float zoomY = viewportSize.Y / bounds.Size.Y;

        float fitZoom = Mathf.Min(zoomX, zoomY);

        // If the room fits at FollowZoom, keep the entire
        // room visible instead of following the player.
        _followPlayer = fitZoom < FollowZoom;

        if (!_followPlayer)
        {
            // Use integer zoom for consistent pixel-art scaling.
            float zoom = Mathf.Clamp(
                Mathf.Floor(fitZoom),
                1.0f,
                MaximumRoomZoom
            );

            _camera.Zoom = new Vector2(zoom, zoom);

            // Center the camera on the entire room.
            _camera.GlobalPosition = bounds.GetCenter();

            _camera.LimitEnabled = false;
            _camera.PositionSmoothingEnabled = false;
        }
        else
        {
            // Larger rooms use a fixed zoom and follow the player.
            float zoom = Mathf.Max(1.0f, FollowZoom);

            _camera.Zoom = new Vector2(zoom, zoom);

            // Stop the camera from scrolling beyond the room.
            _camera.LimitEnabled = true;

            _camera.LimitLeft =
                Mathf.FloorToInt(bounds.Position.X);

            _camera.LimitTop =
                Mathf.FloorToInt(bounds.Position.Y);

            _camera.LimitRight =
                Mathf.CeilToInt(bounds.End.X);

            _camera.LimitBottom =
                Mathf.CeilToInt(bounds.End.Y);

            _camera.PositionSmoothingEnabled = true;
            _camera.PositionSmoothingSpeed =
                CameraSmoothingSpeed;

            _camera.GlobalPosition = _player.GlobalPosition;

            // Avoid an initial smoothing jump.
            _camera.ResetSmoothing();
        }
    }


    public void CapturePersistentState()
    {
        if (string.IsNullOrWhiteSpace(RoomId))
        {
            GD.PushWarning(
                "RoomRoot: Cannot capture state without a RoomId."
            );

            return;
        }

        var objectStates =
            new Dictionary<
                string,
                Godot.Collections.Dictionary<string, Variant>
            >();

        CollectPersistentStates(this, objectStates);

        GameState gameState = GetNode<GameState>(
            "/root/GameState"
        );

        gameState.SaveRoomObjectStates(RoomId, objectStates);
    }


    private void CollectPersistentStates(
        Node parent,
        Dictionary<
            string,
            Godot.Collections.Dictionary<string, Variant>
        > objectStates)
    {
        if (parent is IRoomStateful stateful)
        {
            string id = stateful.PersistentId;

            if (string.IsNullOrWhiteSpace(id))
            {
                GD.PushWarning(
                    $"{parent.GetPath()} implements IRoomStateful " +
                    "but has no PersistentId."
                );
            }
            else if (objectStates.ContainsKey(id))
            {
                GD.PushWarning(
                    $"Duplicate PersistentId '{id}' in room '{RoomId}'."
                );
            }
            else
            {
                objectStates[id] = stateful.CapturePersistentState();
            }
        }

        // Search nested containers as well as direct children.
        foreach (Node child in parent.GetChildren())
        {
            CollectPersistentStates(child, objectStates);
        }
    }


    private void RestorePersistentState()
    {
        if (string.IsNullOrWhiteSpace(RoomId))
        {
            GD.PushWarning(
                "RoomRoot: Cannot restore state without a RoomId."
            );

            return;
        }

        GameState gameState = GetNode<GameState>(
            "/root/GameState"
        );

        var savedStates = gameState.GetRoomObjectStates(RoomId);

        if (savedStates.Count == 0)
            return;

        RestorePersistentNodes(this, savedStates);
    }


    private void RestorePersistentNodes(
        Node parent,
        Dictionary<
            string,
            Godot.Collections.Dictionary<string, Variant>
        > savedStates)
    {
        if (parent is IRoomStateful stateful &&
            !string.IsNullOrWhiteSpace(stateful.PersistentId) &&
            savedStates.TryGetValue(
                stateful.PersistentId,
                out var state))
        {
            stateful.RestorePersistentState(state);
        }

        foreach (Node child in parent.GetChildren())
        {
            RestorePersistentNodes(child, savedStates);
        }
    }
}