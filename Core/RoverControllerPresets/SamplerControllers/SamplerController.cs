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
		"sampler_container_3",
		"sampler_container_4",
	];

	public static int LastMovedContainer { get; set; } = -1;

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
			ContainerDegrees3 = lastState.ContainerDegrees3,
			ContainerDegrees4 = lastState.ContainerDegrees4,
			Timestamp = DateTimeOffset.Now.ToUnixTimeMilliseconds(),
		};

		if (inputEvent.IsActionPressed("sampler_container_0", allowEcho: false, exactMatch: true))
		{
			LastMovedContainer = 0;
			newSamplerControl.ContainerDegrees0 = OperateContainer(
				LocalSettings.Singleton.Sampler.Container0,
				true,
				lastState.ContainerDegrees0
			);
		}
		if (inputEvent.IsActionPressed("sampler_container_1", allowEcho: false, exactMatch: true))
		{
			LastMovedContainer = 1;
			newSamplerControl.ContainerDegrees1 = OperateContainer(
				LocalSettings.Singleton.Sampler.Container1,
				true,
				lastState.ContainerDegrees1
			);
		}
		if (inputEvent.IsActionPressed("sampler_container_2", allowEcho: false, exactMatch: true))
		{
			LastMovedContainer = 2;
			newSamplerControl.ContainerDegrees2 = OperateContainer(
				LocalSettings.Singleton.Sampler.Container2,
				true,
				lastState.ContainerDegrees2
			);
		}
		if (inputEvent.IsActionPressed("sampler_container_3", allowEcho: false, exactMatch: true))
		{
			LastMovedContainer = 3;
			newSamplerControl.ContainerDegrees3 = OperateContainer(
				LocalSettings.Singleton.Sampler.Container3,
				true,
				lastState.ContainerDegrees3
			);
		}
		if (inputEvent.IsActionPressed("sampler_container_4", allowEcho: false, exactMatch: true))
		{
			LastMovedContainer = 4;
			newSamplerControl.ContainerDegrees4 = OperateContainer(
				LocalSettings.Singleton.Sampler.Container4,
				true,
				lastState.ContainerDegrees4
			);
		}

		if (inputEvent.IsActionPressed("sampler_container_precise_up", allowEcho: false, exactMatch: true))
		{
			switch (LastMovedContainer)
			{
				case 0:
					newSamplerControl.ContainerDegrees0 += LocalSettings.Singleton.Sampler.Container0.PreciseStep;
					if (newSamplerControl.ContainerDegrees0 > 180f) newSamplerControl.ContainerDegrees0 = 180f;
					break;
				case 1:
					newSamplerControl.ContainerDegrees1 += LocalSettings.Singleton.Sampler.Container1.PreciseStep;
					if (newSamplerControl.ContainerDegrees1 > 180f) newSamplerControl.ContainerDegrees1 = 180f;
					break;
				case 2:
					newSamplerControl.ContainerDegrees2 += LocalSettings.Singleton.Sampler.Container2.PreciseStep;
					if (newSamplerControl.ContainerDegrees2 > 180f) newSamplerControl.ContainerDegrees2 = 180f;
					break;
				case 3:
					newSamplerControl.ContainerDegrees3 += LocalSettings.Singleton.Sampler.Container3.PreciseStep;
					if (newSamplerControl.ContainerDegrees3 > 180f) newSamplerControl.ContainerDegrees3 = 180f;
					break;
				case 4:
					newSamplerControl.ContainerDegrees4 += LocalSettings.Singleton.Sampler.Container4.PreciseStep;
					if (newSamplerControl.ContainerDegrees4 > 180f) newSamplerControl.ContainerDegrees4 = 180f;
					break;
			}
		}
		if (inputEvent.IsActionPressed("sampler_container_precise_down", allowEcho: false, exactMatch: true))
		{
			switch (LastMovedContainer)
			{
				case 0:
					newSamplerControl.ContainerDegrees0 -= LocalSettings.Singleton.Sampler.Container0.PreciseStep;
					if (newSamplerControl.ContainerDegrees0 < 0f) newSamplerControl.ContainerDegrees0 = 0f;
					break;
				case 1:
					newSamplerControl.ContainerDegrees1 -= LocalSettings.Singleton.Sampler.Container1.PreciseStep;
					if (newSamplerControl.ContainerDegrees1 < 0f) newSamplerControl.ContainerDegrees1 = 0f;
					break;
				case 2:
					newSamplerControl.ContainerDegrees2 -= LocalSettings.Singleton.Sampler.Container2.PreciseStep;
					if (newSamplerControl.ContainerDegrees2 < 0f) newSamplerControl.ContainerDegrees2 = 0f;
					break;
				case 3:
					newSamplerControl.ContainerDegrees3 -= LocalSettings.Singleton.Sampler.Container3.PreciseStep;
					if (newSamplerControl.ContainerDegrees3 < 0f) newSamplerControl.ContainerDegrees3 = 0f;
					break;
				case 4:
					newSamplerControl.ContainerDegrees4 -= LocalSettings.Singleton.Sampler.Container4.PreciseStep;
					if (newSamplerControl.ContainerDegrees4 < 0f) newSamplerControl.ContainerDegrees4 = 0f;
					break;
			}
		}

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
