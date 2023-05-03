using System.Globalization;
using System.ServiceModel;
using System.Text;
using System.Threading;
using Godot;
using RoverControlApp.Core;
using RoverControlApp.MVVM.Model;

namespace RoverControlApp.MVVM.ViewModel
{
	public partial class MainViewModel : Control
	{
		public static EventLogger EventLogger { get; private set; }
		public static LocalSettings Settings { get; private set; }
		public static PressedKeys PressedKeys { get; private set; }

		private RtspStreamClient _rtspClient;

		private Label _label;
		private TextureRect _imTextureRect;
		private ImageTexture? _imTexture;

		// Called when the node enters the scene tree for the first time.
		public override void _Ready()
		{
			Thread.CurrentThread.Name = "MainUI_Thread";
			EventLogger = new EventLogger();
			Settings = new LocalSettings();
			Settings.SaveSettings();
			PressedKeys = new PressedKeys();

			_rtspClient = new RtspStreamClient(
				Settings.Settings.CameraLogin,
				Settings.Settings.CameraPassword,
				Settings.Settings.CameraRtspStreamPath,
				Settings.Settings.CameraIp,
				"rtsp",
				Settings.Settings.CameraRtspPort);

			_imTextureRect = GetNode<TextureRect>("CameraView");
			_label = GetNode<Label>("DebugView");
		}

		protected override void Dispose(bool disposing)
		{
			base.Dispose(disposing);
		}

		public override void _Input(InputEvent @event)
		{
			if (@event is not (InputEventKey or InputEventJoypadButton or InputEventJoypadMotion)) return;
			PressedKeys.HandleInputEvent();
		}

		private void UpdateLabel()
		{
			var sb = new StringBuilder();
			sb.AppendLine($"RTSP connection: {_rtspClient.State}, Time: " +
			              $"{_rtspClient.ElapsedSecondsOnCurrentState.ToString("f2", new CultureInfo("en-US"))}s");

			_label.Text = sb.ToString();
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
	}
}