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

		[Export]
		public NodePath? SettingsManagerNodePath;
		[Export]
		public NodePath? ShowSettingsBtnNodePath;

		private RtspStreamClient? _rtspClient;
		private OnvifPtzCameraController? _ptzClient;
		private JoyVibrato? _joyVibrato;

		private Label? _label;
		private TextureRect? _imTextureRect;
		private ImageTexture? _imTexture;
		private UIOverlay? _uiOverlay;
		private SettingsManager? _settingsManager;
		private Button? _showSettingsBtn;

		private void StartUp()
		{
			EventLogger = new EventLogger();
			Settings = new LocalSettings();
			Settings.SaveSettings();
			PressedKeys = new PressedKeys();
			RoverCommunication = new RoverCommunication(Settings.Settings!.Mqtt);
			MissionStatus = new MissionStatus();

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
			_label = GetNode<Label>("DebugView");
			_uiOverlay = GetNode<UIOverlay>("UIOverlay");
			_settingsManager = GetNode<SettingsManager>(SettingsManagerNodePath);
			_showSettingsBtn = GetNode<Button>(ShowSettingsBtnNodePath);

			if (_ptzClient != null) PressedKeys.OnAbsoluteVectorChanged += _ptzClient.ChangeMoveVector;
			PressedKeys.OnControlModeChanged += _uiOverlay.ControlModeChangedSubscriber;
			if (_joyVibrato is not null) PressedKeys.OnControlModeChanged += _joyVibrato.ControlModeChangedSubscriber;

			_settingsManager.Target = Settings;
			_uiOverlay.ControlMode = PressedKeys.ControlMode;
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

			_showSettingsBtn!.ButtonPressed = Visible = false;
			SetProcess(false);
			if (_ptzClient != null)
			{
				if (PressedKeys != null) PressedKeys.OnAbsoluteVectorChanged -= _ptzClient.ChangeMoveVector;
				_ptzClient?.Dispose();
			}
			PressedKeys.OnControlModeChanged -= _uiOverlay!.ControlModeChangedSubscriber;
			_ptzClient = null;
			_rtspClient?.Dispose();
			_rtspClient = null;
			if(PressedKeys is not null && _joyVibrato is not null)
				PressedKeys.OnControlModeChanged -= _joyVibrato.ControlModeChangedSubscriber;
			_joyVibrato?.Dispose();
			_joyVibrato = null;
			RoverCommunication?.Dispose();
			StartUp();
			Visible = true;
			SetProcess(true);
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
		private void UpdateLabel()
		{
			var sb = new StringBuilder();
			string? age = _rtspClient?.ElapsedSecondsOnCurrentState.ToString("f2", new CultureInfo("en-US"));

			sb.AppendLine($"MQTT: Control Mode: {RoverCommunication?.RoverStatus?.ControlMode},\t" +
						  $"Connection: {RoverCommunication?.RoverStatus?.CommunicationState},\t" +
						  $"Pad connected: {RoverCommunication?.RoverStatus?.PadConnected}");
			switch (RoverCommunication?.RoverStatus?.ControlMode)
			{
				case MqttClasses.ControlMode.Rover:
					sb.AppendLine($"PressedKeys: Rover Mov: {JsonSerializer.Serialize(PressedKeys?.RoverMovement)}");
					break;
				case MqttClasses.ControlMode.Manipulator:
					sb.AppendLine($"PressedKeys: Manipulator Mov: {JsonSerializer.Serialize(PressedKeys?.ManipulatorMovement)}");
					break;
			}

			if (_rtspClient?.State == CommunicationState.Opened)
				sb.AppendLine($"RTSP: Frame is {age}s old");
			else
				sb.AppendLine($"RTSP: {_rtspClient?.State ?? CommunicationState.Closed}, Time: {age ?? "N/A "}s");

			age = _ptzClient?.ElapsedSecondsOnCurrentState.ToString("f2", new CultureInfo("en-US"));

			if (_ptzClient?.State == CommunicationState.Opened)
			{
				sb.AppendLine($"PTZ: Since last move request: {age}s");
				sb.AppendLine($"PTZ: Move vector: {_ptzClient.CameraMotion}");
			}
			else
				sb.AppendLine($"PTZ: {_ptzClient?.State ?? CommunicationState.Closed}, Time: {age ?? "N/A "}s");
			_label!.Text = sb.ToString().ReplaceLineEndings("\n");
		}
	}
}
