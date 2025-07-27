using Godot;

namespace Renderite.Godot.Source;

public partial class Main : Node3D
{
    public static Main Instance;
    public static Rid Scenario => _scenario ??= Instance.GetWorld3D().Scenario;
    private static Rid? _scenario;

    public override void _Ready()
    {
        base._Ready();

        Instance = this;
    }
}
