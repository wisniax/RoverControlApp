using System;
using System.Collections.Generic;

using Godot;

using static RoverControlApp.Core.MqttClasses;

namespace RoverControlApp.Core.RoverControllerPresets.SamplerControllers;

public class SamplerController : IRoverSamplerController
{
	private readonly string[] _usedActions =
	[
		"sampler_move_down", "sampler_move_up",
		"sampler_drill_down", "sampler_drill_up",
		"sampler_drill_movement",
		"sampler_platform_movement",
		"sampler_drill_enable",
		"sampler_container_0",
		"sampler_container_1",
		"sampler_container_2",
	];

	public SamplerControl CalculateMoveVector(in InputEvent inputEvent, in SamplerControl lastState)
	{
		float movement = Input.GetAxis("sampler_move_down", "sampler_move_up");
		if (Mathf.Abs(movement) < LocalSettings.Singleton.Joystick.MinimalInput)
			movement = 0f;

		float drillSpeed = Input.GetAxis("sampler_drill_down", "sampler_drill_up");
		if (Mathf.Abs(drillSpeed) < LocalSettings.Singleton.Joystick.MinimalInput)
			drillSpeed = 0f;

		SamplerControl newSamplerControl = new()
		{
			DrillMovement = Input.IsActionPressed("sampler_drill_movement") ? movement : 0f,
			PlatformMovement = Input.IsActionPressed("sampler_platform_movement") ? movement : 0f,
			DrillAction = Input.IsActionPressed("sampler_drill_enable") ? drillSpeed : 0f,
			ContainerDegrees0 = lastState.ContainerDegrees0,
			ContainerDegrees1 = lastState.ContainerDegrees1,
			ContainerDegrees2 = lastState.ContainerDegrees2,
			Timestamp = DateTimeOffset.Now.ToUnixTimeMilliseconds(),
		};

		if (inputEvent.IsActionPressed("sampler_container_0", allowEcho: false, exactMatch: true))
			newSamplerControl.ContainerDegrees0 = OperateContainer(
				LocalSettings.Singleton.Sampler.Container0,
				true,
				lastState.ContainerDegrees0
			);
		if (inputEvent.IsActionPressed("sampler_container_1", allowEcho: false, exactMatch: true))
			newSamplerControl.ContainerDegrees1 = OperateContainer(
				LocalSettings.Singleton.Sampler.Container1,
				true,
				lastState.ContainerDegrees1
			);
		if (inputEvent.IsActionPressed("sampler_container_2", allowEcho: false, exactMatch: true))
			newSamplerControl.ContainerDegrees2 = OperateContainer(
				LocalSettings.Singleton.Sampler.Container2,
				true,
				lastState.ContainerDegrees2
			);

		return newSamplerControl;
	}

	private static float OperateContainer(Settings.SamplerContainer samplerContainer, bool changeState, float lastState)
	{
		if (!changeState)
			return lastState;

		if (samplerContainer.Position1 == samplerContainer.Position2)
		{
			return Switch2(samplerContainer, lastState);
		}

		return Switch3(samplerContainer, lastState);
	}

	private static float Switch2(Settings.SamplerContainer samplerContainer, float lastState)
	{
		if (samplerContainer.Position0 == lastState)
			return samplerContainer.Position1;
		if (samplerContainer.Position1 == lastState)
			return samplerContainer.Position0;
		return samplerContainer.Position0;
	}

	private static float Switch3(Settings.SamplerContainer samplerContainer, float lastState)
	{
		if (samplerContainer.Position0 == lastState)
			return samplerContainer.Position1;
		if (samplerContainer.Position1 == lastState)
			return samplerContainer.Position2;
		if (samplerContainer.Position2 == lastState)
			return samplerContainer.Position0;
		return samplerContainer.Position0;
	}

	public Dictionary<string, Godot.Collections.Array<InputEvent>> GetInputActions() =>
		IActionAwareController.FetchAllActionEvents(_usedActions);
}
