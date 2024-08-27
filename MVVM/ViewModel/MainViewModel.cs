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

		private WeakReference<RtspStreamClient>? _rtspClientWeak;
		private WeakReference<OnvifPtzCameraController>? _ptzClientWeak;

		private static int MaxCams = 6;

		private RtspStreamClient?[] _rtspClient = new RtspStreamClient?[MaxCams];
		private OnvifPtzCameraController? _ptzClient;
		private JoyVibrato _joyVibrato = new();
		private BackCapture _backCapture = new();

		private ImageTexture?[] _imTexture = new ImageTexture?[MaxCams];

		[Export]
		private TextureRect[] imTextureRect = null!;
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

			ManagePtzStatus(LocalSettings.Singleton.Camera0);
			ManageRtspStatus(LocalSettings.Singleton.Camera0, 0);
			ManageRtspStatus(LocalSettings.Singleton.Camera1, 1);
			ManageRtspStatus(LocalSettings.Singleton.Camera2, 2);
			ManageRtspStatus(LocalSettings.Singleton.Camera3, 3);
			ManageRtspStatus(LocalSettings.Singleton.Camera4, 4);
			ManageRtspStatus(LocalSettings.Singleton.Camera5, 5);

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
			foreach (var client in _rtspClient)
			{
				client?.Dispose();
			}
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
			UpdateLabel();
		}

		void ChangeCamera(int id)
		{
			if (_rtspClient[id] is null)
				return;

			for (int i = 0; i < MaxCams; i++)
			{
				if (_rtspClient[i].isHD)
				{
					_rtspClient[i].isHD = false;
					imTextureRect[i].Visible = true;
					//_rtspClient[i].RestartCapture();
				}
			}


			imTextureRect[id].Visible = false;
			_rtspClient[id].isHD = true;

			//_rtspClient[id].RestartCapture();
			
			GetNode<Label>("CameraViewMain0/Label").Text = $"Camera {id} HD";
		}

		void RTSPworkHandler(int id)
		{
			CallDeferred("RTSPwork", id);
		}

		void RTSPwork(int id)
		{
			_backCapture.CleanUpHistory();

			_rtspClient[id].LockGrabbingFrames();
			if (_imTexture[id] == null) _imTexture[id] = ImageTexture.CreateFromImage(_rtspClient[id].LatestImage);
			else _imTexture[id].Update(_rtspClient[id].LatestImage);

			_backCapture.FrameFeed(_rtspClient[id].LatestImage);

			_rtspClient[id].UnLockGrabbingFrames();
			if (!_rtspClient[id].isHD)
				imTextureRect[id].Texture = _imTexture[id];
			else
				imTextureRect[6].Texture = _imTexture[id];
			_rtspClient[id].MarkFrameOld();
		}

		/*
		 * Settings event handlers
		 */

		void OnSettingsCategoryChanged(StringName property)
		{
			switch (property)
			{
				case nameof(LocalSettings.Camera0):
					ManagePtzStatus(LocalSettings.Singleton.Camera0);
					ManageRtspStatus(LocalSettings.Singleton.Camera0, 1); 
					break;
				case nameof(LocalSettings.Camera1):
					ManageRtspStatus(LocalSettings.Singleton.Camera1, 1);
					break;
				case nameof(LocalSettings.Camera2):
					ManageRtspStatus(LocalSettings.Singleton.Camera2, 2);
					break;
				case nameof(LocalSettings.Camera3):
					ManageRtspStatus(LocalSettings.Singleton.Camera3, 3);
					break;
				case nameof(LocalSettings.Camera4):
					ManageRtspStatus(LocalSettings.Singleton.Camera4, 4);
					break;
				case nameof(LocalSettings.Camera5):
					ManageRtspStatus(LocalSettings.Singleton.Camera5, 5);
					break;
				default:
					break;
			}
		}

		void OnSettingsPropertyChanged(StringName category, StringName name, Variant oldValue, Variant newValue)
		{
			if (category == nameof(LocalSettings.Camera0) && name == nameof(LocalSettings.Camera0.EnablePtzControl))
			{
				ManagePtzStatus(LocalSettings.Singleton.Camera0);
			}

			if (name == nameof(LocalSettings.Camera1.EnableRtspStream))
			{
				switch (category)
				{
					case nameof(LocalSettings.Camera0):
						ManageRtspStatus(LocalSettings.Singleton.Camera0, 0);
						break;
					case nameof(LocalSettings.Camera1):
						ManageRtspStatus(LocalSettings.Singleton.Camera1, 1);
						break;
					case nameof(LocalSettings.Camera2):
						ManageRtspStatus(LocalSettings.Singleton.Camera2, 2);
						break;
					case nameof(LocalSettings.Camera3):
						ManageRtspStatus(LocalSettings.Singleton.Camera3, 3);
						break;
					case nameof(LocalSettings.Camera4):
						ManageRtspStatus(LocalSettings.Singleton.Camera4, 4);
						break;
					case nameof(LocalSettings.Camera5):
						ManageRtspStatus(LocalSettings.Singleton.Camera5, 5);
						break;
					default: 
						break;
				}
			}
		}

		/*
		 * settings handlers end
		 */

		private void ManageRtspStatus(Core.Settings.Camera camera, int id)
		{
			switch (camera.EnableRtspStream)
			{
				case true when _rtspClient[id] is null:
					_rtspClient[id] = new(id);
					_rtspClientWeak = new(_rtspClient[id]);
					_rtspClient[id].FrameReceived += RTSPworkHandler;
					break;
				case false when _rtspClient[id] is not null:
					_rtspClient[id].Dispose();
					_rtspClient[id] = null;
					break;
			}
		}

		private void ManagePtzStatus(Core.Settings.Camera camera)
		{
			switch (camera.EnablePtzControl)
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

			RtspStreamClient? rtspClient = null;
			OnvifPtzCameraController? ptzClient = null;

			_rtspClientWeak?.TryGetTarget(out rtspClient);
			_ptzClientWeak?.TryGetTarget(out ptzClient);

			Color mqttStatusColor = GetColorForCommunicationState(RoverCommunication?.RoverStatus?.CommunicationState);

			Color rtspStatusColor = GetColorForCommunicationState(rtspClient?.State);

			Color ptzStatusColor = GetColorForCommunicationState(ptzClient?.State);

			Color rtspAgeColor;
			if (rtspClient?.ElapsedSecondsOnCurrentState < 1.0f)
				rtspAgeColor = Colors.LightGreen;
			else
				rtspAgeColor = Colors.Orange;

			string? rtspAge = rtspClient?.ElapsedSecondsOnCurrentState.ToString("f2", new CultureInfo("en-US"));
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

			if (rtspClient?.State == CommunicationState.Opened)
				FancyDebugViewRLab.AppendText($"RTSP: Frame is [color={rtspAgeColor.ToHtml(false)}]{rtspAge}s[/color] old\n");
			else
				FancyDebugViewRLab.AppendText($"RTSP: [color={rtspStatusColor.ToHtml(false)}]{rtspClient?.State ?? CommunicationState.Closed}[/color], Time: {rtspAge ?? "N/A "}s\n");

			if (ptzClient?.State == CommunicationState.Opened)
			{
				FancyDebugViewRLab.AppendText($"PTZ: Since last move request: {ptzAge}s\n");
				FancyDebugViewRLab.AppendText($"PTZ: Move vector: {ptzClient.CameraMotion}\n");
			}
			else
				FancyDebugViewRLab.AppendText($"PTZ: [color={ptzStatusColor.ToHtml(false)}]{ptzClient?.State ?? CommunicationState.Closed}[/color], Time: {ptzAge ?? "N/A "}s\n");
		}

		public async Task<bool> CaptureCameraImage(int activeCam, string subfolder = "CapturedImages", string? fileName = null, string fileExtension = "jpg")
		{
			if (_rtspClient is null)
				return false;

			Image img = new();

			for (int i = 0; i < MaxCams; i++)
			{
				_rtspClient[i].LockGrabbingFrames();
				if (_rtspClient[i].LatestImage is not null && !_rtspClient[i].LatestImage.IsEmpty())
					img.CopyFrom(_rtspClient[i].LatestImage);
				_rtspClient[i].UnLockGrabbingFrames();

			}

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
			CaptureCameraImage(0, subfolder: "Screenshots", fileName: DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString());
		}
	}
}
