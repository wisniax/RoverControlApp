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
		"sampler_drilling_altmode",
		"sampler_container_0",
		"sampler_container_1",
		"sampler_container_2",
		"sampler_container_3",
		"sampler_container_4",
		"sampler_container_precise_up",
		"sampler_container_precise_down",
	];

	public static int LastMovedContainer { get; set; } = -1;
	public SamplerControl CalculateMoveVector(in InputEvent inputEvent, in SamplerControl lastState)
	{
		float movement = Input.GetAxis("sampler_move_down", "sampler_move_up");
		if (Mathf.Abs(movement) < LocalSettings.Singleton.Joystick.MinimalInput)
			movement = 0f;

		float drillSpeed = Input.GetAxis("sampler_drill_down", "sampler_drill_up");
		//if (Mathf.Abs(drillSpeed) < LocalSettings.Singleton.Joystick.MinimalInput)
		//	drillSpeed = 0f; //No deadzone for trigger

		SamplerControl newSamplerControl;
		if (Input.IsActionPressed("sampler_drilling_altmode"))
		{
			newSamplerControl = new()
			{
				DrillMovement = drillSpeed,
				PlatformMovement = 0f,
				DrillAction = drillSpeed,
				ContainerDegrees0 = lastState.ContainerDegrees0,
				ContainerDegrees1 = lastState.ContainerDegrees1,
				ContainerDegrees2 = lastState.ContainerDegrees2,
				ContainerDegrees3 = lastState.ContainerDegrees3,
				ContainerDegrees4 = lastState.ContainerDegrees4,
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
				ContainerDegrees1 = lastState.ContainerDegrees1,
				ContainerDegrees2 = lastState.ContainerDegrees2,
				ContainerDegrees3 = lastState.ContainerDegrees3,
				ContainerDegrees4 = lastState.ContainerDegrees4,
				Timestamp = DateTimeOffset.Now.ToUnixTimeMilliseconds(),
			};
		};

		if (inputEvent.IsActionPressed("sampler_container_0", allowEcho: false, exactMatch: true))
		{
			LastMovedContainer = 0;
			newSamplerControl.ContainerDegrees0 = OperateContainer(
				LocalSettings.Singleton.Sampler.Container0,
				true,
				newSamplerControl.ContainerDegrees0
			);
		}
		if (inputEvent.IsActionPressed("sampler_container_1", allowEcho: false, exactMatch: true))
		{
			LastMovedContainer = 1;
			newSamplerControl.ContainerDegrees1 = OperateContainer(
				LocalSettings.Singleton.Sampler.Container1,
				true,
				newSamplerControl.ContainerDegrees1
			);
		}
		if (inputEvent.IsActionPressed("sampler_container_2", allowEcho: false, exactMatch: true))
		{
			LastMovedContainer = 2;
			newSamplerControl.ContainerDegrees2 = OperateContainer(
				LocalSettings.Singleton.Sampler.Container2,
				true,
				newSamplerControl.ContainerDegrees2
			);
		}
		if (inputEvent.IsActionPressed("sampler_container_3", allowEcho: false, exactMatch: true))
		{
			LastMovedContainer = 3;
			newSamplerControl.ContainerDegrees3 = OperateContainer(
				LocalSettings.Singleton.Sampler.Container3,
				true,
				newSamplerControl.ContainerDegrees3
			);
		}
		if (inputEvent.IsActionPressed("sampler_container_4", allowEcho: false, exactMatch: true))
		{
			LastMovedContainer = 4;
			newSamplerControl.ContainerDegrees4 = OperateContainer(
				LocalSettings.Singleton.Sampler.Container4,
				true,
				newSamplerControl.ContainerDegrees4
			);
		}

		if (inputEvent.IsActionPressed("sampler_container_precise_up", allowEcho: false, exactMatch: true))
		{
			switch (LastMovedContainer)
			{
				case 0:
					newSamplerControl.ContainerDegrees0 += LocalSettings.Singleton.Sampler.Container0.PreciseStep;
					if (newSamplerControl.ContainerDegrees0 > 1000000f) newSamplerControl.ContainerDegrees0 = 1000000f;
					break;
				case 1:
					newSamplerControl.ContainerDegrees1 += LocalSettings.Singleton.Sampler.Container1.PreciseStep;
					if (newSamplerControl.ContainerDegrees1 > 1000000f) newSamplerControl.ContainerDegrees1 = 1000000f;
					break;
				case 2:
					newSamplerControl.ContainerDegrees2 += LocalSettings.Singleton.Sampler.Container2.PreciseStep;
					if (newSamplerControl.ContainerDegrees2 > 1000000f) newSamplerControl.ContainerDegrees2 = 1000000f;
					break;
				case 3:
					newSamplerControl.ContainerDegrees3 += LocalSettings.Singleton.Sampler.Container3.PreciseStep;
					if (newSamplerControl.ContainerDegrees3 > 1000000f) newSamplerControl.ContainerDegrees3 = 1000000f;
					break;
				case 4:
					newSamplerControl.ContainerDegrees4 += LocalSettings.Singleton.Sampler.Container4.PreciseStep;
					if (newSamplerControl.ContainerDegrees4 > 1000000f) newSamplerControl.ContainerDegrees4 = 1000000f;
					break;
			}
		}
		if (inputEvent.IsActionPressed("sampler_container_precise_down", allowEcho: false, exactMatch: true))
		{
			switch (LastMovedContainer)
			{
				case 0:
					newSamplerControl.ContainerDegrees0 -= LocalSettings.Singleton.Sampler.Container0.PreciseStep;
					if (newSamplerControl.ContainerDegrees0 < -1f) newSamplerControl.ContainerDegrees0 = -1f;
					break;
				case 1:
					newSamplerControl.ContainerDegrees1 -= LocalSettings.Singleton.Sampler.Container1.PreciseStep;
					if (newSamplerControl.ContainerDegrees1 < -1f) newSamplerControl.ContainerDegrees1 = -1f;
					break;
				case 2:
					newSamplerControl.ContainerDegrees2 -= LocalSettings.Singleton.Sampler.Container2.PreciseStep;
					if (newSamplerControl.ContainerDegrees2 < -1f) newSamplerControl.ContainerDegrees2 = -1f;
					break;
				case 3:
					newSamplerControl.ContainerDegrees3 -= LocalSettings.Singleton.Sampler.Container3.PreciseStep;
					if (newSamplerControl.ContainerDegrees3 < -1f) newSamplerControl.ContainerDegrees3 = -1f;
					break;
				case 4:
					newSamplerControl.ContainerDegrees4 -= LocalSettings.Singleton.Sampler.Container4.PreciseStep;
					if (newSamplerControl.ContainerDegrees4 < -1f) newSamplerControl.ContainerDegrees4 = -1f;
					break;
			}
		}

		return newSamplerControl;
	}

	private static float OperateContainer(Settings.SamplerContainer samplerContainer, bool changeState, float lastState)
	{
		if (!changeState)
			return lastState;

		if (samplerContainer.Position0 == samplerContainer.Position1)
		{
			return Switch2(samplerContainer, lastState, samplerContainer.Position1, samplerContainer.Position2);
		}
		if (samplerContainer.Position1 == samplerContainer.Position2)
		{
			return Switch2(samplerContainer, lastState, samplerContainer.Position0, samplerContainer.Position1);
		}
		if (samplerContainer.Position0 == samplerContainer.Position2)
		{
			return Switch2(samplerContainer, lastState, samplerContainer.Position0, samplerContainer.Position1);
		}


		return Switch3(samplerContainer, lastState);
	}

	private static float Switch2(Settings.SamplerContainer samplerContainer, float lastState, float pos1, float pos2)
	{
		float deltaPos1 = Mathf.Abs(lastState - pos1);
		float deltaPos2 = Mathf.Abs(lastState - pos2);

		if (deltaPos1 < deltaPos2)
			return pos2;
		else
			return pos1;
	}

	private static float Switch3(Settings.SamplerContainer samplerContainer, float lastState)
	{
		float deltaPos0 = Mathf.Abs(lastState - samplerContainer.Position0);
		float deltaPos1 = Mathf.Abs(lastState - samplerContainer.Position1);
		float deltaPos2 = Mathf.Abs(lastState - samplerContainer.Position2);

		if (deltaPos0 == deltaPos1 && deltaPos0 < deltaPos2)
			return samplerContainer.Position1;
		if (deltaPos1 == deltaPos2 && deltaPos1 < deltaPos0)
			return samplerContainer.Position2;
		if (deltaPos0 == deltaPos2 && deltaPos0 < deltaPos1)
			return samplerContainer.Position0;

		if (deltaPos0 < deltaPos1 && deltaPos0 < deltaPos2)
			return samplerContainer.Position1;
		if (deltaPos1 < deltaPos0 && deltaPos1 < deltaPos2)
			return samplerContainer.Position2;
		if (deltaPos2 < deltaPos0 && deltaPos2 < deltaPos1)
			return samplerContainer.Position0;

		else
			return samplerContainer.Position0;
	}

	public Dictionary<string, Godot.Collections.Array<InputEvent>> GetInputActions() =>
		IActionAwareController.FetchAllActionEvents(_usedActions);

	public string GetInputActionsAdditionalNote() =>
	"""	
	Drilling:
	Press and hold 'X' to keep drill enabled. Press triggers to control drill speed and direction.

	Platform movement:
	Press and hold 'Y' to keep platform movement enabled. Move left joystick up and down to control speed and direction.

	Drill movement:
	Press and hold 'A' to keep drill movement enabled. Move left joystick up and down to control speed and direction.

	Drilling alt mode:
	Press and hold left joystick button to map both drilling and drill movement to triggers.

	Container control:
	Press 'B', 'LB', 'RB', 'L-Dpad', 'R-Dpad' to move respective container to next position.
	(settable in Sampler->ContainerX->PositionY. If all 3 positions are different 1->2->3, if only 2 differ it will switch between them)

	Container precise adjustment:
	Press 'Up-Dpad' or 'Down-Dpad' to precisely adjust last moved container position.
	(Step settable in settings, "next position" will be "next" relative to one closest to current)
	""";
}
