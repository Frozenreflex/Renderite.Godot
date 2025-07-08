using Godot;

namespace Renderite.Godot.Source;

public partial class Main : Node3D
{
    public static Main Instance;

    public override void _Ready()
    {
        base._Ready();

        Instance = this;
    }
}