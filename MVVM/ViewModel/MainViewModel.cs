using System.Globalization;
using System.ServiceModel;
using System.Text;
using System.Threading;
using Godot;
using Onvif.Core.Client.Ptz;
using RoverControlApp.Core;
using RoverControlApp.MVVM.Model;

namespace RoverControlApp.MVVM.ViewModel
{
	public partial class MainViewModel : Control
	{
		public static EventLogger EventLogger { get; private set; }
		public static LocalSettings Settings { get; private set; }
		public static PressedKeys PressedKeys { get; private set; }
		public static RoverCommunication RoverCommunication { get; private set; }

		private RtspStreamClient _rtspClient;
		private OnvifPtzCameraController _ptzClient;

		private Label _label;
		private TextureRect _imTextureRect;
		private ImageTexture? _imTexture;

		// Called when the node enters the scene tree for the first time.
		public override async void _Ready()
		{
			Thread.CurrentThread.Name = "MainUI_Thread";
			EventLogger = new EventLogger();
			Settings = new LocalSettings();
			Settings.SaveSettings();
			PressedKeys = new PressedKeys();

			_ptzClient = new OnvifPtzCameraController(
				Settings.Settings.LocalSettingsCamera.CameraIp,
				Settings.Settings.LocalSettingsCamera.CameraPtzPort,
				Settings.Settings.LocalSettingsCamera.CameraLogin,
				Settings.Settings.LocalSettingsCamera.CameraPassword);

			_rtspClient = new RtspStreamClient(
				Settings.Settings.LocalSettingsCamera.CameraLogin,
				Settings.Settings.LocalSettingsCamera.CameraPassword,
				Settings.Settings.LocalSettingsCamera.CameraRtspStreamPath,
				Settings.Settings.LocalSettingsCamera.CameraIp,
				"rtsp",
				Settings.Settings.LocalSettingsCamera.CameraRtspPort);

			RoverCommunication = new RoverCommunication();
			await RoverCommunication.Connect_Client();

			_imTextureRect = GetNode<TextureRect>("CameraView");
			_label = GetNode<Label>("DebugView");
		}

		protected override void Dispose(bool disposing)
		{
			_rtspClient.Dispose();
			_rtspClient = null;
			RoverCommunication.StopClient().Wait(1000);
			RoverCommunication.Dispose();
			base.Dispose(disposing);
		}

		public override void _Input(InputEvent @event)
		{
			if (@event is not (InputEventKey or InputEventJoypadButton or InputEventJoypadMotion)) return;
			PressedKeys.HandleInputEvent();
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
				_imTextureRect.Texture = _imTexture;
				_rtspClient.NewFrameSaved = false;
			}
			UpdateLabel();
		}
		private void UpdateLabel()
		{
			var sb = new StringBuilder();
			string age = _rtspClient.ElapsedSecondsOnCurrentState.ToString("f2", new CultureInfo("en-US"));

			sb.AppendLine($"MQTT: Rover Mov: {PressedKeys.RoverMovementVector}");

			if (_rtspClient.State == CommunicationState.Opened)
				sb.AppendLine($"RTSP: Frame is {age}s old");
			else
				sb.AppendLine($"RTSP: {_rtspClient.State}, Time: {age}s");

			age = _ptzClient.ElapsedSecondsOnCurrentState.ToString("f2", new CultureInfo("en-US"));

			if (_ptzClient.State == CommunicationState.Opened)
			{
				sb.AppendLine($"PTZ: Since last move request: {age}s");
				sb.AppendLine($"PTZ: Move vector: {_ptzClient.CameraMotion}");
			}
			else
				sb.AppendLine($"PTZ: {_ptzClient.State}, Time: {age}s");
			_label.Text = sb.ToString().ReplaceLineEndings("\n");
		}
	}
}
