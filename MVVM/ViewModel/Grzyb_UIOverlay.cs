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
		{ 1, new(Colors.DarkRed, Colors.Orange, "Moshroom: MOLDED", "Moshroom: ") }
	};

	public override void _Ready()
	{
		base._Ready();
		ControlMode = 1;
	}

	public void SetMushroom(bool molded)
	{
		if ( molded && ControlMode != 1) { ControlMode = 1; return; }
		if (!molded && ControlMode != 0) { ControlMode = 0; return; }
	}
}
