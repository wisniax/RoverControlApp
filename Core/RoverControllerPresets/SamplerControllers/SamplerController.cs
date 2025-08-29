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
		"sampler_alt_mode",
		"sampler_drill_movement",
		"sampler_platform_movement",
		"sampler_drill_enable",
		"sampler_container_1",
		"sampler_container_2",
		"sampler_container_3",
		"sampler_container_4",
		"sampler_container_precise_up",
		"sampler_container_precise_down",
	];

	public static int LastMovedContainer { get; set; } = -1;
	public static bool AltMode { get; set; } = false;

	public SamplerControl CalculateMoveVector(in InputEvent inputEvent, in SamplerControl lastState)
	{
		float movement = Input.GetAxis("sampler_move_down", "sampler_move_up");
		if (Mathf.Abs(movement) < LocalSettings.Singleton.Joystick.MinimalInput)
			movement = 0f;

		float drillSpeed = Input.GetAxis("sampler_drill_down", "sampler_drill_up");
		//if (Mathf.Abs(drillSpeed) < LocalSettings.Singleton.Joystick.MinimalInput)
		//	drillSpeed = 0f; //No deadzone for trigger

		if (inputEvent.IsActionPressed("sampler_alt_mode", allowEcho: false, exactMatch: true)) AltMode = true;
		if (inputEvent.IsActionReleased("sampler_alt_mode", exactMatch: true)) AltMode = false;



		SamplerControl newSamplerControl;

		if (AltMode)
		{
			newSamplerControl = new()
			{
				DrillMovement = movement,
				PlatformMovement = 0f,
				DrillAction = movement,
				ContainerDegrees0 = lastState.ContainerDegrees0,
				VacuumSuction = lastState.VacuumSuction,
				VacuumA = lastState.VacuumA,
				VacuumB = lastState.VacuumB,
				Timestamp = DateTimeOffset.Now.ToUnixTimeMilliseconds(),
			};
		}
		else
		{
			newSamplerControl = new()
			{
				DrillMovement = Input.IsActionPressed("sampler_drill_movement") ? movement : 0f,
				PlatformMovement = Input.IsActionPressed("sampler_platform_movement") ? movement : 0f,
				DrillAction = Input.IsActionPressed("sampler_drill_enable") ? drillSpeed : 0f,
				ContainerDegrees0 = lastState.ContainerDegrees0,
				VacuumSuction = lastState.VacuumSuction,
				VacuumA = lastState.VacuumA,
				VacuumB = lastState.VacuumB,
				Timestamp = DateTimeOffset.Now.ToUnixTimeMilliseconds(),
			};
		};

		if (inputEvent.IsActionPressed("sampler_container_1", allowEcho: false, exactMatch: true))
		{
			LastMovedContainer = 1;
			newSamplerControl.ContainerDegrees0 = OperateContainer(
				LocalSettings.Singleton.Sampler.Container1,
				true,
				newSamplerControl.ContainerDegrees0
			);
		}
		if (inputEvent.IsActionPressed("sampler_container_2", allowEcho: false, exactMatch: true))
		{
			LastMovedContainer = 2;
			newSamplerControl.VacuumSuction = OperateContainer(
				LocalSettings.Singleton.Sampler.Container2,
				true,
				newSamplerControl.VacuumSuction
			);
		}
		if (inputEvent.IsActionPressed("sampler_container_3", allowEcho: false, exactMatch: true))
		{
			LastMovedContainer = 3;
			newSamplerControl.VacuumA = OperateContainer(
				LocalSettings.Singleton.Sampler.Container3,
				true,
				newSamplerControl.VacuumA
			);
		}
		if (inputEvent.IsActionPressed("sampler_container_4", allowEcho: false, exactMatch: true))
		{
			LastMovedContainer = 4;
			newSamplerControl.VacuumB = OperateContainer(
				LocalSettings.Singleton.Sampler.Container4,
				true,
				newSamplerControl.VacuumB
			);
		}

		if (inputEvent.IsActionPressed("sampler_container_precise_up", allowEcho: false, exactMatch: true))
		{
			switch (LastMovedContainer)
			{
				case 1:
					newSamplerControl.ContainerDegrees0 += LocalSettings.Singleton.Sampler.Container1.PreciseStep;
					if (newSamplerControl.ContainerDegrees0 > 1000000f) newSamplerControl.ContainerDegrees0 = 1000000f;
					break;
				case 2:
					newSamplerControl.VacuumSuction += LocalSettings.Singleton.Sampler.Container2.PreciseStep;
					if (newSamplerControl.VacuumSuction > 1f) newSamplerControl.VacuumSuction = 1f;
					break;
				case 3:
					newSamplerControl.VacuumA += LocalSettings.Singleton.Sampler.Container3.PreciseStep;
					if (newSamplerControl.VacuumA > 1f) newSamplerControl.VacuumA = 1f;
					break;
				case 4:
					newSamplerControl.VacuumB += LocalSettings.Singleton.Sampler.Container4.PreciseStep;
					if (newSamplerControl.VacuumB > 1f) newSamplerControl.VacuumB = 1f;
					break;
			}
		}
		if (inputEvent.IsActionPressed("sampler_container_precise_down", allowEcho: false, exactMatch: true))
		{
			switch (LastMovedContainer)
			{
				case 1:
					newSamplerControl.ContainerDegrees0 -= LocalSettings.Singleton.Sampler.Container1.PreciseStep;
					if (newSamplerControl.ContainerDegrees0 < -1f) newSamplerControl.ContainerDegrees0 = -1f;
					break;
				case 2:
					newSamplerControl.VacuumSuction -= LocalSettings.Singleton.Sampler.Container2.PreciseStep;
					if (newSamplerControl.VacuumSuction < -1f) newSamplerControl.VacuumSuction = -1f;
					break;
				case 3:
					newSamplerControl.VacuumA -= LocalSettings.Singleton.Sampler.Container3.PreciseStep;
					if (newSamplerControl.VacuumA < -1f) newSamplerControl.VacuumA = -1f;
					break;
				case 4:
					newSamplerControl.VacuumB -= LocalSettings.Singleton.Sampler.Container4.PreciseStep;
					if (newSamplerControl.VacuumB < -1f) newSamplerControl.VacuumB = -1f;
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
