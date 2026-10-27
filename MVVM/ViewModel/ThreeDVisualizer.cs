using Godot;
using MQTTnet;
using RoverControlApp.Core;
using RoverControlApp.Core.Settings;
using RoverControlApp.MVVM.Model;
using System;
using System.Text.Json;
using System.Threading.Tasks;
using static RoverControlApp.Core.MqttClasses;

public partial class ThreeDVisualizer : Node3D
{
	[Export] private Node3D _chassis = null!;

	[Export] private Node3D _suspensionFL = null!;
	[Export] private Node3D _suspensionFR = null!;
	[Export] private Node3D _suspensionRL = null!;
	[Export] private Node3D _suspensionRR = null!;

	[Export] private Node3D _wheelFL = null!;
	[Export] private Node3D _wheelFR = null!;
	[Export] private Node3D _wheelRL = null!;
	[Export] private Node3D _wheelRR = null!;

	[Export] private ThreeDBattery _battery0 = null!;
	[Export] private ThreeDBattery _battery1 = null!;
	[Export] private ThreeDBattery _battery2 = null!;

	[Export] private Node3D _hotswap0 = null!;
	[Export] private Node3D _hotswap1 = null!;
	[Export] private Node3D _hotswap2 = null!;



	public override void _EnterTree()
	{
		MqttNode.Singleton.MessageReceivedAsync += VescInfoChanged;
		//MqttNode.Singleton.MessageReceivedAsync += BatteryInfoChanged;
	}

	public async Task VescInfoChanged(string subTopic, MqttApplicationMessage? msg)
	{
		if (string.IsNullOrEmpty(LocalSettings.Singleton.Mqtt.TopicWheelFeedback) || subTopic != LocalSettings.Singleton.Mqtt.TopicWheelFeedback)
			return;
		if (msg is null || msg.PayloadSegment.Count == 0)
			return;

		string payload = System.Text.Encoding.UTF8.GetString(msg.PayloadSegment.ToArray());
		var velData = JsonSerializer.Deserialize<MqttClasses.WheelFeedback>(payload);
		if (velData is null) return;

		int vescID = velData.VescId;

		switch (vescID)
		{
			case var id when id == HexStringToInt(LocalSettings.Singleton.WheelData.FrontLeftTurn): // Front Left turn
				CallDeferred(nameof(UpdateWheelRotation), _wheelFL, velData.PrecisePos);
				break;
			case var id when id == HexStringToInt(LocalSettings.Singleton.WheelData.FrontRightTurn): // Front Right turn
				CallDeferred(nameof(UpdateWheelRotation), _wheelFR, velData.PrecisePos);
				break;
			case var id when id == HexStringToInt(LocalSettings.Singleton.WheelData.BackLeftTurn): // Rear Left turn
				CallDeferred(nameof(UpdateWheelRotation), _wheelRL, velData.PrecisePos);
				break;
			case var id when id == HexStringToInt(LocalSettings.Singleton.WheelData.BackRightTurn): // Rear Right turn
				CallDeferred(nameof(UpdateWheelRotation), _wheelRR, velData.PrecisePos);
				break;
			default:
				EventLogger.LogMessage("ThreeDVisualizer", EventLogger.LogLevel.Warning, $"Unhandled VESC ID: {vescID}");
				break;
		}

		await Task.Yield();
	}

	int HexStringToInt(string hexString)
	{
		if (hexString.StartsWith("0x"))
		{
			hexString = hexString.Substring(2);
		}
		return Convert.ToInt32(hexString, 16);
	}

	void UpdateWheelRotation(Node3D wheelNode, double precisePos)
	{
		wheelNode.RotationDegrees = new Vector3(0, -(float)precisePos, 0);
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
}
