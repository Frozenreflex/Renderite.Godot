using System.Collections.Generic;
using System.Text;
using Godot;
using Renderite.Godot.Source.Helpers;
using Renderite.Shared;

namespace Renderite.Godot.Source;

public partial class InputManager : Node
{
    public static InputManager Instance;

    private readonly StringBuilder _typeDelta = new();

    private List<string> _droppedFiles = [];

    private Vector2 _mouseDelta = Vector2.Zero;
    private float _scrollDelta = 0.0f;

    private bool _lastMouseLocked;
    private Vector2I? _lastLockPosition;

    private readonly InputState _inputState = new InputState
    {
        keyboard = new KeyboardState
        {
            heldKeys = [],
        },
        window = new WindowState(),
        mouse = new MouseState
        {
            isActive = true,
        },
    };

    public override void _Ready()
    {
        base._Ready();
        Instance = this;
        GetWindow().FilesDropped += (files => { _droppedFiles.AddRange(files); });
    }

    public override void _Input(InputEvent @event)
    {
        switch (@event)
        {
            case InputEventKey { Pressed: true } keyDown:
                _inputState.keyboard.heldKeys.Add(keyDown.Keycode.ToRenderite(keyDown.Location));
                if (keyDown.Unicode > 0)
                    _typeDelta.Append((char)keyDown.Unicode);
                else
                {
                    switch (keyDown.Keycode)
                    {
                        case global::Godot.Key.Backspace:
                            _typeDelta.Append('\b');
                            break;
                        case global::Godot.Key.Enter:
                            _typeDelta.Append('\n');
                            break;
                    }
                }

                // TODO: remove when clipboard is added on linux
                if (!OS.HasFeature("windows") && keyDown.Keycode == global::Godot.Key.V &&
                    Input.IsKeyPressed(global::Godot.Key.Ctrl) &&
                    DisplayServer.ClipboardHas())
                {
                    var text = DisplayServer.ClipboardGet();
                    if (Input.MouseMode != Input.MouseModeEnum.Captured)
                    {
                        _typeDelta.Remove(_typeDelta.Length - 1, 1);
                        _typeDelta.Append(text);
                    }
                    else
                        _droppedFiles.Add(text);
                }

                break;

            case InputEventKey keyUp:
                _inputState.keyboard.heldKeys.Remove(keyUp.Keycode.ToRenderite(keyUp.Location));
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
        var mouseScreenPosition = GetViewport().GetMousePosition() * GetViewport().GetScreenTransform();

        _inputState.keyboard.typeDelta = _typeDelta.ToString();

        _inputState.window.isWindowFocused = GetWindow().HasFocus();
        _inputState.window.windowResolution = GetWindow().Size.ToRenderite();
        _inputState.window.dragAndDropEvent = _droppedFiles.Count != 0
            ? new DragAndDropEvent
            {
                paths = _droppedFiles,
                dropPoint = new RenderVector2i() // NOTE: don't bother with this, it isn't actually used for anything 
            }
            : null;

        _inputState.mouse.directDelta = _mouseDelta.ToRenderite();
        _inputState.mouse.desktopPosition = Vector2.Zero.ToRenderite();
        _inputState.mouse.windowPosition = mouseScreenPosition.ToRenderite();
        _inputState.mouse.scrollWheelDelta =
            new RenderVector2(0.0f, _scrollDelta * 15.0f); // arbitrary factor, it just felt WAY too slow
        _inputState.mouse.leftButtonState = Input.IsMouseButtonPressed(MouseButton.Left);
        _inputState.mouse.rightButtonState = Input.IsMouseButtonPressed(MouseButton.Right);
        _inputState.mouse.middleButtonState = Input.IsMouseButtonPressed(MouseButton.Middle);
        _inputState.mouse.button4State = Input.IsMouseButtonPressed(MouseButton.Xbutton1);
        _inputState.mouse.button5State = Input.IsMouseButtonPressed(MouseButton.Xbutton2);
        _inputState.vr = HeadOutputManager.Instance.GetVRInputState();

        _typeDelta.Clear();
        _mouseDelta = Vector2.Zero;
        _scrollDelta = 0.0f;
        _droppedFiles = [];
        return _inputState;
    }

    public void Handle(OutputState state)
    {
        var newLockPos = state.lockCursorPosition?.ToGodot();

        if (newLockPos.HasValue)
        {
            var currentPos = GetViewport().GetMousePosition() * GetViewport().GetScreenTransform();
            _mouseDelta -= currentPos - (Vector2)newLockPos;
            Input.WarpMouse(newLockPos.Value);
        }

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
            {
                var currentPos = GetViewport().GetMousePosition() * GetViewport().GetScreenTransform();
                _mouseDelta -= currentPos - (Vector2)previousLockPos.Value;
                Input.WarpMouse(previousLockPos.Value);
            }
        }
    }
}