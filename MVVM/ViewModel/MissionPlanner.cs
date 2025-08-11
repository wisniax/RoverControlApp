using Godot;
using MQTTnet;
using OpenCvSharp;
using RoverControlApp.Core;
using RoverControlApp.Core.Settings;
using RoverControlApp.MVVM.Model;
using RoverControlApp.MVVM.ViewModel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading.Tasks;
using static RoverControlApp.Core.MqttClasses;

public partial class MissionPlanner : Panel
{
	[Export] TextureRect picture = null!;

	[Export] Label picturePathLabel = null!;
	[Export] Label mousePosLabel = null!;
	[Export] Label localPosLabel = null!;
	[Export] VBoxContainer waypointsContainer = null!;
	[Export] Button[] selectReferencePoint = new Button[2];
	[Export] Point[] referencePoints = new Point[2];
	[Export] Control pointsContainer = null!;

	[Export] Button startButton = null!;
	[Export] Button pauseButton = null!;
	[Export] Button cancelButton = null!;

	[Export] Point roverPosition = null!;

	[Export] TextEdit[] refPoint1 = new TextEdit[4];
	[Export] TextEdit[] refPoint2 = new TextEdit[4];

	[Export] Label roverPosLabel = null!;
	[Export] Label distanceLabel = null!;

	List<Point> points = new List<Point>();
	List<Waypoint> waypoints = new List<Waypoint>();

	int _lastSelectedReferencePoint = 0;

	Vector2? _nextTargetWaypoint;
	int _nextWaypointNumber = 0;
	MqttClasses.MissionStatus? MissionStatus;
	bool _missionActive = false;

	Vector2 _roverPosition;

	public override void _EnterTree()
	{
		selectReferencePoint[0].Pressed += () =>
		{
			_lastSelectedReferencePoint = 0;
			referencePoints[0].SetColor(Colors.DarkGreen);
			referencePoints[1].SetColor(Colors.DarkRed);
		};
		selectReferencePoint[1].Pressed += () =>
		{
			_lastSelectedReferencePoint = 1;
			referencePoints[0].SetColor(Colors.DarkRed);
			referencePoints[1].SetColor(Colors.DarkGreen);
		};
		picture.GuiInput += HandleMouseInput;
		//DisplayServer.WindowResized += HandleScreenSizeChange;
		GetTree().Root.SizeChanged += HandleScreenSizeChange;
		LocalSettings.Singleton.Connect(LocalSettings.SignalName.PropagatedPropertyChanged, Callable.From<StringName, StringName, Variant, Variant>(LoadPictureHandler));

		refPoint1[0].TextChanged += () => UpdateReferenceCoordinatesPhoto(0);
		refPoint1[1].TextChanged += () => UpdateReferenceCoordinatesPhoto(1);
		refPoint2[0].TextChanged += () => UpdateReferenceCoordinatesPhoto(2);
		refPoint2[1].TextChanged += () => UpdateReferenceCoordinatesPhoto(3);
		refPoint1[2].TextChanged += () => UpdateReferenceCoordinatesReal(0);
		refPoint1[3].TextChanged += () => UpdateReferenceCoordinatesReal(1);
		refPoint2[2].TextChanged += () => UpdateReferenceCoordinatesReal(2);
		refPoint2[3].TextChanged += () => UpdateReferenceCoordinatesReal(3);

		MqttNode.Singleton.MessageReceivedAsync += OnRoverPositionReceived;

		startButton.Pressed += StartOrContinueMission;
		pauseButton.Pressed += PauseMission;
		cancelButton.Pressed += CancelMission;
	}

	private void StartOrContinueMission()
	{
		_nextTargetWaypoint = waypoints[_nextWaypointNumber].Coordinates;
		_missionActive = true;
		SendNextWaypointToRover(waypoints[_nextWaypointNumber]);
	}

	private void PauseMission()
	{
		var data = new MissionPlannerMessage();
		data.MessageType = MissionPlannerMessageType.PauseMission;
		waypoints[_nextWaypointNumber].SetColor(Colors.White);
		points[_nextWaypointNumber].SetColor(Colors.Blue);
		MqttNode.Singleton.EnqueueMessageAsync(LocalSettings.Singleton.Mqtt.TopicMissionPlanner, JsonSerializer.Serialize(data));

		return;
	}

	private void CancelMission()
	{
		var data = new MissionPlannerMessage();
		data.MessageType = MissionPlannerMessageType.CancelMission;
		_nextTargetWaypoint = null;
		_nextWaypointNumber = 0;
		_missionActive = false;
		ResetWaypoints();
		MqttNode.Singleton.EnqueueMessageAsync(LocalSettings.Singleton.Mqtt.TopicMissionPlanner, JsonSerializer.Serialize(data));

		return;
	}

	private async Task OnRoverPositionReceived(string subTopic, MqttApplicationMessage? msg)
	{
		if (string.IsNullOrEmpty(LocalSettings.Singleton.Mqtt.TopicMissionPlannerFeedback) || subTopic != LocalSettings.Singleton.Mqtt.TopicMissionPlannerFeedback)
			return;
		if (msg is null || msg.PayloadSegment.Count == 0)
		{
			EventLogger.LogMessage("MissionPlannerFeedback", EventLogger.LogLevel.Error, "Empty payload");
			return;
		}
		try
		{
			MqttClasses.MissionPlannerFeedback data;
			data = JsonSerializer.Deserialize<MqttClasses.MissionPlannerFeedback>(msg.ConvertPayloadToString());

			var roverPos = new Vector2(data.CurrentPosX, data.CurrentPosY);
			_roverPosition = roverPos;
			UpdateRoverPositionHandler(roverPos);
		}
		catch(Exception e)
		{
			EventLogger.LogMessage("MissionPlanner", EventLogger.LogLevel.None, $"{e}");
		}
	}

	public Task UpdateRoverPositionHandler(Vector2 roverPos)
	{
		CallDeferred("UpdateRoverPosition", roverPos);
		return Task.CompletedTask;
	}

	public void UpdateRoverPosition(Vector2 roverPos)
	{
		roverPosition.Position = RealToPhoto(roverPos);
		roverPosLabel.Text = $"RoverPos: {roverPos}";

		if (!_missionActive) return;
		var distance = roverPos.DistanceTo((Vector2)_nextTargetWaypoint);
		if (distance < waypoints[_nextWaypointNumber].Deadzone)
		{
			HandleReachingPoint();
			distance = roverPos.DistanceTo((Vector2)_nextTargetWaypoint);
		}

		distanceLabel.Text = $"DistanceToTarget: {distance:f2}m";
	}

	void HandleReachingPoint()
	{
		points[_nextWaypointNumber].SetColor(Colors.Green);
		waypoints[_nextWaypointNumber].SetColor(Colors.Green);

		if (waypoints.Last<Waypoint>() == waypoints[_nextWaypointNumber])
		{
			ResetWaypoints();
			return;
		}

		_nextWaypointNumber++;
		_nextTargetWaypoint = waypoints[_nextWaypointNumber].Coordinates;

		if (waypoints[_nextWaypointNumber-1].IsWaitChecked) return; //Next waypoint will be sent when start button is pressed again

		SendNextWaypointToRover(waypoints[_nextWaypointNumber]);
	}

	private void ResetWaypoints()
	{
		foreach (var point in points)
		{
			point.SetColor(Colors.Blue);
		}
		foreach (var waypoint in waypoints)
		{
			waypoint.SetColor(Colors.White);
		}

		_nextWaypointNumber = 0;
	}

	public override void _Ready()
	{
		referencePoints[0].SetColor(Colors.DarkGreen);
		referencePoints[0].SetNumber(0);
		referencePoints[1].SetColor(Colors.DarkRed);
		referencePoints[1].SetNumber(1);

		roverPosition.SetColor(Colors.DarkOrange);
		roverPosition.SetString("R");

		LoadPicture();
		HandleScreenSizeChange();

		//MqttNode.Singleton.EnqueueMessageAsync(LocalSettings.Singleton.Mqtt.TopicBatteryControl,JsonSerializer.Serialize(arg));
	}



	public override void _Process(double delta)
	{
		//float fi;
		//float scale;
		//double[] t_p2r = new double[2];
		//double[] t_r2p = new double[2];

		//GD.Print($"fi: {fi}\n" +
		//		$"scale: {scale}\n" +
		//		$"t_p2r: {t_p2r[0]}, {t_p2r[1]}\n" +
		//		$"t_r2p: {t_r2p[0]}, {t_r2p[1]}");
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
					string exeDir = OS.GetExecutablePath().GetBaseDir();
					string filePath = exeDir + "/" + LocalSettings.Singleton.General.MissionControlMapPath;
					if (!File.Exists(filePath)) break;

					var img = new Image();
					var err = img.Load(filePath);
					if (err == Error.Ok)
					{
						var tex = ImageTexture.CreateFromImage(img);
						picture.Texture = tex;
					}

					EventLogger.LogMessage("MissionPlanner", EventLogger.LogLevel.Info, $"Picture loaded from path: {LocalSettings.Singleton.General.MissionControlMapPath}");
					break;
			}
		}
		catch (Exception e)
		{
			GD.PrintErr($"Error initializing picture: {e.Message}");
			return;
		}

		picturePathLabel.Text = $"Picture path: {LocalSettings.Singleton.General.MissionControlMapPath}";
	}

	void HandleMouseInput(InputEvent inputEvent)
	{
		if (inputEvent is InputEventMouseMotion)
		{
			Vector2 temp = GetLocalMousePosition();
			mousePosLabel.Text = $"OnPhotoPos: ({MathF.Round(ToGoodCoordinates(temp).X,0)}, {MathF.Round(ToGoodCoordinates(temp).Y, 0)})";
			if (Point1Photo == Point2Photo || Point1Real == Point2Real) return;
			localPosLabel.Text = $"OnLocalPos: {PhotoToReal(temp)}";
			return;
		}

		if (inputEvent is InputEventMouseButton mouseButton && mouseButton.IsPressed())
		{
			if (waypointsContainer.GetParent().GetParent<Control>().Visible)
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
			else
			{
				if (mouseButton.ButtonIndex == MouseButton.Left)
				{
					_lastSelectedReferencePoint = 0;
				}

				if (mouseButton.ButtonIndex == MouseButton.Right)
				{
					_lastSelectedReferencePoint = 1;
				}
				MoveReferencePoint(GetLocalMousePosition());
			}	
		}
	}

	void TryAddPoint(Vector2 pos)
	{
		if (pos.X > picture.Size.X || pos.Y > picture.Size.Y) return;

		var scene = GD.Load<PackedScene>("res://MVVM/View/Point.tscn");
		var inst = scene.Instantiate();
		if (inst is Point point)
		{
			pointsContainer.AddChild(inst);
			point.SetColor(Colors.Blue);
			point.SetNumber(points.Count);
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
			waypoint.Coordinates = PhotoToReal(pos);
			waypoint.Number = waypoints.Count;
			waypoint.Deadzone = 0.2f;
			waypoints.Add(waypoint);

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
				RemoveWaypoint(waypoints[point.Number]);

				break;
			}
		}

		GD.Print($"Trying to remove point at position: {pos}");
	}

	public void RemoveWaypoint(Waypoint waypoint)
	{
		if (waypoint == null) return;

		points[waypoint.Number].QueueFree();
		points.RemoveAll(p => p.Number == waypoint.Number);
		points.ForEach(p => p.SetNumber(points.IndexOf(p)));


		waypointsContainer.RemoveChild(waypoint);
		waypoint.QueueFree();
		waypoints.Remove(waypoint);
		for (int i = 0; i < waypoints.Count; i++)
		{
			waypoints[i].Number = i;
			waypointsContainer.MoveChild(waypoints[i], i);
		}
	}

	public void MovePoint(Vector2 realPos, int number)
	{
		points[number].Position = RealToPhoto(realPos);
	}

	void HandleScreenSizeChange()
	{
		if (DisplayServer.WindowGetSize().X > 1700 && DisplayServer.WindowGetSize().Y > 900)
			this.Scale = new Vector2(1.0f, 1.0f);
		else
			this.Scale = new Vector2(0.6f, 0.6f);
		GD.Print($"Screen size changed to: {DisplayServer.WindowGetSize()}");
	}

	Vector2 Point1Real;
	Vector2 Point2Real;
	Vector2 Point1Photo;
	Vector2 Point2Photo;

	float fi;
	float scale;
	double[] t_p2r = new double[2];
	double[] t_r2p = new double[2];

	void UpdateReferenceCoordinatesPhoto(int whichOne)
	{
		switch(whichOne)
		{
			case 0: // X1
				Point1Photo.X = float.Parse(refPoint1[0].Text);
				_lastSelectedReferencePoint = 0;
				MoveReferencePoint(Point1Photo);
				break;
			case 1: // Y1
				Point1Photo.Y = float.Parse(refPoint1[1].Text);
				_lastSelectedReferencePoint = 0;
				MoveReferencePoint(Point1Photo);
				break;
			case 2: // X2
				Point2Photo.X = float.Parse(refPoint2[0].Text);
				_lastSelectedReferencePoint = 1;
				MoveReferencePoint(Point2Photo);
				break;
			case 3: // Y2
				Point2Photo.Y = float.Parse(refPoint2[1].Text);
				_lastSelectedReferencePoint = 1;
				MoveReferencePoint(Point2Photo);
				break;
		}

		CalibrateMap();
	}

	void UpdateReferenceCoordinatesReal(int whichOne)
	{
		switch (whichOne)
		{
			case 0: // X1
				Point1Real.X = float.Parse(refPoint1[2].Text);
				break;
			case 1: // Y1
				Point1Real.Y = float.Parse(refPoint1[3].Text);
				break;
			case 2: // X2
				Point2Real.X = float.Parse(refPoint2[2].Text);
				break;
			case 3: // Y2
				Point2Real.Y = float.Parse(refPoint2[3].Text);
				break;
		}

		CalibrateMap();
	}

	private void MoveReferencePoint(Vector2 newPlace)
	{
		referencePoints[_lastSelectedReferencePoint].Position = newPlace;
		selectReferencePoint[_lastSelectedReferencePoint].GetChild(0).GetChild(1).GetChild<TextEdit>(2).Text = Math.Round(referencePoints[_lastSelectedReferencePoint].Position.Y, 0).ToString();
		selectReferencePoint[_lastSelectedReferencePoint].GetNode<TextEdit>("Point/PicturePos/TextEdit").Text = Math.Round(referencePoints[_lastSelectedReferencePoint].Position.X, 0).ToString();

		switch (_lastSelectedReferencePoint)
		{
			case 0:
				Point1Photo = new Vector2(referencePoints[_lastSelectedReferencePoint].Position.X, picture.Size.Y - referencePoints[_lastSelectedReferencePoint].Position.Y);
				break;
			case 1:
				Point2Photo = new Vector2(referencePoints[_lastSelectedReferencePoint].Position.X, picture.Size.Y - referencePoints[_lastSelectedReferencePoint].Position.Y);
				break;
		}

		CalibrateMap();
	}

	void CalibrateMap()
	{
		float deltaPX = Point2Photo.X - Point1Photo.X;
		float deltaPY = Point2Photo.Y - Point1Photo.Y;
		float deltaRX = Point2Real.X - Point1Real.X;
		float deltaRY = Point2Real.Y - Point1Real.Y;

		scale = MathF.Sqrt((deltaRX * deltaRX + deltaRY * deltaRY) / (deltaPX * deltaPX + deltaPY * deltaPY));
		fi = MathF.Atan2(deltaRY, deltaRX) - MathF.Atan2(deltaPY, deltaPX);
		
		t_p2r[0] = Point1Real.X - scale * (Point1Photo.X * MathF.Cos(fi) - Point1Photo.Y * MathF.Sin(fi));
		t_p2r[1] = Point1Real.Y - scale * (Point1Photo.X * MathF.Sin(fi) + Point1Photo.Y * MathF.Cos(fi));
		
		t_r2p[0] = Point1Photo.X - 1/scale * (Point1Real.X * MathF.Cos(-fi) - Point1Real.Y * MathF.Sin(-fi));
		t_r2p[1] = Point1Photo.Y - 1/scale * (Point1Real.X * MathF.Sin(-fi) + Point1Real.Y * MathF.Cos(-fi));

		foreach(var waypoint in waypoints)
		{
			waypoint.Coordinates = PhotoToReal(points[waypoint.Number-1].Position);
		}
	}

	Vector2 PhotoToReal(Vector2 photo)
	{
		photo = ToGoodCoordinates(photo);
		Vector2 real = new Vector2();

		real.X = (float)(scale * (photo.X * Math.Cos(fi) - photo.Y * Math.Sin(fi)) + t_p2r[0]);
		real.Y = (float)(scale * (photo.X * Math.Sin(fi) + photo.Y * Math.Cos(fi)) + t_p2r[1]);

		real.X = MathF.Round(real.X, 4);
		real.Y = MathF.Round(real.Y, 4);

		return real;
	}

	Vector2 RealToPhoto(Vector2 real)
	{
		Vector2 photo = new Vector2();

		photo.X = (float)(1 / scale * (real.X * Math.Cos(-fi) - real.Y * Math.Sin(-fi)) + t_r2p[0]);
		photo.Y = (float)(1 / scale * (real.X * Math.Sin(-fi) + real.Y * Math.Cos(-fi)) + t_r2p[1]);
		GD.Print(photo);

		photo = ToGoodCoordinates(photo);

		GD.Print(photo);

		return photo;
	}

	Vector2 ToGoodCoordinates(Vector2 vec)
	{
		vec.Y = picture.Size.Y - vec.Y;
		return vec;
	}

	private async Task SendNextWaypointToRover(Waypoint waypoint)
	{
		var data = new MissionPlannerMessage();
		data.RequestedPosX = waypoint.Coordinates.X;
		data.RequestedPosY = waypoint.Coordinates.Y;
		data.Deadzone = waypoint.Deadzone;
		data.MessageType = MqttClasses.MissionPlannerMessageType.PointToNavigate;

		points[waypoint.Number].SetColor(Colors.Yellow);
		waypoints[waypoint.Number].SetColor(Colors.Yellow);
		await MqttNode.Singleton.EnqueueMessageAsync(LocalSettings.Singleton.Mqtt.TopicMissionPlanner, JsonSerializer.Serialize(data));
	}
}
