using Godot;
using RoverControlApp.Core;
using RoverControlApp.MVVM.Model;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace RoverControlApp.MVVM.ViewModel;

public partial class Grzyb_UIOverlay : UIOverlay
{
	public override Dictionary<int, Setting> Presets { get; } = new()
	{
		{ 0, new(Colors.DarkGreen, Colors.LightGreen, "Moshroom: UNMOLDED", "Moshroom: ") },
		{ 1, new(Colors.DarkRed, Colors.Orange, "Moshroom: MOLDED", "Moshroom: ") },
		{ 2, new(Colors.WebGray, Colors.LightGray, "Moshroom: N/A", "Moshroom: ") },
	};

	public override void _Ready()
	{
		base._Ready();
		ControlMode = 2;
	}

	public void SetMushroom(MqttClasses.MushroomStatus molded)
	{
		if ((int)molded == 0 && ControlMode != 0) { ControlMode = 0; return; }
		if ((int)molded == 1 && ControlMode != 1) { ControlMode = 1; return; }
		if ((int)molded == 2 && ControlMode != 2) { ControlMode = 2; return; }
	}
}
