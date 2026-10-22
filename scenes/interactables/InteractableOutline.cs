using Godot;

public partial class InteractableOutline : Node
{
    [Export]
    public NodePath InteractionAreaPath { get; set; } = new NodePath("../InteractionArea");

    [Export]
    public NodePath VisualPath { get; set; } = new NodePath("../Visual");

    [Export]
    public Color OutlineColor { get; set; } = Colors.White;

    [Export(PropertyHint.Range, "1,4,0.5")]
    public float OutlineWidth { get; set; } = 1.0f;

    private Area2D? _interactionArea;
    private CanvasItem? _visual;
    private ShaderMaterial? _outlineMaterial;

    public override void _Ready()
    {
        _interactionArea = GetNodeOrNull<Area2D>(InteractionAreaPath);
        _visual = GetNodeOrNull<CanvasItem>(VisualPath);

        if (_interactionArea == null || _visual == null)
        {
            GD.PushWarning(
                $"{GetPath()}: InteractableOutline needs a valid interaction area " +
                "and visual path."
            );
            return;
        }

        Shader? outlineShader = GD.Load<Shader>(
            "res://shaders/interaction_outline.gdshader"
        );

        if (outlineShader == null)
        {
            GD.PushWarning("InteractableOutline: Could not load the outline shader.");
            return;
        }

        _outlineMaterial = new ShaderMaterial
        {
            Shader = outlineShader
        };
        _outlineMaterial.SetShaderParameter("outline_color", OutlineColor);
        _outlineMaterial.SetShaderParameter("outline_width", OutlineWidth);
        _outlineMaterial.SetShaderParameter("outline_enabled", false);

        _visual.Material = _outlineMaterial;

        _interactionArea.BodyEntered += OnBodyEntered;
        _interactionArea.BodyExited += OnBodyExited;
    }

    private void OnBodyEntered(Node2D body)
    {
        if (body.IsInGroup("player"))
            SetOutlineVisible(true);
    }

    private void OnBodyExited(Node2D body)
    {
        if (body.IsInGroup("player"))
            SetOutlineVisible(false);
    }

    private void SetOutlineVisible(bool visible)
    {
        _outlineMaterial?.SetShaderParameter("outline_enabled", visible);
    }
}
