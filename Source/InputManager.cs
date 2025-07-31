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

    private Vector2 _mouseDelta = Vector2.Zero;
    private float _scrollDelta = 0.0f;

    private bool _lastMouseLocked;
    private Vector2I? _lastLockPosition;

    public override void _Ready()
    {
        base._Ready();
        Instance = this;
    }

    public override void _Input(InputEvent @event)
    {
        switch (@event)
        {
            case InputEventKey { Pressed: true } keyDown:
                _heldKeys.Add(keyDown.Keycode.ToRenderite(keyDown.Location));
                if (keyDown.Unicode > 0)
                    _typeDelta.Append((char)keyDown.Unicode);
                break;

            case InputEventKey keyUp:
                _heldKeys.Remove(keyUp.Keycode.ToRenderite(keyUp.Location));
                break;

            case InputEventMouseMotion motion:
                var delta = motion.Relative;
                delta.Y = -delta.Y; // Unity has this flipped it seems
                _mouseDelta += delta;
                break;

            case InputEventMouseButton { ButtonIndex: MouseButton.WheelUp } wheelUp:
                _scrollDelta += wheelUp.Factor;
                break;

            case InputEventMouseButton { ButtonIndex: MouseButton.WheelDown } wheelDown:
                _scrollDelta -= wheelDown.Factor;
                break;
        }
    }

    public InputState GetInputState()
    {
        var typeDelta = _typeDelta.ToString();
        _typeDelta.Clear();

        var mouseDelta = _mouseDelta;
        _mouseDelta = Vector2.Zero;
        var mouseScreenPosition = GetViewport().GetMousePosition() * GetViewport().GetScreenTransform();

        var scrollDelta = _scrollDelta;
        _scrollDelta = 0.0f;

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
            },
            mouse = new MouseState
            {
                isActive = true,
                directDelta = mouseDelta.ToRenderite(),
                windowPosition = mouseScreenPosition.ToRenderite(),
                scrollWheelDelta = new RenderVector2(0.0f, scrollDelta * 3.0f), // arbitrary factor, it just felt too slow
                leftButtonState = Input.IsMouseButtonPressed(MouseButton.Left),
                rightButtonState = Input.IsMouseButtonPressed(MouseButton.Right),
                middleButtonState = Input.IsMouseButtonPressed(MouseButton.Middle),
                button4State = Input.IsMouseButtonPressed(MouseButton.Xbutton1),
                button5State = Input.IsMouseButtonPressed(MouseButton.Xbutton2)
            }
        };
    }

    public void Handle(OutputState state)
    {
        var newLockPos = state.lockCursorPosition?.ToGodot();

        if (newLockPos.HasValue)
            Input.WarpMouse(newLockPos.Value);

        if (state.lockCursor == _lastMouseLocked && newLockPos == _lastLockPosition)
            return;

        var previousLockPos = _lastLockPosition;
        _lastMouseLocked = state.lockCursor;
        _lastLockPosition = newLockPos;

        if (state.lockCursor)
        {
            Input.MouseMode = newLockPos.HasValue
            ? Input.MouseModeEnum.ConfinedHidden
            : Input.MouseModeEnum.Captured;
        }
        else
        {
            Input.MouseMode = Input.MouseModeEnum.Visible;
            if (previousLockPos.HasValue)
                Input.WarpMouse(previousLockPos.Value);
        }
        // TODO: verify this all works correctly
        // in fact something already isn't working correctly from what I can tell,
        // grabbing things and holding e to rotate snaps back for some reason
    }
}
