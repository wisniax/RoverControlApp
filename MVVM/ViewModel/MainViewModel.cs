using System;
using System.Globalization;
using System.ServiceModel;
using System.Text;
using System.Text.Json;
using System.Threading;
using Godot;
using RoverControlApp.Core;
using RoverControlApp.MVVM.Model;

namespace RoverControlApp.MVVM.ViewModel
{
	public partial class MainViewModel : Control
	{
		public static EventLogger? EventLogger { get; private set; }
		public static LocalSettings? Settings { get; private set; }
		public static PressedKeys? PressedKeys { get; private set; }
		public static RoverCommunication? RoverCommunication { get; private set; }
		public static MissionStatus? MissionStatus { get; private set; }
		public static MqttClient? MqttClient { get; private set; }
		public static MissionSetPoint? MissionSetPoint { get; private set; }

		private RtspStreamClient? _rtspClient;
		private OnvifPtzCameraController? _ptzClient;
		private JoyVibrato? _joyVibrato;

		private TextureRect? _imTextureRect;
		private ImageTexture? _imTexture;

		[Export]
		private UIOverlay UIOverlayNode = null!;

		[Export]
		private Button ShowSettingsBtn = null!, ShowMissionControlBrn = null!;
		[Export]
		private SettingsManager SettingsManagerNode = null!;
		[Export]
		private MissionControl MissionControlNode = null!;

		[Export]
		private RichTextLabel FancyDebugViewRLab = null!;

		private void StartUp()
		{
			EventLogger = new EventLogger();
			Settings = new LocalSettings();
			Settings.SaveSettings();
			MqttClient = new MqttClient(Settings.Settings!.Mqtt);
			PressedKeys = new PressedKeys();
			MissionStatus = new MissionStatus();
			RoverCommunication = new RoverCommunication(Settings.Settings!.Mqtt);
			MissionSetPoint = new MissionSetPoint();

			if (Settings.Settings.JoyVibrateOnModeChange)
			{
				_joyVibrato = new();
			}

			if (Settings.Settings.Camera.EnablePtzControl)
				_ptzClient = new OnvifPtzCameraController(
					Settings.Settings.Camera.Ip,
					Settings.Settings.Camera.PtzPort,
					Settings.Settings.Camera.Login,
					Settings.Settings.Camera.Password);

			if (Settings.Settings.Camera.EnableRtspStream)
				_rtspClient = new RtspStreamClient(
								Settings.Settings.Camera.Login,
								Settings.Settings.Camera.Password,
								Settings.Settings.Camera.RtspStreamPath,
								Settings.Settings.Camera.Ip,
								"rtsp",
								Settings.Settings.Camera.RtspPort);

			_imTextureRect = GetNode<TextureRect>("CameraView");

			if (_ptzClient != null) PressedKeys.OnAbsoluteVectorChanged += _ptzClient.ChangeMoveVector;
			PressedKeys.OnControlModeChanged += UIOverlayNode.ControlModeChangedSubscriber;
			if (_joyVibrato is not null) PressedKeys.OnControlModeChanged += _joyVibrato.ControlModeChangedSubscriber;

			SettingsManagerNode.Target = Settings;
			MissionStatus.OnRoverMissionStatusChanged += MissionControlNode!.MissionStatusUpdatedSubscriber;
			MissionControlNode.LoadSizeAndPos();
			MissionControlNode.SMissionControlVisualUpdate();

			UIOverlayNode.ControlMode = PressedKeys.ControlMode;
			//state new mode
			_joyVibrato?.ControlModeChangedSubscriber(PressedKeys.ControlMode);
		}

		// Called when the node enters the scene tree for the first time.
		public override void _Ready()
		{
			Thread.CurrentThread.Name = "MainUI_Thread";
			StartUp();
		}

		private void VirtualRestart()
		{
			ShowSettingsBtn.ButtonPressed = ShowMissionControlBrn.ButtonPressed = false;
			if (_ptzClient != null)
			{
				if (PressedKeys != null) PressedKeys.OnAbsoluteVectorChanged -= _ptzClient.ChangeMoveVector;
				_ptzClient?.Dispose();
			}
			PressedKeys.OnControlModeChanged -= UIOverlayNode!.ControlModeChangedSubscriber;
			_ptzClient = null;
			_rtspClient?.Dispose();
			_rtspClient = null;
			if (PressedKeys is not null && _joyVibrato is not null)
				PressedKeys.OnControlModeChanged -= _joyVibrato.ControlModeChangedSubscriber;
			_joyVibrato?.Dispose();
			_joyVibrato = null;
			MissionStatus!.OnRoverMissionStatusChanged -= MissionControlNode!.MissionStatusUpdatedSubscriber;
			RoverCommunication?.Dispose();
			MqttClient?.Dispose();
			StartUp();
		}

		protected override void Dispose(bool disposing)
		{
			RoverCommunication?.Dispose();
			MqttClient?.Dispose();
			_rtspClient?.Dispose();
			_ptzClient?.Dispose();
			base.Dispose(disposing);
		}

		public override void _Input(InputEvent @event)
		{
			if (@event is not (InputEventKey or InputEventJoypadButton or InputEventJoypadMotion)) return;
			PressedKeys?.HandleInputEvent(@event);
		}

		// Called every frame. 'delta' is the elapsed time since the previous frame.
		public override void _Process(double delta)
		{
			if (_rtspClient is { NewFrameSaved: true })
			{
				_rtspClient.LockGrabbingFrames();
				if (_imTexture == null) _imTexture = ImageTexture.CreateFromImage(_rtspClient.LatestImage);
				else _imTexture.Update(_rtspClient.LatestImage);
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
			if(_rtspClient?.ElapsedSecondsOnCurrentState < 1.0f)
				rtspAgeColor= Colors.LightGreen; 
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
					FancyDebugViewRLab.AppendText($"PressedKeys: Rover Mov: {JsonSerializer.Serialize(PressedKeys?.RoverMovement)}\n");
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
	}
}
