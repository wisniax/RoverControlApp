using Godot;
using MQTTnet;
using RoverControlApp.Core;
using RoverControlApp.MVVM.Model;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace RoverControlApp.MVVM.ViewModel;

public partial class MissionStatus_UIOverlay : UIOverlay
{
	public override Dictionary<int, Setting> Presets { get; } = new()
	{
		{ -1, new(Colors.DarkRed, Colors.OrangeRed, "Mission Status: N/A", "Mission Status: ") },
		{ 0, new(Colors.DarkRed, Colors.Orange, "Mission Status: STOPPED", "Mission Status: ") },
		{ 4, new(Colors.DarkRed, Colors.Orange, "Mission Status: STOPPED", "Mission Status: ") },
		{ 1, new(Colors.Blue, Colors.LightBlue, "Mission Status: Starting...", "Mission Status: ") },
		{ 2, new(Colors.DarkGreen, Colors.LightGreen, "Mission Status: Running", "Mission Status: ") },
		{ 3, new(Colors.Blue, Colors.LightBlue, "Mission Status: Stopping...", "Mission Status: ") },
		{ 5, new(Colors.Orange, Colors.LightYellow, "Mission Status: PAUSED", "Mission Status: ") },
	};

	public override void _Ready()
	{
		base._Ready();
		ControlMode = -1;
	}

	public Task StatusChangeSubscriber(MqttClasses.RoverMissionStatus? status)
	{
		if (status is not null)
			ControlMode = (int)status.MissionStatus;
		else
			ControlMode = -1;
		return Task.CompletedTask;
	}
}
