using Godot;
using System;
using System.Text.Json;
using System.Threading.Tasks;
using MQTTnet.Protocol;
using RoverControlApp.Core;
using RoverControlApp.MVVM.Model;

public partial class SamplerControl : Panel
{
	[Export] private Label _drillStateLabel;
	[Export] private Label _platformStateLabel;
	
	private MqttClasses.SamplerControl _samplerControl = new();
	
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		#region GodotFixYourShit
		Button DrillUp = GetNode<Button>("DrillMenu/VBoxContainer/UP");
		DrillUp.Pressed += () => OnDrillAction(MqttClasses.SamplerDirection.Up);
		Button DrillStop = GetNode<Button>("DrillMenu/VBoxContainer/STOP");
		DrillStop.Pressed += () => OnDrillAction(MqttClasses.SamplerDirection.Stop);
		Button DrillDown = GetNode<Button>("DrillMenu/VBoxContainer/DOWN");
		DrillDown.Pressed += () => OnDrillAction(MqttClasses.SamplerDirection.Down);

		Button PlatformUp = GetNode<Button>("PlatformMenu/VBoxContainer/UP");
		PlatformUp.Pressed += () => OnPlatformAction(MqttClasses.SamplerDirection.Up);
		Button PlatformStop = GetNode<Button>("PlatformMenu/VBoxContainer/STOP");
		PlatformStop.Pressed += () => OnPlatformAction(MqttClasses.SamplerDirection.Stop);
		Button PlatformDown = GetNode<Button>("PlatformMenu/VBoxContainer/DOWN");
		PlatformDown.Pressed += () => OnPlatformAction(MqttClasses.SamplerDirection.Down);

		#endregion

	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}

	public void OnDrillAction(MqttClasses.SamplerDirection direction)
	{
		if (_samplerControl.DrillCommand == direction)
		{
			return;
		}
		_samplerControl.DrillCommand = direction;

		MqttClasses.SamplerDirection enumValue = (MqttClasses.SamplerDirection)direction;
		string enumName = Enum.GetName(typeof(MqttClasses.SamplerDirection), enumValue);
		_drillStateLabel.Text = $"State: {enumName}";

		SendSamplerMsg();
	}

	public void OnPlatformAction(MqttClasses.SamplerDirection direction)
	{
		if (_samplerControl.PlatformCommand == direction)
		{
			return;
		}
		_samplerControl.PlatformCommand = direction;

		MqttClasses.SamplerDirection enumValue = (MqttClasses.SamplerDirection)direction;
		string enumName = Enum.GetName(typeof(MqttClasses.SamplerDirection), enumValue);
		_platformStateLabel.Text = $"State: {enumName}";
		
		SendSamplerMsg();
	}


	public async Task SendSamplerMsg()
	{
		await MqttNode.Singleton.EnqueueMessageAsync("sampler", JsonSerializer.Serialize(_samplerControl), MqttQualityOfServiceLevel.ExactlyOnce);
	}
}
