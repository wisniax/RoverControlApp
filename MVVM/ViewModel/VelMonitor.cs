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

namespace RoverControlApp.MVVM.ViewModel;
public partial class VelMonitor : Panel
{
	const int ITEMS = 4;

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
	NodePath[] headLabs_NodePaths = new NodePath[4];
	[ExportGroup("NodePaths")]
	[Export]
	NodePath[] dataLabs_NodePaths = new NodePath[4];
	[ExportGroup("NodePaths")]
	[Export]
	NodePath[] sliders_NodePaths = new NodePath[4];
	[ExportGroup("NodePaths")]
	[Export]
	NodePath[] timestampLabelNodePaths = new NodePath[ITEMS];
	
	Dictionary<int, int> idSettings = new()
	{
		{ 1, 1 },
		{ 2, 0 },
		{ 3, 3 },
		{ 4, 2 },
		{ 5, 5 },
		{ 6, 4 },
	};

	Label[] headLabs;
	Label[] dataLabs;
	Label[] timestampLabels;
	SliderController[] sliderControllers;

	private bool LenCheck()
	{
		return headLabs_NodePaths.Length == 4 && dataLabs_NodePaths.Length == 4 && sliders_NodePaths.Length == 4 && idSettings.Count == 4;
	}

	public override void _Ready()
	{
		if (!LenCheck())
			throw new Exception("Array lenght missmath!");

		headLabs = new Label[ITEMS];
		dataLabs = new Label[ITEMS];
		timestampLabels = new Label[ITEMS];
		sliderControllers = new SliderController[ITEMS];
		for (int i = 0; i < ITEMS; i++)
		{
			headLabs[i] = GetNode<Label>(headLabs_NodePaths[i]);
			dataLabs[i] = GetNode<Label>(dataLabs_NodePaths[i]);
			timestampLabels[i] = GetNode<Label>(timestampLabelNodePaths[i]);

			var keyOfValue = idSettings.First(kvp => kvp.Value == i).Key;

			headLabs[i].Text = headStr + keyOfValue.ToString();
			dataLabs[i].Text = powerStr + "N/A";
			timestampLabels[i].Text = timestr + "0ms";

			sliderControllers[i] = GetNode<SliderController>(sliders_NodePaths[i]);
			sliderControllers[i].InputMinValue(SliderMinVal);
			sliderControllers[i].InputMaxValue(SliderMaxVal);
		}
	}

	struct SingleWheel
	{
		public UInt32 id;
		public float current;
		public float voltsIn;
		public UInt32 timestamp;
		
		public SingleWheel(uint id, float current, float voltsIn, uint timestamp)
		{
			this.id = id;
			this.current= current;
			this.voltsIn = voltsIn;
			this.timestamp = timestamp;
			
		}
		
		/*public static unsafe SingleWheel FromBytes(byte* rawdata)
		{
			byte* idPtr = &rawdata[0];
			byte* CurrentPtr = &rawdata[8];
			byte* VoltsInPtr = &rawdata[40];
			byte* timestampPtr = &rawdata[72];

			return new SingleWheel(*(UInt32*)idPtr, *(float*)CurrentPtr, *(float*)VoltsInPtr, *(UInt32*)timestampPtr);
		}
		*/
	}

	public Task MqttSubscriber(string subTopic, MqttApplicationMessage? msg)
{
	if (MainViewModel.Settings?.Settings?.Mqtt.TopicWheelFeedback is null ||
		subTopic != MainViewModel.Settings?.Settings?.Mqtt.TopicWheelFeedback)
		return Task.CompletedTask;

	if (msg is null)
	{
		MainViewModel.EventLogger?.LogMessage($"VelMonitor WARNING: Empty payload!");
		return Task.CompletedTask;
	}

	try
	{
		var json = msg.Payload;
		CallDeferred(nameof(UpdateVisual), json);
	}
	catch (Exception e)
	{
		MainViewModel.EventLogger?.LogMessage($"VelMonitor ERROR: Something went wrong: {e.Message}");
	}

	return Task.CompletedTask;
}


	public void UpdateVisual(string json)
{
	try
	{
		var dic = JsonSerializer.Deserialize<Dictionary<string, string>>(json);

			if (dic == null)
		{
			MainViewModel.EventLogger?.LogMessage($"VelMonitor ERROR: Failed to parse JSON data.");
			return;
		}

		var vescId = int.Parse(dic["VescId"]);
		var current = float.Parse(dic["Current"]);
		var voltsIn = float.Parse(dic["VoltsIn"]);
		var time = UInt32.Parse(dic["Timestamp"]);
		
		var localIdx = idSettings.ContainsKey(vescId) ? idSettings[vescId] : -1;
		if (localIdx != -1)
		{
			dataLabs[localIdx].Text = $"{powerStr} {current * voltsIn}";
			sliderControllers[localIdx].InputValue(current * voltsIn);
			timestampLabels[localIdx].Text = $"{timestr}: {time}";
		}
		else
		{
			MainViewModel.EventLogger?.LogMessage($"VelMonitor WARNING: Unknown VescId: {vescId}");
		}
	}
	catch (Exception e)
	{
		MainViewModel.EventLogger?.LogMessage($"VelMonitor ERROR: Failed to update visual: {e.Message}");
	}
}

}
