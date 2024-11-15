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
			ContainerDegrees0 = Input.IsActionJustPressed("sampler_container_0") ? FloatSwapper(lastState.ContainerDegrees0,
					LocalSettings.Singleton.Sampler.ContainerDegreesOpened0, LocalSettings.Singleton.Sampler.ContainerDegreesClosed0)
							: lastState.ContainerDegrees0,
			ContainerDegrees1 = Input.IsActionJustPressed("sampler_container_1") ? FloatSwapper(lastState.ContainerDegrees1, 
				LocalSettings.Singleton.Sampler.ContainerDegreesOpened1, LocalSettings.Singleton.Sampler.ContainerDegreesClosed1) 
							: lastState.ContainerDegrees1,
			ContainerDegrees2 = Input.IsActionJustPressed("sampler_container_2") ? FloatSwapper(lastState.ContainerDegrees2,
					LocalSettings.Singleton.Sampler.ContainerDegreesOpened2, LocalSettings.Singleton.Sampler.ContainerDegreesClosed2)
							: lastState.ContainerDegrees2,

			Timestamp = DateTimeOffset.Now.ToUnixTimeMilliseconds()
		};

		return samplerControl;
	}

	private float FloatSwapper(float lastState, float openState, float closeState)
	{
		return 0f;
	}
}
