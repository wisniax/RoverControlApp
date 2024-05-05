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

    [Export]
    Timer timer;
    [Export]
    Label timerDisplay;

    //Every {timerInterval} seconds, timer will trigger _on_timer_timeout and timeSLU will be updated. timeSLU will later be displayed in timerDisplay. Can easily be changed in Godot editor.
    [Export]
    int timerInterval = 1;
    
    //If time between messages is greater than errorTime, error message is displayed. Can easily be changed in Godot editor.
    [Export]
    int errorTime = 5;


    bool error = false;
    //time since last update
    int timeSLU = 0;   

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
            timeSLU = 0;
            CallDeferred("TimeUpdate", timeSLU);
            return Task.CompletedTask;
        }
		catch (Exception e)
		{
			MainViewModel.EventLogger?.LogMessage($"ZedMonitor Error: {e.Message}");
			return Task.CompletedTask;
        }  
    }

    public void _on_timer_timeout()
    {
        timeSLU += timerInterval;

        if (timeSLU == errorTime)
        {
            GD.Print($"ZedMonitor Error: gyro data >{errorTime} seconds old.");

        }
        if (timeSLU >= errorTime)
        {
            errorDisplay.Visible = true;
        }
        TimeUpdate(timeSLU);
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
        var eulerZYX = Quat.Normalized().GetEuler(EulerOrder.Zyx);
        double rollDeg = Mathf.RadToDeg(eulerZYX.X);
        double pitchDeg = Mathf.RadToDeg(eulerZYX.Y);
        CallDeferred("DisplayUpdate", rollDeg, pitchDeg);
    }
    private void DisplayUpdate(double rollDeg, double pitchDeg)
	{
        if (error == true)
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