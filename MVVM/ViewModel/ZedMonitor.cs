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
	int connected = 1;
	string msg;

	//Events for gyro data
	public event Func<MqttClasses.ZedImuData?, Task>? GyroscopeChanged;

	MqttClasses.ZedImuData ?Gyroscope;

    public override void _Ready()
    {
		
    }


    public Task OnGyroscopeChanged(string subTopic, MqttApplicationMessage? msg)
	{
		if (MainViewModel.Settings?.Settings?.Mqtt.TopicZedImuData is null || subTopic != MainViewModel.Settings?.Settings?.Mqtt.TopicZedImuData)
			return Task.CompletedTask;
        if (msg is null || msg.PayloadSegment.Count == 0)
        {
            MainViewModel.EventLogger?.LogMessage($"ZedMonitor Error: Empty payload");
            return Task.CompletedTask;
        }


        try
		{
            PullGyroscope(msg);
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
		catch (Exception e)
		{
			MainViewModel.EventLogger?.LogMessage($"ZedMonitor Error: {e.Message}");
			return Task.CompletedTask;
        }

       
    }

	

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

	public void PullGyroscope(MqttApplicationMessage? msg)
	{
		try
		{
			Gyroscope = JsonSerializer.Deserialize<MqttClasses.ZedImuData>(msg.ConvertPayloadToString());
			double[] Quat = { Gyroscope.orientation.w, Gyroscope.orientation.x, Gyroscope.orientation.y, Gyroscope.orientation.z };
			
			//Updating the msg is no longer necessary as it is now done in the OnGyroscopeChanged method
			//msg = MainViewModel.MqttClient?.GetReceivedMessageOnTopicAsString(MainViewModel.Settings?.Settings?.Mqtt.TopicZedImuData);
		}
		catch (Exception e)
		{
            GD.Print($"ZedMonitor Error (Something with json deserialization): {e.Message}");
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
