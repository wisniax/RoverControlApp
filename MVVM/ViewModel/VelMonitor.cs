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
	[Export] private Label[] _delayLabel;
	DateTime[] _lastUpdate = new DateTime[8];
	TimeSpan[] _lastDelay = new TimeSpan[8];

	[Export] private Sprite2D[] _wheelSprites = new Sprite2D[4];
	[Export] private VSlider[] _wheelSlider = new VSlider[4];

	[Export] private Sprite2D[] _ghostSprites = new Sprite2D[4];

	int[] driveMotorID = new int[4];
	int[] rotationMotorID = new int[4];
	
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
		foreach (var i in _lastUpdate)
		{
			_lastUpdate[Array.IndexOf(_lastUpdate, i)] = DateTime.Now - TimeSpan.FromSeconds(10);
		}

		LocalSettings.Singleton.Connect(LocalSettings.SignalName.PropagatedPropertyChanged, Callable.From<StringName, StringName, Variant, Variant>(OnSettingsPropertyChanged));
		MqttNode.Singleton.MessageReceivedAsync += VelInfoChanged;

	}
	public override void _ExitTree()
	{
		LocalSettings.Singleton.Disconnect(LocalSettings.SignalName.PropagatedPropertyChanged, Callable.From<StringName, StringName, Variant, Variant>(OnSettingsPropertyChanged));
		MqttNode.Singleton.MessageReceivedAsync -= VelInfoChanged;
	}

	public override void _Process(double delta)
	{
		foreach (var i in _lastUpdate)
		{
			TimeSpan delay = DateTime.Now - i;

			if (delay > TimeSpan.FromSeconds(5))
			{
				if (Array.IndexOf(_lastUpdate, i) <= 3)
				{
					_driveLabel[Array.IndexOf(_lastUpdate, i)].SetText($"Drive:\n" +
																	   $"RPM: ??? rpm\n" +
																	   $"Current: ??? A");	
				}
				else
				{
					_rotationLabel[Array.IndexOf(_lastUpdate, i)-4].SetText($"Rotation:\n" +
																		    $"RPM: ??? rpm\n" +
																			$"Current: ??? A");
				}
				_delayLabel[Array.IndexOf(_lastUpdate, i)].SetText($"Last update: ??? s");
			}
			else
			{
				if (delay > _lastDelay[Array.IndexOf(_lastUpdate, i)])
				{
					_delayLabel[Array.IndexOf(_lastUpdate, i)].SetText($"Last update: {delay.Milliseconds:F0} ms");
					_lastDelay[Array.IndexOf(_lastUpdate, i)] = delay;
				}
				else
				{
					_delayLabel[Array.IndexOf(_lastUpdate, i)].SetText($"Last update: {_lastDelay[Array.IndexOf(_lastUpdate, i)].Milliseconds:F0} ms");
				}
			}

		}
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

		for (int i = 0; i < 8; i++)
		{
			if (i <= 3)
			{
				if (driveMotorID[i] == velData.VescId)
				{
					UpdateDriveMotorInfoHandler(i, velData);
					_lastDelay[i] = DateTime.Now - _lastUpdate[i];
					_lastUpdate[i] = DateTime.Now;

					break;
				}
				continue;
			}
			else
			{
				if (rotationMotorID[i - 4] == velData.VescId)
				{
					UpdateRotationMotorInfoHandler(i - 4, velData);
					_lastUpdate[i] = DateTime.Now;
					break;
				}
				continue;
			}
		}
	}

	void UpdateDriveMotorInfoHandler(int motor, MqttClasses.WheelFeedback data)
	{
		CallDeferred("UpdateDriveMotorInfo", motor, (int)data.ERPM, (int)data.Current);
	}

	void UpdateDriveMotorInfo(int motor, int erpm, int current)
	{
		_driveLabel[motor].SetText($"Drive:\n" +
						 $"RPM: {erpm} rpm\n" +
						 $"Current: {current} A");
		_wheelSlider[motor].Value = (float)erpm;
		_wheelSlider[motor].MinValue = - LocalSettings.Singleton.WheelData.MaxRPM;
		_wheelSlider[motor].MaxValue = LocalSettings.Singleton.WheelData.MaxRPM;
	}

	void UpdateRotationMotorInfoHandler(int motor, MqttClasses.WheelFeedback data)
	{
		CallDeferred("UpdateRotationMotorInfo", motor, (int)data.ERPM, (int)data.Current, (int)data.PrecisePos, (int)data.PidPos);
	}

	void UpdateRotationMotorInfo(int motor, int erpm, int current, int precisePos, int pidPos)
	{
		_rotationLabel[motor].SetText($"Rotation:\n" +
						 $"RPM: {erpm} rpm\n" +
						 $"Current: {current:F0} A");
		_wheelSprites[motor].RotationDegrees = (float)precisePos + ((motor == 0 || motor == 3) ? 90f : -90f);
		_ghostSprites[motor].RotationDegrees = (float)pidPos + ((motor == 0 || motor == 3) ? 90f : -90f);
	}

	private int StringToHexInt(string hexString)
	{
		int value = Convert.ToInt32(hexString.Replace("0x", ""), 16);
		return value;
	}
}