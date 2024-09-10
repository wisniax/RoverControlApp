using Godot;
using System;
using System.Text.Json;
using System.Threading.Tasks;
using MQTTnet.Protocol;
using RoverControlApp.Core;
using RoverControlApp.MVVM.Model;

public partial class SamplerControl : Panel
{
	private MqttClasses.SamplerControl _samplerControl = new();
	public MqttClasses.SamplerControl DoSamplerControl()
	{
		float movement = Input.GetAxis("sampler_move_down", "sampler_move_up");

		_samplerControl.PlatformMovement = Input.IsActionPressed("sampler_platform_movement") ? movement : 0f;
		_samplerControl.DrillMovement = Input.IsActionPressed("sampler_drill_movement") ? movement : 0f;
		_samplerControl.DrillAction = Input.GetAxis("sampler_drill_up", "sampler_drill_down");
		_samplerControl.ExtendContainer1 = Input.IsActionJustPressed("sampler_container_1")
			? !_samplerControl.ExtendContainer1 : _samplerControl.ExtendContainer1; 
		_samplerControl.ExtendContainer2 = Input.IsActionJustPressed("sampler_container_2")
			? !_samplerControl.ExtendContainer2 : _samplerControl.ExtendContainer2;

		_samplerControl.Timestamp = DateTimeOffset.Now.ToUnixTimeMilliseconds();

		return _samplerControl;
	}

	public async Task SendSamplerMsg()
	{
		await MqttNode.Singleton.EnqueueMessageAsync(LocalSettings.Singleton.Mqtt.TopicSampler, JsonSerializer.Serialize(_samplerControl), MqttQualityOfServiceLevel.ExactlyOnce);
	}
}
