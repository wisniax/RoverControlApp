using Godot;
using System;
using System.Text.Json;
using System.Threading.Tasks;
using MQTTnet.Protocol;
using RoverControlApp.Core;
using RoverControlApp.Core.RoverControllerPresets;
using RoverControlApp.MVVM.Model;
using static RoverControlApp.Core.MqttClasses;

namespace RoverControlApp.Core.RoverControllerPresets.SamplerControllers;

public class SamplerController : IRoverSamplerController
{
	public MqttClasses.SamplerControl CalculateMoveVector(SamplerControl lastState)
	{
		float movement = Input.GetAxis("sampler_move_down", "sampler_move_up");
		if (Mathf.Abs(movement) < LocalSettings.Singleton.Joystick.Deadzone)
			movement = 0f;

		float drillSpeed = Input.GetAxis("sampler_drill_down", "sampler_drill_up");
		if (Mathf.Abs(drillSpeed) < LocalSettings.Singleton.Joystick.Deadzone)
			drillSpeed = 0f;

		SamplerControl samplerControl = new()
		{
			DrillMovement = Input.IsActionPressed("sampler_drill_movement") ? movement : 0f,
			PlatformMovement = Input.IsActionPressed("sampler_platform_movement") ? movement : 0f,
			DrillAction = Input.IsActionPressed("sampler_drill_enable") ? drillSpeed : 0f,
			ContainerDegrees0 = OperateContainer(
				LocalSettings.Singleton.Sampler.Container0,
				Input.IsActionJustPressed("sampler_container_0"),
				lastState.ContainerDegrees0
			),
			ContainerDegrees1 = OperateContainer(
				LocalSettings.Singleton.Sampler.Container1,
				Input.IsActionJustPressed("sampler_container_1"),
				lastState.ContainerDegrees1
			),
			ContainerDegrees2 = OperateContainer(
				LocalSettings.Singleton.Sampler.Container2,
				Input.IsActionJustPressed("sampler_container_2"),
				lastState.ContainerDegrees2
			),
			Timestamp = DateTimeOffset.Now.ToUnixTimeMilliseconds()
		};

		return samplerControl;
	}

	private static float OperateContainer(Settings.SamplerContainer samplerContainer, bool changeState, float lastState)
	{
		if (!changeState)
			return lastState;

		if (Mathf.IsEqualApprox(lastState, samplerContainer.OpenDegrees))
		{
			return samplerContainer.ClosedDegrees;
		}
		else
		{
			return samplerContainer.OpenDegrees;
		}
	}
}
