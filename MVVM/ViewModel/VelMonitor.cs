using Godot;
using MQTTnet;
using MQTTnet.Internal;
using OpenCvSharp;
using RoverControlApp.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Text;


namespace RoverControlApp.MVVM.ViewModel;
public partial class VelMonitor : Panel
{
	private MqttClient? _mqttClient => MainViewModel.MqttClient;
	const int ITEMS = 4;
	[Export]
	Label Test;
	[ExportGroup("Settings")]
	[Export]
	string headStr = "ID ";
	[ExportGroup("Settings")]
	[Export]
	string powerStr = "Power: ";
	[ExportGroup("Settings")]
	[Export]
	string timestr = "Delay: ";
	[ExportGroup("Settings")]
	[Export]
	float SliderMaxVal = 5;
	[ExportGroup("Settings")]
	[Export]
	float SliderMinVal = -5;

	[ExportGroup("NodePaths")]
	[Export]
	NodePath[] headLabs_NodePaths = new NodePath[ITEMS];
	[ExportGroup("NodePaths")]
	[Export]
	NodePath[] dataLabs_NodePaths = new NodePath[ITEMS];
	[ExportGroup("NodePaths")]
	[Export]
	NodePath[] sliders_NodePaths = new NodePath[ITEMS];
	[ExportGroup("NodePaths")]
	[Export]
	NodePath[] timestampLabelNodePaths = new NodePath[ITEMS];
	
	Label[] headLabs;
	Label[] dataLabs;
	Label[] timestampLabels;
	SliderController[] sliderControllers;
	MqttClasses.WheelControl ?Wheels;
	int[] ?IdTab;
	
	

	public override void _Ready()
	{
		

		headLabs = new Label[ITEMS];
		dataLabs = new Label[ITEMS];
		timestampLabels = new Label[ITEMS];
		sliderControllers = new SliderController[ITEMS];
		for (int i = 0; i < ITEMS; i++)
		{
			headLabs[i] = GetNode<Label>(headLabs_NodePaths[i]);
			dataLabs[i] = GetNode<Label>(dataLabs_NodePaths[i]);
			timestampLabels[i] = GetNode<Label>(timestampLabelNodePaths[i]);


			headLabs[i].Text = headStr;
			dataLabs[i].Text = powerStr + "N/A";
			timestampLabels[i].Text = timestr + "0ms";

			sliderControllers[i] = GetNode<SliderController>(sliders_NodePaths[i]);
			sliderControllers[i].InputMinValue(SliderMinVal);
			sliderControllers[i].InputMaxValue(SliderMaxVal);
		}


	}

public Task MqttSubscriber(string subTopic, MqttApplicationMessage? msg)
{
	if (MainViewModel.Settings?.Settings?.Mqtt.TopicWheelFeedback is null ||
		subTopic != MainViewModel.Settings?.Settings?.Mqtt.TopicWheelFeedback)
		return Task.CompletedTask;

	if (msg is null || msg?.PayloadSegment is null)
	{
		MainViewModel.EventLogger?.LogMessage($"VelMonitor WARNING: Empty payload!");
		return Task.CompletedTask;
	}

	try
	{
		
		Wheels = JsonSerializer.Deserialize<MqttClasses.WheelControl>(msg.ConvertPayloadToString());

		if (Wheels is null)
		{
			MainViewModel.EventLogger?.LogMessage($"VelMonitor WARNING: Failed to deserialize payload!");
			return Task.CompletedTask;
		}
			int id = Wheels.VescId;
			int power =(int) (Wheels.VoltsIn * Wheels.Current);
			long delay = Wheels.Timestamp;

		 CallDeferred("UpdateVisual", id, power, delay);
		 
		}
	catch (Exception e)
	{
		MainViewModel.EventLogger?.LogMessage($"VelMonitor ERROR: Something went wrong: {e.Message}");
	}

	return Task.CompletedTask;
}



    public void UpdateVisual(int id, int pow, long timestamp)
    {
        try
        {
            if (IdTab == null)
            {
                IdTab = new int[0];
            }
            var localIdx = id;
            if (IdTab.Contains(localIdx))
            {
                var index = Array.IndexOf(IdTab, localIdx);
                dataLabs[index].Text = $"{powerStr} {pow}";
                sliderControllers[index].InputValue(pow);
                long time = ConvertToMMs(timestamp);
                timestampLabels[index].Text = $"{timestr}: {time}";
                if (time < 30)
                {
                    timestampLabels[index].Modulate = new Color(0, 1, 0);
                }
                else if (time >= 30 && time < 100)
                {
                    timestampLabels[index].Modulate = new Color(1, 1, 0);
                }
                else
                {
                    timestampLabels[index].Modulate = new Color(1, 0, 0);
                }
            }
            else
            {
                if (IdTab.Length >= ITEMS)
                {
                    MainViewModel.EventLogger?.LogMessage("Invalid wheel ID");
                }
                else
                {
                    IdTab = IdTab.Append(localIdx).ToArray(); 
                    var index = IdTab.Length - 1; 
                    dataLabs[index].Text = $"{powerStr} {pow}";
                    sliderControllers[index].InputValue(pow);
                    long time = ConvertToMMs(timestamp);
                    timestampLabels[index].Text = $"{timestr}: {time}";

                    if (time < 30)
                    {
                        timestampLabels[index].Modulate = new Color(0, 1, 0);
                    }
                    else if (time >= 30 && time < 100)
                    {
                        timestampLabels[index].Modulate = new Color(1, 1, 0);
                    }
                    else
                    {
                        timestampLabels[index].Modulate = new Color(1, 0, 0);
                    }
                }
            }
        }
        catch (Exception e)
        {
            MainViewModel.EventLogger?.LogMessage($"VelMonitor ERROR: Failed to update visual: {e.Message}");
        }
    }



    public long ConvertToMMs(long timestamp) { 
		return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - timestamp;
	}
}
