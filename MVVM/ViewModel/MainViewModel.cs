using System;
using System.Collections.Generic;
using System.Globalization;
using System.ServiceModel;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Godot;
using RoverControlApp.Core;
using RoverControlApp.MVVM.Model;

namespace RoverControlApp.MVVM.ViewModel
{
    public partial class MainViewModel : Control
	{
		public static MainViewModel? MainViewModelInstance { get; private set; } = null;
		public static LocalSettings Settings { get; private set; }
		public static PressedKeys? PressedKeys { get; private set; }
		public static RoverCommunication? RoverCommunication { get; private set; }
		public static MissionStatus? MissionStatus { get; private set; }
		public static MissionSetPoint? MissionSetPoint { get; private set; }

		private RtspStreamClient? _rtspClient;
		private OnvifPtzCameraController? _ptzClient;
		private JoyVibrato? _joyVibrato;
		private BackCapture _backCapture = new();

		private TextureRect? _imTextureRect;
		private ImageTexture? _imTexture;

		[Export]
		private RoverMode_UIOverlay RoverModeUIDis = null!;
		[Export]
		private Grzyb_UIOverlay GrzybUIDis = null!;
		[Export]
		private MissionStatus_UIOverlay MissionStatusUIDis = null!;

		[Export]
		private Button ShowSettingsBtn = null!, ShowVelMonitor = null!, ShowMissionControlBrn = null!;
		[Export]
		private SettingsManager SettingsManagerNode = null!;
		[Export]
		private MissionControl MissionControlNode = null!;

		[Export]
		private RichTextLabel FancyDebugViewRLab = null!;

		[Export]
		private VelMonitor VelMonitor = null!;

		private void StartUp()
		{
			Settings = LocalSettings.Singleton;

			PressedKeys = new PressedKeys();
			MissionStatus = new MissionStatus();
			RoverCommunication = new RoverCommunication(Settings!.Mqtt);
			MissionSetPoint = new MissionSetPoint();

			if (Settings.Joystick.VibrateOnModeChange)
			{
				_joyVibrato = new();
			}

			if (Settings.Camera.EnablePtzControl)
				_ptzClient = new OnvifPtzCameraController(
					Settings.Camera.ConnectionSettings.Ip,
					Settings.Camera.ConnectionSettings.PtzPort,
					Settings.Camera.ConnectionSettings.Login,
					Settings.Camera.ConnectionSettings.Password);

			if (Settings.Camera.EnableRtspStream)
				_rtspClient = new RtspStreamClient(
								Settings.Camera.ConnectionSettings.Login,
								Settings.Camera.ConnectionSettings.Password,
								Settings.Camera.ConnectionSettings.RtspStreamPath,
								Settings.Camera.ConnectionSettings.Ip,
								"rtsp",
								Settings.Camera.ConnectionSettings.RtspPort);

			_imTextureRect = GetNode<TextureRect>("CameraView");

			if (_ptzClient != null) PressedKeys.OnAbsoluteVectorChanged += _ptzClient.ChangeMoveVector;
			if (_joyVibrato is not null) PressedKeys.OnControlModeChanged += _joyVibrato.ControlModeChangedSubscriber;

			

			MissionStatus.OnRoverMissionStatusChanged += MissionControlNode!.MissionStatusUpdatedSubscriber;
			MissionControlNode.LoadSizeAndPos();
			MissionControlNode.SMissionControlVisualUpdate();

			MqttNode.Singleton.Connect(MqttNode.SignalName.MessageReceived, Callable.From<string, MqttNodeMessage>(VelMonitor.MqttSubscriber));

			//UIDis
			RoverModeUIDis.ControlMode = (int)PressedKeys.ControlMode;
			PressedKeys.OnControlModeChanged += RoverModeUIDis.ControlModeChangedSubscriber;

			MqttNode.Singleton.Connect(MqttNode.SignalName.MessageReceived, Callable.From<string, MqttNodeMessage>(GrzybUIDis.MqttSubscriber));
		
			MissionStatus.OnRoverMissionStatusChanged += MissionStatusUIDis.StatusChangeSubscriber;

			//state new mode
			_joyVibrato?.ControlModeChangedSubscriber(PressedKeys.ControlMode);

			_backCapture.HistoryLength = Settings.General.BackCaptureLength;

			
		}

		public override void _EnterTree()
		{
			SettingsManagerNode.Target = LocalSettings.Singleton;
		}

		// Called when the node enters the scene tree for the first time.
		public override void _Ready()
		{
			if (MainViewModelInstance is not null)
				throw new Exception("MainViewModel must have single instance!!!");
			MainViewModelInstance = this;

			
			StartUp();
		}

		private void VirtualRestart()
		{
			//UIDis

			ShowSettingsBtn.ButtonPressed = ShowMissionControlBrn.ButtonPressed = ShowVelMonitor.ButtonPressed = false;
			if (_ptzClient != null)
			{
				if (PressedKeys != null) PressedKeys.OnAbsoluteVectorChanged -= _ptzClient.ChangeMoveVector;
				_ptzClient?.Dispose();
			}
			PressedKeys.OnControlModeChanged -= RoverModeUIDis!.ControlModeChangedSubscriber;
			_ptzClient = null;
			_rtspClient?.Dispose();
			_rtspClient = null;
			if (PressedKeys is not null && _joyVibrato is not null)
				PressedKeys.OnControlModeChanged -= _joyVibrato.ControlModeChangedSubscriber;
			_joyVibrato?.Dispose();
			_joyVibrato = null;
			MissionStatus!.OnRoverMissionStatusChanged -= MissionControlNode!.MissionStatusUpdatedSubscriber;
			RoverCommunication?.Dispose();
			StartUp();
		}

		protected override void Dispose(bool disposing)
		{
			RoverCommunication?.Dispose();
			_rtspClient?.Dispose();
			_ptzClient?.Dispose();
			base.Dispose(disposing);
		}

		public override void _Input(InputEvent @event)
		{
			if (@event is not (InputEventKey or InputEventJoypadButton or InputEventJoypadMotion)) return;
			PressedKeys?.HandleInputEvent(@event);

			if (@event.IsActionPressed("app_backcapture_save"))
			{
				if (_backCapture.SaveHistory())
					EventLogger.LogMessage($"BackCapture INFO: Saved capture!");
				else
					EventLogger.LogMessage($"BackCapture ERROR: Save failed!");
			}
		}

		

		

		// Called every frame. 'delta' is the elapsed time since the previous frame.
		public override void _Process(double delta)
		{
			if (_rtspClient is { NewFrameSaved: true })
			{
				_backCapture.CleanUpHistory();

				_rtspClient.LockGrabbingFrames();
				if (_imTexture == null) _imTexture = ImageTexture.CreateFromImage(_rtspClient.LatestImage);
				else _imTexture.Update(_rtspClient.LatestImage);

				_backCapture.FrameFeed(_rtspClient.LatestImage);

				_rtspClient.UnLockGrabbingFrames();
				_imTextureRect!.Texture = _imTexture;
				_rtspClient.NewFrameSaved = false;
			}
			UpdateLabel();
		}

		private Color GetColorForCommunicationState(CommunicationState? state)
		{
			switch (state)
			{
				case CommunicationState.Created:
				case CommunicationState.Closed:
					return Colors.Orange;
				case CommunicationState.Opening:
				case CommunicationState.Closing:
					return Colors.Yellow;
				case CommunicationState.Faulted:
					return Colors.Red;
				case CommunicationState.Opened:
					return Colors.LightGreen;
				default:
					return Colors.Cyan;

			}
		}

		private void UpdateLabel()
		{
			FancyDebugViewRLab.Clear();

			Color mqttStatusColor = GetColorForCommunicationState(RoverCommunication?.RoverStatus?.CommunicationState);

			Color rtspStatusColor = GetColorForCommunicationState(_rtspClient?.State);

			Color ptzStatusColor = GetColorForCommunicationState(_ptzClient?.State);

			Color rtspAgeColor;
			if (_rtspClient?.ElapsedSecondsOnCurrentState < 1.0f)
				rtspAgeColor = Colors.LightGreen;
			else
				rtspAgeColor = Colors.Orange;

			string? rtspAge = _rtspClient?.ElapsedSecondsOnCurrentState.ToString("f2", new CultureInfo("en-US"));
			string? ptzAge = _ptzClient?.ElapsedSecondsOnCurrentState.ToString("f2", new CultureInfo("en-US"));

			FancyDebugViewRLab.AppendText($"MQTT: Control Mode: {RoverCommunication?.RoverStatus?.ControlMode},\t" +
						  $"Connection: [color={mqttStatusColor.ToHtml(false)}]{RoverCommunication?.RoverStatus?.CommunicationState}[/color],\t" +
						  $"Pad connected: {RoverCommunication?.RoverStatus?.PadConnected}\n");
			switch (RoverCommunication?.RoverStatus?.ControlMode)
			{
				case MqttClasses.ControlMode.Rover:
					var vecc = new Vector2((float)PressedKeys.RoverMovement.XVelAxis, (float)PressedKeys.RoverMovement.ZRotAxis);
					FancyDebugViewRLab.AppendText($"PressedKeys: Rover Mov: Vel: {vecc.Length():F3}, " +
												  $"Angle: " +
												  $"{vecc.Angle() * 180 / Mathf.Pi:F1}\n");
					break;
				case MqttClasses.ControlMode.Manipulator:
					FancyDebugViewRLab.AppendText($"PressedKeys: Manipulator Mov: {JsonSerializer.Serialize(PressedKeys?.ManipulatorMovement)}\n");
					break;
			}

			if (_rtspClient?.State == CommunicationState.Opened)
				FancyDebugViewRLab.AppendText($"RTSP: Frame is [color={rtspAgeColor.ToHtml(false)}]{rtspAge}s[/color] old\n");
			else
				FancyDebugViewRLab.AppendText($"RTSP: [color={rtspStatusColor.ToHtml(false)}]{_rtspClient?.State ?? CommunicationState.Closed}[/color], Time: {rtspAge ?? "N/A "}s\n");

			if (_ptzClient?.State == CommunicationState.Opened)
			{
				FancyDebugViewRLab.AppendText($"PTZ: Since last move request: {ptzAge}s\n");
				FancyDebugViewRLab.AppendText($"PTZ: Move vector: {_ptzClient.CameraMotion}\n");
			}
			else
				FancyDebugViewRLab.AppendText($"PTZ: [color={ptzStatusColor.ToHtml(false)}]{_ptzClient?.State ?? CommunicationState.Closed}[/color], Time: {ptzAge ?? "N/A "}s\n");
		}

		public async Task<bool> CaptureCameraImage(string subfolder = "CapturedImages", string? fileName = null, string fileExtension = "jpg")
		{
			if (_rtspClient is null)
				return false;

			Image img = new();
			_rtspClient.LockGrabbingFrames();
			if (_rtspClient.LatestImage is not null && !_rtspClient.LatestImage.IsEmpty())
				img.CopyFrom(_rtspClient.LatestImage);
			_rtspClient.UnLockGrabbingFrames();

			if (img.IsEmpty())
			{
				EventLogger.LogMessage($"CaptureCameraImage ERROR: No image to capture!");
				return false;
			}

			bool saveAsJpg;
			if (fileExtension.Equals("jpg", StringComparison.InvariantCultureIgnoreCase))
				saveAsJpg = true;
			else if (fileExtension.Equals("png", StringComparison.InvariantCultureIgnoreCase))
				saveAsJpg = false;
			else
			{
				EventLogger.LogMessage($"CaptureCameraImage ERROR: \"{fileExtension}\" is not valid extension! (png or jpg)");
				return false;
			}

			fileName ??= DateTime.Now.ToString("yyyyMMdd_hhmmss");
			string pathToFile = $"user://{subfolder}";

			if (!DirAccess.DirExistsAbsolute(pathToFile))
			{
				EventLogger.LogMessage($"CaptureCameraImage INFO: Subfolder \"{pathToFile}\" not exists yet, creating.");
				var err = DirAccess.MakeDirAbsolute(pathToFile);
				if (err != Error.Ok)
				{
					EventLogger.LogMessage($"CaptureCameraImage ERROR: Creating subfolder \"{pathToFile}\" failed. ({err.ToString()})");
					return false;
				}
			}

			pathToFile += $"/{fileName}.{fileExtension}";

			if (FileAccess.FileExists(pathToFile))
				EventLogger.LogMessage($"CaptureCameraImage WARNING: \"{pathToFile}\" already exists and will be overwrited!");

			Error imgSaveErr;
			if (saveAsJpg)
				imgSaveErr = img.SaveJpg(pathToFile);
			else
				imgSaveErr = img.SavePng(pathToFile);

			if (imgSaveErr != Error.Ok)
			{
				EventLogger.LogMessage($"CaptureCameraImage ERROR: Creating subfolder \"{pathToFile}\" failed. ({imgSaveErr.ToString()})");
				return false;
			}

			return true;
		}


		private void OnBackCapture()
		{
			if (_backCapture.SaveHistory())
				EventLogger.LogMessage($"BackCapture INFO: Saved capture!");
			else
				EventLogger.LogMessage($"BackCapture ERROR: Save failed!");
		}

		private void OnRTSPCapture()
		{
			CaptureCameraImage(subfolder: "Screenshots", fileName: DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString());
		}
	}
}
