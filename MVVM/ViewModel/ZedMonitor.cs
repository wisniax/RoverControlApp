using Godot;
using MQTTnet;
using MQTTnet.Internal;
using OpenCvSharp;
using RoverControlApp.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Text.Json;
using RoverControlApp.MVVM.ViewModel;

namespace RoverControlApp.MVVM.ViewModel;

public partial class ZedMonitor : Panel
{
	//[Export]
	double pitchDeg;
	//[Export]
	double rollDeg;

    //Sprites for gyro visualisation
    [Export]
	Sprite2D pitchVisualisation;
    [Export]
    Sprite2D rollVisualisation;

	//Labels for gyro display
	[Export]
	Label pitchDisplay;
    [Export]
	Label rollDisplay;

	//Fake quaterion values for testing

	[Export]
	double X;
	[Export]
	double Y;
	[Export]
	double Z;
	[Export]
	double W;

	double QuatX, QuatY, QuatZ, QuatW;

	//Events for gyro data
	public event Func<MqttClasses.ZedImuData?, Task>? GyroscopeChanged;

	MqttClasses.ZedImuData ?Gyroscope;
	string? msg;
	int connected = 0;

    public override void _Ready()
    {
        GyroscopeChanged += OnGyroscopeChanged;
    }

	public Task OnGyroscopeChanged(MqttClasses.ZedImuData? arg)
	{
        PullGyroscope();
        QuatW = Gyroscope.orientation.w;
        QuatX = Gyroscope.orientation.x;
        QuatY = Gyroscope.orientation.y;
        QuatZ = Gyroscope.orientation.z;
        GD.Print($"QuatW: {Gyroscope.orientation.w}, QuatX: {Gyroscope.orientation.x}, QuatY: {Gyroscope.orientation.y}, QuatZ: {Gyroscope.orientation.z}");
        Roll();
        Pitch();
        VisualisationUpdate();
        DisplayUpdate();
        return Task.CompletedTask;
    }

	//public override void _Process(double delta)
	//{
	//	//Main cycle of the program with the MQTT connection check and quaternion processing
	//	if (connected == 0)
	//	{
	//		ConnectionCheck();
	//	}
	//	else
	//	{
	//		PullGyroscope();
	//		QuatW = Gyroscope.orientation.w;
	//		QuatX = Gyroscope.orientation.x;
	//		QuatY = Gyroscope.orientation.y;
	//		QuatZ = Gyroscope.orientation.z;
	//		GD.Print($"QuatW: {Gyroscope.orientation.w}, QuatX: {Gyroscope.orientation.x}, QuatY: {Gyroscope.orientation.y}, QuatZ: {Gyroscope.orientation.z}");
	//		Roll();
	//		Pitch();
	//		VisualisationUpdate();
	//		DisplayUpdate();
	//	}



	//	//Manual quaternion input for testing
	//	//QuatW = W;
	//	//QuatX = X;
	//	//QuatY = Y;
	//	//QuatZ = Z;
	//	//Roll();
	//	//Pitch();
	//	//VisualisationUpdate();
	//	//DisplayUpdate();
	//}

	public void VisualisationUpdate()
	{
		pitchVisualisation.RotationDegrees = -(float)pitchDeg;
		rollVisualisation.RotationDegrees = (float)rollDeg;
	}

	public void DisplayUpdate()
	{
		pitchDisplay.Text = $"{Math.Round(-pitchDeg, 0)} deg";
        rollDisplay.Text = $"{Math.Round(rollDeg, 0)} deg";
    }

	public void PullGyroscope()
	{		
		Gyroscope = JsonSerializer.Deserialize<MqttClasses.ZedImuData>(msg);
		double[] Quat = {Gyroscope.orientation.w, Gyroscope.orientation.x, Gyroscope.orientation.y, Gyroscope.orientation.z };
        msg = MainViewModel.MqttClient?.GetReceivedMessageOnTopicAsString(MainViewModel.Settings?.Settings?.Mqtt.TopicZedImuData);
    }

	public void ConnectionCheck()
	{
        
        msg = MainViewModel.MqttClient?.GetReceivedMessageOnTopicAsString(MainViewModel.Settings?.Settings?.Mqtt.TopicZedImuData);
        if (msg != null)
		{
			connected = 1;
			GD.Print($"{MainViewModel.Settings?.Settings?.Mqtt.TopicZedImuData}");
        }
    }

	public double ConvertToDegrees(double radians)
	{
		return radians * (180.0 / (float)Math.PI);
	}

	public void Roll()
	{
        rollDeg = ConvertToDegrees(Math.Atan2(2 * (QuatW * QuatX + QuatY * QuatZ), 1 - 2 * (QuatX * QuatX + QuatY * QuatY)));
    }

	public void Pitch()
	{
        pitchDeg = ConvertToDegrees(Math.Asin(2 * (QuatW * QuatY - QuatZ * QuatX)));
	}

}
