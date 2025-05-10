using Godot;
using RoverControlApp.Core;
using RoverControlApp.Core.Settings;
using RoverControlApp.MVVM.Model;
using MQTTnet;
using System.Text.Json;
using System.Threading.Tasks;
using System.Text;
using System;


namespace RoverControlApp.MVVM.ViewModel;
public partial class VelMonitor : Panel
{
	[Export] private Label _flLabel;
	[Export] private Label _frLabel;
	[Export] private Label _brLabel;
	[Export] private Label _blLabel;

	[Export] private Label[] _driveLabel;
	[Export] private Label[] _rotationLabel;

	[Export] private Sprite2D[] _wheelSprites;
	[Export] private VSlider[] _wheelSlider;

	[Export] private Sprite2D[] _ghostSprites;

	int[] driveMotorID = new int[4];
	int[] rotationMotorID = new int[4];
	int temp = 3;

	enum Pos
	{
		FrontLeft = 0,
		FrontRight = 1,
		BackRight = 2,
		BackLeft = 3
	}

	public override void _EnterTree()
	{
		UpdateCanIDLabels();
		LocalSettings.Singleton.Connect(LocalSettings.SignalName.PropagatedPropertyChanged, Callable.From<StringName, StringName, Variant, Variant>(OnSettingsPropertyChanged));

		MqttNode.Singleton.MessageReceivedAsync += VelInfoChanged;

	}
	public override void _ExitTree()
	{
		LocalSettings.Singleton.Disconnect(LocalSettings.SignalName.PropagatedPropertyChanged, Callable.From<StringName, StringName, Variant, Variant>(OnSettingsPropertyChanged));
		MqttNode.Singleton.MessageReceivedAsync -= VelInfoChanged;
	}

	void OnSettingsPropertyChanged(StringName category, StringName name, Variant oldValue, Variant newValue)
	{
		if (category != nameof(WheelData)) return;
		UpdateCanIDLabels();
	}

	private void UpdateCanIDLabels()
	{
		_flLabel.SetText($"D: {LocalSettings.Singleton.WheelData.FrontLeftDrive}\n" +
						 $"R: {LocalSettings.Singleton.WheelData.FrontLeftTurn}");
		_frLabel.SetText($"D: {LocalSettings.Singleton.WheelData.FrontRightDrive}\n" +
						 $"R: {LocalSettings.Singleton.WheelData.FrontRightTurn}");
		_blLabel.SetText($"D: {LocalSettings.Singleton.WheelData.BackLeftDrive}\n" +
						 $"R: {LocalSettings.Singleton.WheelData.BackLeftTurn}");
		_brLabel.SetText($"D: {LocalSettings.Singleton.WheelData.BackRightDrive}\n" +
						 $"R: {LocalSettings.Singleton.WheelData.BackRightTurn}");

		driveMotorID[(int)Pos.FrontLeft] = StringToHexInt(LocalSettings.Singleton.WheelData.FrontLeftDrive);
		driveMotorID[(int)Pos.FrontRight] = StringToHexInt(LocalSettings.Singleton.WheelData.FrontRightDrive);
		driveMotorID[(int)Pos.BackRight] = StringToHexInt(LocalSettings.Singleton.WheelData.BackRightDrive);
		driveMotorID[(int)Pos.BackLeft] = StringToHexInt(LocalSettings.Singleton.WheelData.BackLeftDrive);

		rotationMotorID[(int)Pos.FrontLeft] = StringToHexInt(LocalSettings.Singleton.WheelData.FrontLeftTurn);
		rotationMotorID[(int)Pos.FrontRight] = StringToHexInt(LocalSettings.Singleton.WheelData.FrontRightTurn);
		rotationMotorID[(int)Pos.BackRight] = StringToHexInt(LocalSettings.Singleton.WheelData.BackRightTurn);
		rotationMotorID[(int)Pos.BackLeft] = StringToHexInt(LocalSettings.Singleton.WheelData.BackLeftTurn);
	}

	private async Task VelInfoChanged(string subTopic, MqttApplicationMessage? msg)
	{
		if (string.IsNullOrEmpty(LocalSettings.Singleton.Mqtt.TopicWheelFeedback) || subTopic != LocalSettings.Singleton.Mqtt.TopicWheelFeedback)
			return;
		if (msg is null || msg.PayloadSegment.Count == 0)
			return;

		string payload = System.Text.Encoding.UTF8.GetString(msg.PayloadSegment.ToArray());
		var velData = JsonSerializer.Deserialize<MqttClasses.WheelFeedback>(payload);
		if (velData is null) return;

		int vescID = velData.VescId;

		for (int i = 0; i < 7; i++)
		{
			if (i <= 3)
			{
				if (driveMotorID[i] == velData.VescId)
				{
					UpdateDriveMotorInfo(i, velData);
				}
				continue;
			}
			else
			{
				if (rotationMotorID[i - 4] == velData.VescId)
				{
					UpdateRotationMotorInfo(i - 4, velData);
				}
				continue;
			}
		}
	}

	void UpdateDriveMotorInfo(int motor, MqttClasses.WheelFeedback data)
	{
		_driveLabel[motor].SetText($"Drive:\n" +
						 $"RPM: {data.ERPM} rpm\n" +
						 $"Current: {data.Current:F0} A");
		_wheelSlider[motor].Value = (float)data.ERPM;
	}

	void UpdateRotationMotorInfo(int motor, MqttClasses.WheelFeedback data)
	{
		_rotationLabel[motor].SetText($"Rotation:\n" +
						 $"RPM: {data.ERPM} rpm\n" +
						 $"Current: {data.Current:F0} A");
		_wheelSprites[motor].RotationDegrees = (float)data.PrecisePos + ((motor == 0 || motor == 3) ? 90f : -90f);
		_ghostSprites[motor].RotationDegrees = (float)data.PidPos + ((motor == 0 || motor == 3) ? 90f : -90f);
	}

	private int StringToHexInt(string hexString)
	{
		int value = Convert.ToInt32(hexString.Replace("0x", ""), 16);
		return value;
	}
}