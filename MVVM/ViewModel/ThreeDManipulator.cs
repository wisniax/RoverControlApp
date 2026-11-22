using Godot;
using MQTTnet;
using RoverControlApp.Core;
using RoverControlApp.MVVM.Model;
using System;
using System.Text.Json;
using System.Threading.Tasks;

public partial class ThreeDManipulator : Node3D
{
	[Export] private Node3D _axis1 = null!;
	[Export] private Node3D _axis2 = null!;
	[Export] private Node3D _axis3 = null!;
	[Export] private Node3D _axis4 = null!;
	[Export] private Node3D _axis5 = null!;
	[Export] private Node3D _axis6 = null!;
	[Export] private Node3D _gripper = null!;

	public DateTime _lastUpdateTime = DateTime.MinValue;

	public DateTime[] manipulatorTimers = Array.Empty<DateTime>();

	public override void _EnterTree()
	{
		MqttNode.Singleton.MessageReceivedAsync += VescInfoChanged;
	}

	public override void _ExitTree()
	{
		MqttNode.Singleton.MessageReceivedAsync -= VescInfoChanged;
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
			case var id when id == HexStringToInt(LocalSettings.Singleton.CanIds.Manipulator.Axis1): // Manipulator Axis 1
				CallDeferred(nameof(UpdateManipulatorAxis), 0, velData.PrecisePos);
				manipulatorTimers[0] = DateTime.Now;
				break;
			case var id when id == HexStringToInt(LocalSettings.Singleton.CanIds.Manipulator.Axis2): // Manipulator Axis 2
				CallDeferred(nameof(UpdateManipulatorAxis), 1, velData.PrecisePos);
				manipulatorTimers[1] = DateTime.Now;
				break;
			case var id when id == HexStringToInt(LocalSettings.Singleton.CanIds.Manipulator.Axis3): // Manipulator Axis 3
				CallDeferred(nameof(UpdateManipulatorAxis), 2, velData.PrecisePos);
				manipulatorTimers[2] = DateTime.Now;
				break;
			case var id when id == HexStringToInt(LocalSettings.Singleton.CanIds.Manipulator.Axis4): // Manipulator Axis 4
				CallDeferred(nameof(UpdateManipulatorAxis), 3, velData.PrecisePos);
				manipulatorTimers[3] = DateTime.Now;
				break;
			case var id when id == HexStringToInt(LocalSettings.Singleton.CanIds.Manipulator.Axis5): // Manipulator Axis 5
				CallDeferred(nameof(UpdateManipulatorAxis), 4, velData.PrecisePos);
				manipulatorTimers[4] = DateTime.Now;
				break;
			case var id when id == HexStringToInt(LocalSettings.Singleton.CanIds.Manipulator.Axis6): // Manipulator Axis 6
				CallDeferred(nameof(UpdateManipulatorAxis), 5, velData.PrecisePos);
				manipulatorTimers[5] = DateTime.Now;
				break;
			case var id when id == HexStringToInt(LocalSettings.Singleton.CanIds.Manipulator.Gripper): // Manipulator Gripper
				CallDeferred(nameof(UpdateManipulatorAxis), 6, velData.PrecisePos);
				manipulatorTimers[6] = DateTime.Now;
				break;
			default:
				EventLogger.LogMessage("ThreeDVisualizer", EventLogger.LogLevel.Warning, $"Unhandled VESC ID: {vescID}");
				break;
		}

		await Task.Yield();
	}

	public int HexStringToInt(string hexString)
	{
		if (hexString.StartsWith("0x"))
		{
			hexString = hexString.Substring(2);
		}
		return Convert.ToInt32(hexString, 16);
	}

	public void UpdateManipulatorAxis(int axisNumber, double value)
	{
		_lastUpdateTime = DateTime.Now;
		CallDeferred(nameof(UpdateManipulatorVisibility), true);

		switch (axisNumber)
		{
			case 0:
				_axis1.RotationDegrees = new Vector3(0, 0, (float)value);
				break;
			case 1:
				_axis2.RotationDegrees = new Vector3((float)value, 0, 0);
				break;
			case 2:
				_axis3.RotationDegrees = new Vector3((float)value, 0, 0);
				break;
			case 3:
				_axis4.RotationDegrees = new Vector3(0, (float)value, 0);
				break;
			case 4:
				_axis5.RotationDegrees = new Vector3((float)value, 0, 0);
				break;
			case 5:
				_axis6.RotationDegrees = new Vector3(0, (float)value, 0);
				break;
			case 6:
				//_gripper.RotationDegrees = new Vector3(0, 0, (float)value);
				break;
			default:
				GD.PrintErr($"Invalid axis number: {axisNumber}");
				break;
		}
	}

	public void UpdateManipulatorVisibility(bool isVisible)
	{
		this.Visible = isVisible;
	}


	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		if ((DateTime.Now - _lastUpdateTime).TotalSeconds > LocalSettings.Singleton.Battery.ExpectedMessageInterval)
		{
			CallDeferred(nameof(UpdateManipulatorVisibility), false);
		}
	}

}
