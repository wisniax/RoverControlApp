using Godot;
using RoverControlApp.Core;
using RoverControlApp.MVVM.Model;
using System;
using System.Globalization;
using System.ServiceModel;
using System.Text.Json;
using System.Threading.Tasks;

namespace RoverControlApp.MVVM.ViewModel
{
	public partial class MainViewModel : Control
	{
		public PressedKeys PressedKeys { get; private set; }
		public RoverCommunication RoverCommunication { get; private set; }
		public MissionStatus MissionStatus { get; private set; }
		public MissionSetPoint MissionSetPoint { get; private set; }

		private WeakReference<RtspStreamClient>[]? _rtspClientWeak = new WeakReference<RtspStreamClient>[numCams];
		private WeakReference<OnvifPtzCameraController>? _ptzClientWeak;

		private RtspStreamClient[]? _rtspClient = new RtspStreamClient[numCams];
		private OnvifPtzCameraController? _ptzClient;
		private JoyVibrato _joyVibrato = new();
		private BackCapture _backCapture = new();

		private ImageTexture[]? _imTexture = new ImageTexture[numCams];

		[Export]
		private TextureRect[] imTextureRect = new TextureRect[numCams];
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

		[Export]
		private ZedMonitor ZedMonitor = null!;

		private const int numCams = 2;

		public MainViewModel()
		{
			PressedKeys = new PressedKeys();
			MissionStatus = new MissionStatus();
			RoverCommunication = new RoverCommunication(PressedKeys, MissionStatus);
			MissionSetPoint = new MissionSetPoint();
		}

		public override void _EnterTree()
		{
			SettingsManagerNode.Target = LocalSettings.Singleton;

			PressedKeys.OnControlModeChanged += RoverModeUIDis.ControlModeChangedSubscriber;
			PressedKeys.OnControlModeChanged += _joyVibrato.ControlModeChangedSubscriber;
			MissionStatus.OnRoverMissionStatusChanged += MissionStatusUIDis.StatusChangeSubscriber;
			MissionStatus.OnRoverMissionStatusChanged += MissionControlNode.MissionStatusUpdatedSubscriber;

			Task.Run(async () => await _joyVibrato.ControlModeChangedSubscriber(PressedKeys!.ControlMode));
		}

		// Called when the node enters the scene tree for the first time.
		public override void _Ready()
		{
			MissionControlNode.LoadSizeAndPos();
			MissionControlNode.SMissionControlVisualUpdate();

			RoverModeUIDis.ControlMode = (int)PressedKeys.ControlMode;

			ManagePtzStatus();
			ManageRtspStatus(0);
			ManageRtspStatus(1);

			LocalSettings.Singleton.Connect(LocalSettings.SignalName.CategoryChanged, Callable.From<StringName>(OnSettingsCategoryChanged));
			LocalSettings.Singleton.Connect(LocalSettings.SignalName.PropagatedPropertyChanged, Callable.From<StringName, StringName, Variant, Variant>(OnSettingsPropertyChanged));
		}

		public override void _ExitTree()
		{
			ShowSettingsBtn.ButtonPressed = ShowMissionControlBrn.ButtonPressed = ShowVelMonitor.ButtonPressed = false;

			PressedKeys.OnControlModeChanged -= RoverModeUIDis.ControlModeChangedSubscriber;
			PressedKeys.OnControlModeChanged -= _joyVibrato.ControlModeChangedSubscriber;
			MissionStatus.OnRoverMissionStatusChanged -= MissionStatusUIDis.StatusChangeSubscriber;
			MissionStatus.OnRoverMissionStatusChanged -= MissionControlNode.MissionStatusUpdatedSubscriber;

			LocalSettings.Singleton.Disconnect(LocalSettings.SignalName.CategoryChanged, Callable.From<StringName>(OnSettingsCategoryChanged));
			LocalSettings.Singleton.Disconnect(LocalSettings.SignalName.PropagatedPropertyChanged, Callable.From<StringName, StringName, Variant, Variant>(OnSettingsPropertyChanged));
		}

		protected override void Dispose(bool disposing)
		{
			PressedKeys.Dispose();
			RoverCommunication.Dispose();
			_rtspClient[0]?.Dispose();
			_rtspClient[1]?.Dispose();
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
					EventLogger.LogMessage("MainViewModel/BackCapture", EventLogger.LogLevel.Info, "Saved capture!");
				else
					EventLogger.LogMessage("MainViewModel/BackCapture", EventLogger.LogLevel.Error, "Save failed!");
			}
		}

		// Called every frame. 'delta' is the elapsed time since the previous frame.
		public override void _Process(double delta)
		{
			for (int i = 0; i < numCams; i++)
			{
				if (_rtspClient[i] is { NewFrameSaved: true })
				{
					_backCapture.CleanUpHistory();

					_rtspClient[i].LockGrabbingFrames();
					if (_imTexture[i] == null) _imTexture[i] = ImageTexture.CreateFromImage(_rtspClient[i].LatestImage);
					else _imTexture[i].Update(_rtspClient[i].LatestImage);

					_backCapture.FrameFeed(_rtspClient[i].LatestImage);

					_rtspClient[i].UnLockGrabbingFrames();
					if (_rtspClient[i].isBig)
					{
						imTextureRect[0].Texture = _imTexture[i];
					}
					else
					{
						imTextureRect[1].Texture = _imTexture[i];
					}
					_rtspClient[i].MarkFrameOld();
				}
			}

			UpdateLabel();
		}

		/*
		 * Settings event handlers
		 */

		void OnSettingsCategoryChanged(StringName property)
		{
			switch (property)
			{
				case nameof(LocalSettings.Camera0):
					ManagePtzStatus();
					ManageRtspStatus(0);
					break;
				case nameof(LocalSettings.Camera1):
					ManageRtspStatus(1);
					break;
			}
		}

		void OnSettingsPropertyChanged(StringName category, StringName name, Variant oldValue, Variant newValue)
		{
			switch (category)
			{
				case nameof(LocalSettings.Camera0):
					switch (name)
					{
						case nameof(LocalSettings.Camera0.EnablePtzControl):
							ManagePtzStatus();
							break;
						case nameof(LocalSettings.Camera0.EnableRtspStream):
							ManageRtspStatus(0);
							break;
					}
					break;
				case nameof(LocalSettings.Camera1):
					switch (name)
					{
						case nameof(LocalSettings.Camera1.EnableRtspStream):
							ManageRtspStatus(1);
							break;
					}
					break;
			}
			
		}

		/*
		 * settings handlers end
		 */

		void CameraChange()
		{
			foreach (var client in _rtspClient)
			{
				client.isBig = !client.isBig;
			}
		}


		private void ManageRtspStatus(int id)
		{
			Core.Settings.Camera? cam = id switch
			{
				0 => LocalSettings.Singleton.Camera0,
				1 => LocalSettings.Singleton.Camera1,
				_ => null
			};

			switch (cam.EnableRtspStream)
			{
				case true when _rtspClient[id] is null:
					_rtspClient[id] = new(id);
					_rtspClientWeak[id] = new(_rtspClient[id]);
					break;
				case false when _rtspClient[id] is not null:
					_rtspClient[id].Dispose();
					_rtspClient[id] = null;
					break;
			}
		}

		private void ManagePtzStatus()
		{
			switch (LocalSettings.Singleton.Camera0.EnablePtzControl)
			{
				case true when _ptzClient is null:
					_ptzClient = new OnvifPtzCameraController();
					_ptzClientWeak = new(_ptzClient);
					PressedKeys.OnAbsoluteVectorChanged += _ptzClient.ChangeMoveVector;
					break;
				case false when _ptzClient is not null:
					PressedKeys.OnAbsoluteVectorChanged -= _ptzClient.ChangeMoveVector;
					_ptzClient.Dispose();
					_ptzClient = null;
					break;
			}
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

			RtspStreamClient[]? rtspClient = new RtspStreamClient[numCams];
			OnvifPtzCameraController? ptzClient = null;

			_rtspClientWeak[0]?.TryGetTarget(out rtspClient[0]);
			_rtspClientWeak[1]?.TryGetTarget(out rtspClient[1]);
			_ptzClientWeak?.TryGetTarget(out ptzClient);

			Color mqttStatusColor = GetColorForCommunicationState(RoverCommunication?.RoverStatus?.CommunicationState);

			Color[] rtspStatusColor = new Color[numCams];

			for (int i = 0; i < numCams; i++)
			{
				rtspStatusColor[i] = GetColorForCommunicationState(rtspClient[i]?.State);
			}


			Color ptzStatusColor = GetColorForCommunicationState(ptzClient?.State);

			Color[] rtspAgeColor = new Color[numCams];

			for (int i = 0; i < numCams; i++)
			{
				if (rtspClient[i]?.ElapsedSecondsOnCurrentState < 1.0f)
					rtspAgeColor[i] = Colors.LightGreen;
				else
					rtspAgeColor[i] = Colors.Orange;
			}

			string[]? rtspAge = new string[numCams];

			for (int i = 0; i < numCams; i++)
			{
				rtspAge[i] = rtspClient[i]?.ElapsedSecondsOnCurrentState.ToString("f2", new CultureInfo("en-US"));
			}

			string? ptzAge = ptzClient?.ElapsedSecondsOnCurrentState.ToString("f2", new CultureInfo("en-US"));

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

			for (int i = 0; i < numCams; i++)
			{
				if (rtspClient[i]?.State == CommunicationState.Opened)
					FancyDebugViewRLab.AppendText($"RTSP: Frame is [color={rtspAgeColor[i].ToHtml(false)}]{rtspAge[i]}s[/color] old\n");
				else
					FancyDebugViewRLab.AppendText($"RTSP: [color={rtspStatusColor[i].ToHtml(false)}]{rtspClient[i]?.State ?? CommunicationState.Closed}[/color], Time: {rtspAge[i] ?? "N/A "}s\n");
			}

			if (ptzClient?.State == CommunicationState.Opened)
			{
				FancyDebugViewRLab.AppendText($"PTZ: Since last move request: {ptzAge}s\n");
				FancyDebugViewRLab.AppendText($"PTZ: Move vector: {ptzClient.CameraMotion}\n");
			}
			else
				FancyDebugViewRLab.AppendText($"PTZ: [color={ptzStatusColor.ToHtml(false)}]{ptzClient?.State ?? CommunicationState.Closed}[/color], Time: {ptzAge ?? "N/A "}s\n");
		}

		public async Task<bool> CaptureCameraImage(string subfolder = "CapturedImages", string? fileName = null, string fileExtension = "jpg")
		{
			if (_rtspClient is null)
				return false;

			Image img = new();
			_rtspClient[0].LockGrabbingFrames();
			if (_rtspClient[0].LatestImage is not null && !_rtspClient[0].LatestImage.IsEmpty())
				img.CopyFrom(_rtspClient[0].LatestImage);
			_rtspClient[0].UnLockGrabbingFrames();

			if (img.IsEmpty())
			{
				EventLogger.LogMessage("MainViewModel/CaptureCameraImage", EventLogger.LogLevel.Error, $"No image to capture!");
				return false;
			}

			bool saveAsJpg;
			if (fileExtension.Equals("jpg", StringComparison.InvariantCultureIgnoreCase))
				saveAsJpg = true;
			else if (fileExtension.Equals("png", StringComparison.InvariantCultureIgnoreCase))
				saveAsJpg = false;
			else
			{
				EventLogger.LogMessage("MainViewModel/CaptureCameraImage", EventLogger.LogLevel.Error, $"\"{fileExtension}\" is not valid extension! (png or jpg)");
				return false;
			}

			fileName ??= DateTime.Now.ToString("yyyyMMdd_hhmmss");
			string pathToFile = $"user://{subfolder}";

			if (!DirAccess.DirExistsAbsolute(pathToFile))
			{
				EventLogger.LogMessage("MainViewModel/CaptureCameraImage", EventLogger.LogLevel.Info, $"Subfolder \"{pathToFile}\" not exists yet, creating.");
				var err = DirAccess.MakeDirAbsolute(pathToFile);
				if (err != Error.Ok)
				{
					EventLogger.LogMessage("MainViewModel/CaptureCameraImage", EventLogger.LogLevel.Error, $"Creating subfolder \"{pathToFile}\" failed. ({err})");
					return false;
				}
			}

			pathToFile += $"/{fileName}.{fileExtension}";

			if (FileAccess.FileExists(pathToFile))
				EventLogger.LogMessage("MainViewModel/CaptureCameraImage", EventLogger.LogLevel.Warning, $"\"{pathToFile}\" already exists and will be overwrited!");

			Error imgSaveErr;
			if (saveAsJpg)
				imgSaveErr = img.SaveJpg(pathToFile);
			else
				imgSaveErr = img.SavePng(pathToFile);

			if (imgSaveErr != Error.Ok)
			{
				EventLogger.LogMessage("MainViewModel/CaptureCameraImage", EventLogger.LogLevel.Error, $"Creating subfolder \"{pathToFile}\" failed. ({imgSaveErr.ToString()})");
				return false;
			}

			return true;
		}


		private void OnBackCapture()
		{
			if (_backCapture.SaveHistory())
				EventLogger.LogMessage("MainViewModel/BackCapture", EventLogger.LogLevel.Info, "Saved capture!");
			else
				EventLogger.LogMessage("MainViewModel/BackCapture", EventLogger.LogLevel.Error, "Save capture failed!");
		}

		private void OnRTSPCapture()
		{
			CaptureCameraImage(subfolder: "Screenshots", fileName: DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString());
		}
	}
}
