using Godot;
using RoverControlApp.Core.Settings;
using RoverControlApp.MVVM.Model;
using System;
using static RoverControlApp.Core.MqttClasses;

public partial class MissionPlanner : Panel
{
	[Export] TextureRect picture = null!;
	[Export] TextEdit picturePath = null!;

	public override void _EnterTree()
	{
		picturePath.TextChanged += LoadPicture;
	}

	public override void _Ready()
	{
		LoadPicture();
	}

	public override void _Process(double delta)
	{
	}

	void LoadPicture()
	{
		try
		{
			switch(picturePath.Text)
			{
				case "ExampleMap":
				case "ExampleMap.png":
				case "ExampleMap.jpg":
				case "ExampleMap.jpeg":
					picture.Texture = GD.Load<Texture2D>("res://Resources/ExampleMap.jpeg");
					GD.Print("Using example map.");
					break;
				case "":
					picture.Texture = GD.Load<Texture2D>("res://Resources/raptors_logoHorizontal_color_nobg.png");
					GD.Print("No picture path provided, using default logo.");
					break;
				default:
					picture.Texture = GD.Load<Texture2D>(picturePath.Text);
					GD.Print($"Picture loaded from path: {picturePath.Text}");
					break;
			}
		}
		catch (Exception e)
		{
			GD.PrintErr($"Error initializing picture: {e.Message}");
			return;
		}
	}
}
