using Godot;
using RoverControlApp.Core;
using RoverControlApp.Core.Settings;
using RoverControlApp.MVVM.Model;
using System;
using System.IO;
using static RoverControlApp.Core.MqttClasses;

public partial class MissionPlanner : Panel
{
	[Export] TextureRect picture = null!;
	[Export] TextEdit picturePath = null!;

	public override void _EnterTree()
	{
		picturePath.TextChanged += LoadPicture;
		picture.MouseEntered += () =>
		{
			GD.Print("Mouse entered the picture area.");
		};
		picture.MouseExited += () =>
		{
			GD.Print("Mouse exited the picture area.");
		};
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
					EventLogger.LogMessage("MissionPlanner", EventLogger.LogLevel.Info, "Using example map.");
					break;

				case "":
					picture.Texture = GD.Load<Texture2D>("res://Resources/raptors_logoHorizontal_color_nobg.png");
					EventLogger.LogMessage("MissionPlanner", EventLogger.LogLevel.Info, "No picture path provided, using default logo.");
					break;

				default:
					if (!File.Exists(picturePath.Text)) break;
					picture.Texture = GD.Load<Texture2D>(picturePath.Text);
					EventLogger.LogMessage("MissionPlanner", EventLogger.LogLevel.Info, $"Picture loaded from path: {picturePath.Text}");		
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
