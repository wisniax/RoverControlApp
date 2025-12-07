using System;
using System.Linq;
using System.Collections.Generic;
using Godot;

namespace RoverControlApp.Core.RoverControllerPresets;

public static class DualSeatEvent
{
	public enum InputDevice : int
	{
		Universal = -1,
		Master = 0,
		Slave = 1,
	}

	private static Dictionary<StringName, StringName[]> _nameMemory = [];

	public static StringName GetName(StringName eventName, InputDevice inputDevice = InputDevice.Universal)
	{
		if (!_nameMemory.ContainsKey(eventName))
			return new StringName();

		if (inputDevice == InputDevice.Universal)
			return eventName;

		return _nameMemory[eventName][(int)inputDevice];
	}

	public static void GenerateStrings(StringName eventName)
	{
		List<StringName> eventNames = [];
		foreach (var inputDevice in Enum.GetValues<InputDevice>().SkipLast(1))
		{
			eventNames.Add($"{eventName}_{(int)inputDevice}");
		}
		_nameMemory.Add(eventName,eventNames.ToArray());
	}
}
