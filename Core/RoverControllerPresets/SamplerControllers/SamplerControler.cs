using Godot;
using System;
using System.Text.Json;
using System.Threading.Tasks;
using MQTTnet.Protocol;
using RoverControlApp.Core;
using RoverControlApp.MVVM.Model;
using static RoverControlApp.Core.MqttClasses;

public partial class SamplerControler : Panel
{
	public MqttClasses.SamplerControl CalculateMoveVector(SamplerControl oldSamplerControl)
	{
		float velocity = Input.GetAxis("sampler_move_down", "sampler_move_up");
		if (Mathf.Abs(velocity) < LocalSettings.Singleton.Joystick.Deadzone)
			velocity = 0f;


		SamplerControl samplerControl = new()
		{
			DrillMovement = Input.IsActionPressed("sampler_drill_movement") ? velocity : 0f,
			PlatformMovement = Input.IsActionPressed("sampler_platform_movement") ? velocity : 0f,
			
			DrillAction = Input.GetAxis("sampler_drill_up", "sampler_drill_down"),
			
			ExtendContainer1 = Input.IsActionJustPressed("sampler_container_1") ? !oldSamplerControl.ExtendContainer1 : oldSamplerControl.ExtendContainer1,
			ExtendContainer2 = Input.IsActionJustPressed("sampler_container_2") ? !oldSamplerControl.ExtendContainer2 : oldSamplerControl.ExtendContainer2,

			Timestamp = DateTimeOffset.Now.ToUnixTimeMilliseconds()
		};




		return samplerControl;
	}

	public async Task SendSamplerMsg()
	{
		//await MqttNode.Singleton.EnqueueMessageAsync(LocalSettings.Singleton.Mqtt.TopicSamplerControl, JsonSerializer.Serialize(samplerControl), MqttQualityOfServiceLevel.ExactlyOnce);
	}
}
