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
    [Export]
	Panel errorDisplay;

    bool error = false;

    public event Func<MqttClasses.ZedImuData?, Task>? GyroscopeChanged;

	MqttClasses.ZedImuData ?Gyroscope;

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
			Quaternion Quat = PullGyroscope(msg);
			AngleUpdate(Quat);

            return Task.CompletedTask;
        }
		catch (Exception e)
		{
			MainViewModel.EventLogger?.LogMessage($"ZedMonitor Error: {e.Message}");
			return Task.CompletedTask;
        }  
    }
    public Quaternion PullGyroscope(MqttApplicationMessage? msg)
    {
        try
        {
            Gyroscope = JsonSerializer.Deserialize<MqttClasses.ZedImuData>(msg.ConvertPayloadToString());
            Quaternion Quat = new Quaternion((float)Gyroscope.orientation.x, (float)Gyroscope.orientation.y, (float)Gyroscope.orientation.z, (float)Gyroscope.orientation.w);
            error = false;
            return Quat;
        }
        catch (Exception e)
        {
            GD.Print($"ZedMonitor Error (Something is wrong with json/deserialization): {e.Message}");
            error = true;
            return new Quaternion((float)Gyroscope.orientation.x, (float)Gyroscope.orientation.y, (float)Gyroscope.orientation.z, (float)Gyroscope.orientation.w);
        }
    }
    public void AngleUpdate(Quaternion Quat)
    {
        double rollDeg = (180.0 / Math.PI) * Math.Atan2(2 * (Quat.W * Quat.X + Quat.Y * Quat.Z), 1 - 2 * (Quat.X * Quat.X + Quat.Y * Quat.Y));
        double pitchDeg = (180.0 / Math.PI) * Math.Asin(2 * (Quat.W * Quat.Y - Quat.Z * Quat.X));
        CallDeferred("DisplayUpdate", rollDeg, pitchDeg);
    }
    private void DisplayUpdate(double rollDeg, double pitchDeg)
	{
        if(error == true)
        {
            errorDisplay.Visible = true;
            return;
        }
        errorDisplay.Visible = false;
        pitchVisualisation.RotationDegrees = -(float)pitchDeg;
        rollVisualisation.RotationDegrees = (float)rollDeg;
        pitchDisplay.Text = $"{Math.Round(-pitchDeg, 0)} deg";
		rollDisplay.Text = $"{Math.Round(rollDeg, 0)} deg";
	}
}
