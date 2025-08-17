using Godot;
using MQTTnet;
using RoverControlApp.Core;
using RoverControlApp.MVVM.Model;
using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace RoverControlApp.MVVM.ViewModel;

public partial class ZedMonitor : Panel
{
	//Sprites for gyro visualisation
	[Export]
	Sprite2D pitchVisualisation = null!;
	[Export]
	Sprite2D rollVisualisation = null!;

	//Labels for gyro display
	[Export]
	Label pitchDisplay = null!;
	[Export]
	Label rollDisplay = null!;
	[Export]
	Panel errorDisplay = null!;

	[Export]
	Timer timer = null!;
	[Export]
	Label timerDisplay = null!;

	//Every {timerInterval} seconds, timer will trigger _on_timer_timeout and timeSLU will be updated. timeSLU will later be displayed in timerDisplay. Can easily be changed in Godot editor.
	[Export]
	int timerInterval = 1;

	//If time between messages is greater than errorTime, error message is displayed. Can easily be changed in Godot editor.
	[Export]
	int errorTime = 5;


	bool error = false;
	//time since last update
	int timeSLU = 0;

	MqttClasses.ZedImuData? Gyroscope;

	public override void _EnterTree()
	{
		MqttNode.Singleton.MessageReceivedAsync += OnGyroscopeChanged;
	}

	public override void _ExitTree()
	{
		MqttNode.Singleton.MessageReceivedAsync -= OnGyroscopeChanged;
	}

	public Task OnGyroscopeChanged(string subTopic, MqttApplicationMessage? msg)
	{
		if (string.IsNullOrEmpty(LocalSettings.Singleton.Mqtt.TopicZedImuData) || subTopic != LocalSettings.Singleton.Mqtt.TopicZedImuData)
			return Task.CompletedTask;
		if (msg is null || msg.PayloadSegment.Count == 0)
		{
			EventLogger.LogMessage("ZedMonitor", EventLogger.LogLevel.Error, "Empty payload");
			return Task.CompletedTask;
		}

		try
		{
			Quaternion Quat = PullGyroscope(msg);
			AngleUpdate(Quat);
			timeSLU = 0;
			CallDeferred("TimeUpdate", timeSLU);
			return Task.CompletedTask;
		}
		catch (Exception e)
		{
			EventLogger.LogMessage("ZedMonitor", EventLogger.LogLevel.Error, $"{e.Message}");
			return Task.CompletedTask;
		}
	}

	public void _on_timer_timeout()
	{
		timeSLU += timerInterval;

		if (timeSLU == errorTime)
			EventLogger.LogMessage("ZedMonitor", EventLogger.LogLevel.Error, $"gyro data >{errorTime} seconds old.");
		if (timeSLU >= errorTime)
			errorDisplay.Visible = true;
		TimeUpdate(timeSLU);
	}

	public Quaternion PullGyroscope(MqttApplicationMessage? msg)
	{
		try
		{
			Gyroscope = JsonSerializer.Deserialize<MqttClasses.ZedImuData>(msg.ConvertPayloadToString());
			if(Gyroscope is null) 
				throw new InvalidDataException("Invalid ZedImuData payload.");
			Quaternion Quat = new Quaternion((float)Gyroscope.orientation.x, (float)Gyroscope.orientation.y, (float)Gyroscope.orientation.z, (float)Gyroscope.orientation.w);
			error = false;

			return Quat;
		}
		catch (Exception e)
		{
			EventLogger.LogMessage("ZedMonitor", EventLogger.LogLevel.Error, $"Something is wrong with json/deserialization: {e.Message}");
			error = true;
			return Quaternion.Identity;
		}
	}
	public void AngleUpdate(Quaternion Quat)
	{
		var eulerZYX = Quat.Normalized().GetEuler(EulerOrder.Zyx);
		double rollDeg = Mathf.RadToDeg(eulerZYX.X);
		double pitchDeg = Mathf.RadToDeg(eulerZYX.Y);
		CallDeferred("DisplayUpdate", rollDeg, pitchDeg);
	}
	private void DisplayUpdate(double rollDeg, double pitchDeg)
	{
		if (error)
		{
			errorDisplay.Visible = true;
			return;
		}
		errorDisplay.Visible = false;
		pitchVisualisation.RotationDegrees = -(float)pitchDeg;
		rollVisualisation.RotationDegrees = (float)rollDeg;
		pitchDisplay.Text = $"{Math.Round(-pitchDeg, 0)} deg";
		rollDisplay.Text = $"{Math.Round(rollDeg, 0)} deg";
		timer.Start(timerInterval);
	}

	private void TimeUpdate(float time)
	{
		timerDisplay.Text = $"Data is >{time} seconds old.";
	}
}