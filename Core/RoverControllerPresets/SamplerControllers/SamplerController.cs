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
			DrillAction = drillSpeed,
			ExtendContainer1 = Input.IsActionJustPressed("sampler_container_1") ? !lastState.ExtendContainer1 : lastState.ExtendContainer1,
			ExtendContainer2 = Input.IsActionJustPressed("sampler_container_2") ? !lastState.ExtendContainer2 : lastState.ExtendContainer2,
			OpenSkibidi = Input.IsActionJustPressed("sampler_open_skibidi") ? !lastState.OpenSkibidi : lastState.OpenSkibidi,

			Timestamp = DateTimeOffset.Now.ToUnixTimeMilliseconds()
		};

		return samplerControl;
	}

}
