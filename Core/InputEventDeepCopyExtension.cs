using Godot;

namespace RoverControlApp.Core;
public static class InputEventDeepCopyExtensions
{
	public static InputEventKey DeepCopy(this InputEventKey original)
	{
		return new InputEventKey
		{
			Keycode = original.Keycode,
			PhysicalKeycode = original.PhysicalKeycode,
			KeyLabel = original.KeyLabel,
			Echo = original.Echo,
			Device = original.Device
		};
	}

	public static InputEventJoypadButton DeepCopy(this InputEventJoypadButton original)
	{
		return new InputEventJoypadButton
		{
			ButtonIndex = original.ButtonIndex,
			Device = original.Device
		};
	}

	public static InputEventJoypadMotion DeepCopy(this InputEventJoypadMotion original)
	{
		return new InputEventJoypadMotion
		{
			Axis = original.Axis,
			Device = original.Device
		};
	}
}
