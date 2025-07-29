using System.Collections.Generic;
using Godot;
using Renderite.Godot.Source.Helpers;
using Renderite.Shared;

namespace Renderite.Godot.Source;

public partial class InputManager : Node
{
	public static InputManager Instance;
	private HashSet<Shared.Key> _heldKeys = new HashSet<Shared.Key>();

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
				_heldKeys.Add(keyEvent.Keycode.ToRenderite(keyEvent.Location));
			else
				_heldKeys.Remove(keyEvent.Keycode.ToRenderite(keyEvent.Location));
		}
	}

	public InputState GetInputState() => new()
	{
		keyboard = new KeyboardState
		{
			heldKeys = _heldKeys,
			// TODO: typeDelta (if I'm thinking about this correctly it's used for actual typing)
		}
		// TODO: mouse and window state
	};
}
