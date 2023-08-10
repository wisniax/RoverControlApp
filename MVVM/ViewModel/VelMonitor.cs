using Godot;
using MQTTnet;
using MQTTnet.Internal;
using OpenCvSharp;
using RoverControlApp.Core;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RoverControlApp.MVVM.ViewModel;
public partial class VelMonitor : Panel
{
	const int ITEMS = 6;

	const string HEAD_STR = "ID ";
	const string ANGVEL_STR = "AngVel: ";
	const string STEERANG_STR = "SteerAng: ";

	[Export]
	NodePath[] headLabs_NodePaths = new NodePath[6];
	[Export]
	NodePath[] dataLabs_NodePaths = new NodePath[6];

	Dictionary<int,int> idSettings = new()
	{
		{ 1, 0 },
		{ 2, 2 },
		{ 3, 4 },
		{ 4, 1 },
		{ 5, 3 },
		{ 6, 5 },
	};

	Label[] headLabs;
	Label[] dataLabs;

	public override void _Ready()
	{
		if (headLabs_NodePaths.Length != ITEMS || dataLabs_NodePaths.Length != ITEMS || idSettings.Count != ITEMS)
			throw new Exception("Array lenght missmath!");

		headLabs = new Label[ITEMS];
		dataLabs = new Label[ITEMS];
		for (int i = 0;i< ITEMS; i++)
		{
			headLabs[i] = GetNode<Label>(headLabs_NodePaths[i]);
			dataLabs[i] = GetNode<Label>(dataLabs_NodePaths[i]);

			var keyOfValue = idSettings.First(kvp => kvp.Value == i).Key;

			headLabs[i].Text = HEAD_STR + keyOfValue.ToString();
			dataLabs[i].Text = ANGVEL_STR + "N/A\n" + STEERANG_STR + "N/A";
		}
	}

	struct SingleWheel
	{
		public UInt32 id;
		public float angleVelocity;
		public float steerAngle;

		public SingleWheel(uint id, float angleVelocity, float steerAngle)
		{
			this.id = id;
			this.angleVelocity = angleVelocity;
			this.steerAngle = steerAngle;
		}

		public static unsafe SingleWheel FromBytes(byte* rawdata)
		{
			byte* idPtr = &rawdata[0];
			byte* angleVelocityPtr = &rawdata[4];
			byte* steerAnglePtr = &rawdata[8];

			return new SingleWheel( *(UInt32*)idPtr, *(float*)angleVelocityPtr, *(float*)steerAnglePtr);
		}
	}

	public System.Threading.Tasks.Task MqttSubscriber(string subTopic, MqttApplicationMessage? msg)
	{
		if(MainViewModel.Settings?.Settings?.Mqtt.TopicWheelFeedback is null || subTopic != MainViewModel.Settings?.Settings?.Mqtt.TopicWheelFeedback)
			return System.Threading.Tasks.Task.CompletedTask;

		if(msg is null || msg.PayloadSegment.Count == 0)
		{
			MainViewModel.EventLogger?.LogMessage($"VelMonitor WARNING: Empty payload!");
			return System.Threading.Tasks.Task.CompletedTask;
		}

		UpdateVisual(msg!.PayloadSegment.Array!);
		return System.Threading.Tasks.Task.CompletedTask;
	}

	public unsafe void UpdateVisual(byte[] rawdata)
	{
		if(rawdata.Length == 76)
		{
			MainViewModel.EventLogger?.LogMessage($"VelMonitor ERROR: rawdata.Length mismatch! (!= 76)");
			return;
		}

		fixed(byte* rawdataPtr = &rawdata[0])
			for (int offset = 4; offset < 76; offset += 12)
			{
				var wheelData = SingleWheel.FromBytes(&rawdataPtr[offset]);
				dataLabs[idSettings[(int)wheelData.id]].Text = ANGVEL_STR + $"{wheelData.angleVelocity}\n" + STEERANG_STR + $"{wheelData.steerAngle}";
			}
			
	}
}
