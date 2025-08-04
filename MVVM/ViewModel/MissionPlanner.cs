using Godot;
using OpenCvSharp;
using RoverControlApp.Core;
using RoverControlApp.Core.Settings;
using RoverControlApp.MVVM.Model;
using RoverControlApp.MVVM.ViewModel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using static RoverControlApp.Core.MqttClasses;

public partial class MissionPlanner : Panel
{
	[Export] TextureRect picture = null!;
	[Export] Label mousePosLabel = null!;
	[Export] VBoxContainer waypointsContainer = null!;



	List<Point> points = new List<Point>();
	List<Waypoint> wayPoints = new List<Waypoint>();

	public override void _EnterTree()
	{
		//picturePath.TextChanged += LoadPicture;
		picture.GuiInput += HandleMouseInput;
		//DisplayServer.WindowResized += HandleScreenSizeChange;
		GetTree().Root.SizeChanged += HandleScreenSizeChange;
		LocalSettings.Singleton.Connect(LocalSettings.SignalName.PropagatedPropertyChanged, Callable.From<StringName, StringName, Variant, Variant>(LoadPictureHandler));
	}


	public override void _Ready()
	{
		LoadPicture();
	}



	public override void _Process(double delta)
	{
		//GD.Print(DisplayServer.WindowGetSize());
	}

	void LoadPictureHandler(StringName category, StringName name, Variant oldValue, Variant newValue)
	{
		if (category != nameof(General)) return;
		if (name != nameof(General.MissionControlMapPath)) return;
		LoadPicture();
	}

	void LoadPicture()
	{
		try
		{
			switch (LocalSettings.Singleton.General.MissionControlMapPath)
			{
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
					if (!File.Exists(LocalSettings.Singleton.General.MissionControlMapPath)) break;
					picture.Texture = GD.Load<Texture2D>(LocalSettings.Singleton.General.MissionControlMapPath);
					EventLogger.LogMessage("MissionPlanner", EventLogger.LogLevel.Info, $"Picture loaded from path: {LocalSettings.Singleton.General.MissionControlMapPath}");
					break;
			}
		}
		catch (Exception e)
		{
			GD.PrintErr($"Error initializing picture: {e.Message}");
			return;
		}
	}

	void HandleMouseInput(InputEvent inputEvent)
	{
		if (inputEvent is InputEventMouseMotion)
		{
			Vector2 temp = GetLocalMousePosition();
			mousePosLabel.Text = $"Current Position: {temp.X:F0}, {temp.Y:F0}";
			return;
		}

		if (inputEvent is InputEventMouseButton mouseButton && mouseButton.IsPressed())
		{
			if (mouseButton.ButtonIndex == MouseButton.Left)
			{
				TryAddPoint(GetLocalMousePosition());
				return;
			}

			if (mouseButton.ButtonIndex == MouseButton.Right)
			{
				TryRemovePoint(GetLocalMousePosition());
				return;
			}
		}
	}

	void TryAddPoint(Vector2 pos)
	{
		if (pos.X > picture.Size.X || pos.Y > picture.Size.Y) return;

		var scene = GD.Load<PackedScene>("res://MVVM/View/Point.tscn");
		var inst = scene.Instantiate();
		AddChild(inst);
		if (inst is Point point)
		{
			point.SetColor(Colors.Blue);
			point.SetNumber(points.Count + 1);
			point.Position = pos;
			points.Add(point);
			GD.Print($"Point added at position: {pos}, total points: {points.Count}");
		}
		else
		{
			GD.PrintErr("Failed to instantiate Point scene.");
			return;
		}

		scene = GD.Load<PackedScene>("res://MVVM/View/Waypoint.tscn");
		inst = scene.Instantiate();
		if (inst is Waypoint waypoint)
		{
			waypoint.Coordinates = pos;
			waypoint.Number = wayPoints.Count + 1;
			waypoint.Deadzone = 2;
			wayPoints.Add(waypoint);

			waypointsContainer.AddChild(inst);
		}
		else
		{
			GD.PrintErr("Failed to instantiate Waypoint scene.");
			return;
		}



		GD.Print($"Trying to add point at position: {pos}");
	}

	void TryRemovePoint(Vector2 pos)
	{
		if (points.Count == 0) return;
		if (pos.X > picture.Size.X || pos.Y > picture.Size.Y) return;

		foreach (var point in points)
		{
			if (point.Position.DistanceTo(pos) < 20)
			{
				RemoveWaypoint(point);
				
				break;
			}
		}

		GD.Print($"Trying to remove point at position: {pos}");
	}

	void HandleScreenSizeChange()
	{
		if (DisplayServer.WindowGetSize().X > 1700 && DisplayServer.WindowGetSize().Y > 900)
			this.Scale = new Vector2(1.0f, 1.0f);
		else
			this.Scale = new Vector2(0.6f, 0.6f);
		GD.Print($"Screen size changed to: {DisplayServer.WindowGetSize()}");
	}

	public void RemoveWaypoint(Waypoint waypoint)
	{
		if (waypoint == null) return;

		points[waypoint.Number-1].QueueFree();
		points.RemoveAll(p=> p.Number == waypoint.Number);
		points.ForEach(p => p.SetNumber(points.IndexOf(p)+1));


		waypointsContainer.RemoveChild(waypoint);
		waypoint.QueueFree();
		wayPoints.Remove(waypoint);
		for (int i = 0; i < wayPoints.Count; i++)
		{
			wayPoints[i].Number = i + 1;
			waypointsContainer.MoveChild(wayPoints[i], i+2);
		}
	}

	public void RemoveWaypoint(Point point)
	{
		if (point == null) return;

		point.QueueFree();
		points.RemoveAll(p => p == point);
		points.ForEach(p => p.SetNumber(points.IndexOf(p) + 1));


		waypointsContainer.RemoveChild(wayPoints[point.Number]);
		wayPoints[point.Number-1].QueueFree();
		wayPoints.Remove(wayPoints[point.Number]);
		for (int i = 0; i < wayPoints.Count; i++)
		{
			wayPoints[i].Number = i + 1;
			waypointsContainer.MoveChild(wayPoints[i], i + 2);
		}
	}
}
