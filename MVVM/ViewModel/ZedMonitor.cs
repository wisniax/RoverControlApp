using Godot;
using MQTTnet;
using MQTTnet.Internal;
using OpenCvSharp;
using RoverControlApp.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RoverControlApp.MVVM.ViewModel;

public partial class ZedMonitor : Panel
{
	//Fake data for UI testing
	[Export]
	float pitchDeg;
	[Export]
	float rollDeg;

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

    public override void _Ready()
	{
		
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		VisualisationUpdate();
		DisplayUpdate();
	}

	public void VisualisationUpdate()
	{
		pitchVisualisation.RotationDegrees = -pitchDeg;
		rollVisualisation.RotationDegrees = rollDeg;
	}

	public void DisplayUpdate()
	{
		pitchDisplay.Text = $"{pitchDeg} deg";
        rollDisplay.Text = $"{rollDeg} deg";
    }


}
