using System.Collections.Generic;
using System.Text;
using Godot;
using Renderite.Godot.Source.Helpers;
using Renderite.Shared;

namespace Renderite.Godot.Source;

public partial class InputManager : Node
{
    public static InputManager Instance;
    private HashSet<Shared.Key> _heldKeys = new();
    private StringBuilder _typeDelta = new();

    public override void _Ready()
    {
        base._Ready();
        Instance = this;
    }

    public override void _Input(InputEvent @event)
    {
        if (@event is InputEventKey keyEvent)
        {
            if (keyEvent.Pressed)
            {
                _heldKeys.Add(keyEvent.Keycode.ToRenderite(keyEvent.Location));
                // TODO: Verify this, I'm pretty sure it's gonna be wrong compared to what Unity does
                // Also thanks for the vague docs again
                _typeDelta.Append((char)keyEvent.Unicode);
            }
            else
                _heldKeys.Remove(keyEvent.Keycode.ToRenderite(keyEvent.Location));
        }
    }

    public InputState GetInputState()
    {
        var typeDelta = _typeDelta.ToString();
        _typeDelta.Clear();

        return new InputState
        {
            keyboard = new KeyboardState
            {
                heldKeys = _heldKeys,
                typeDelta = typeDelta,
            },
            window = new WindowState
            {
                isWindowFocused = GetWindow().HasFocus(),
                windowResolution = GetWindow().Size.ToRenderite(),
                // TODO: drag and drop
            }
            // TODO: mouse and window state
        };
    }
}
