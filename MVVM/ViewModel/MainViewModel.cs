using Godot;
using RoverControlApp.Core;
using RoverControlApp.MVVM.Model;
using System;
using System.Diagnostics;
using System.Globalization;
using System.ServiceModel;
using System.Text.Json;
using System.Threading.Tasks;
using RoverControlApp.Core.Settings;

namespace RoverControlApp.MVVM.ViewModel
{
	public partial class MainViewModel : Control
	{
		public PressedKeys PressedKeys { get; private set; }
		public RoverCommunication RoverCommunication { get; private set; }
		public MissionStatus MissionStatus { get; private set; }
		public MissionSetPoint MissionSetPoint { get; private set; }

		private WeakReference<RtspStreamClient>?[] _rtspClientWeak = new WeakReference<RtspStreamClient>?[MaxCams];
		private WeakReference<OnvifPtzCameraController>? _ptzClientWeak;

		private static int MaxCams = 6;

		private bool _rtspHidden = true;
		private bool _ptzHidden = true;

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
		private Button ShowSettingsBtn = null!, ShowVelMonitor = null!, ShowMissionControlBrn = null!, ShowPTZflipper = null!, ShowRTSPflipper = null!;
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

			ManagePtzStatus();


			ManageRtspStatus(LocalSettings.Singleton.AllCameras.Camera0, 0);
			ManageRtspStatus(LocalSettings.Singleton.AllCameras.Camera1, 1);
			ManageRtspStatus(LocalSettings.Singleton.AllCameras.Camera2, 2);
			ManageRtspStatus(LocalSettings.Singleton.AllCameras.Camera3, 3);
			ManageRtspStatus(LocalSettings.Singleton.AllCameras.Camera4, 4);
			ManageRtspStatus(LocalSettings.Singleton.AllCameras.Camera5, 5);


			LocalSettings.Singleton.Connect(LocalSettings.SignalName.CategoryChanged, Callable.From<StringName>(OnSettingsCategoryChanged));
			LocalSettings.Singleton.Connect(LocalSettings.SignalName.PropagatedPropertyChanged, Callable.From<StringName, StringName, Variant, Variant>(OnSettingsPropertyChanged));
			LocalSettings.Singleton.Connect(LocalSettings.SignalName.PropagatedSubcategoryChanged, Callable.From<StringName, StringName, Variant, Variant>(OnSettingsSubcategoryChanged));
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
			for (int i = 0; i < MaxCams; i++)
			{
				if (_rtspClient[i] == null)
					imTextureRect[i].Visible = false;
				else
					imTextureRect[i].Visible = !_rtspClient[i].isHD;
			}
		}

		void ChangeCamera(int id)
		{
			if (_rtspClient[id] is null)
				return;

			for (int i = 0; i < MaxCams; i++)
			{
				if (_rtspClient[i] != null)
				{
					if (_rtspClient[i].isHD)
					{
						_rtspClient[i].isHD = false;
						if (!LocalSettings.Singleton.General.sdOnlyMode)
						{
							_rtspClient[i].UpdateConnectionSettings();
							_rtspClient[i].SetStateClosing();
						}
						imTextureRect[i].Visible = true;
					}
				}
			}


			imTextureRect[id].Visible = false;
			_rtspClient[id].isHD = true;
			if (!LocalSettings.Singleton.General.sdOnlyMode)
			{
				_rtspClient[id].UpdateConnectionSettings();
				_rtspClient[id].SetStateClosing();
			}
			
			GetNode<Label>("CameraViewMain0/Label").Visible = true;
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
			if (_imTexture[id] == null || (_imTexture[id].GetWidth() != _rtspClient[id].LatestImage.GetWidth() || 
			                               _imTexture[id].GetHeight() != _rtspClient[id].LatestImage.GetHeight())) 
				_imTexture[id] = ImageTexture.CreateFromImage(_rtspClient[id].LatestImage);
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


		void OnSettingsSubcategoryChanged(StringName category, StringName name, Variant oldValue, Variant newValue)
		{
			if (category == nameof(LocalSettings.AllCameras))
			{
				switch (name)
				{
					case nameof(AllCameras.Camera0):
						ManagePtzStatus();
						ManageRtspStatus(LocalSettings.Singleton.AllCameras.Camera0, 0);
						if (_rtspClient[0] != null)
						{
							_rtspClient[0].UpdateConnectionSettings();
							_rtspClient[0].SetStateClosing();
						}
						break;
					case nameof(AllCameras.Camera1):
						ManageRtspStatus(LocalSettings.Singleton.AllCameras.Camera1, 1);
						if (_rtspClient[1] != null)
						{
							_rtspClient[1].UpdateConnectionSettings();
							_rtspClient[1].SetStateClosing();
						}
						break;
					case nameof(AllCameras.Camera2):
						ManageRtspStatus(LocalSettings.Singleton.AllCameras.Camera2, 2);
						if (_rtspClient[2] != null)
						{
							_rtspClient[2].UpdateConnectionSettings();
							_rtspClient[2].SetStateClosing();
						}
						break;
					case nameof(AllCameras.Camera3):
						ManageRtspStatus(LocalSettings.Singleton.AllCameras.Camera3, 3);
						if (_rtspClient[3] != null)
						{
							_rtspClient[3].UpdateConnectionSettings();
							_rtspClient[3].SetStateClosing();
						}
						break;
					case nameof(AllCameras.Camera4):
						ManageRtspStatus(LocalSettings.Singleton.AllCameras.Camera4, 4);
						if (_rtspClient[4] != null)
						{
							_rtspClient[4].UpdateConnectionSettings();
							_rtspClient[4].SetStateClosing();
						}
						break;
					case nameof(AllCameras.Camera5):
						ManageRtspStatus(LocalSettings.Singleton.AllCameras.Camera5, 5);
						if (_rtspClient[5] != null)
						{
							_rtspClient[5].UpdateConnectionSettings();
							_rtspClient[5].SetStateClosing();
						}
						break;
					default:
						break;
				}
			}
			return;
		}

		void OnSettingsCategoryChanged(StringName property)
		{
			switch (property)
			{
				case nameof(LocalSettings.AllCameras.Camera0):
					ManagePtzStatus();
					ManageRtspStatus(LocalSettings.Singleton.AllCameras.Camera0, 0); 
					_rtspClient[0].UpdateConnectionSettings();
					break;
				case nameof(LocalSettings.AllCameras.Camera1):
					ManageRtspStatus(LocalSettings.Singleton.AllCameras.Camera1, 1);
					_rtspClient[1].UpdateConnectionSettings();
					break;
				case nameof(LocalSettings.AllCameras.Camera2):
					ManageRtspStatus(LocalSettings.Singleton.AllCameras.Camera2, 2);
					_rtspClient[2].UpdateConnectionSettings();
					break;
				case nameof(LocalSettings.AllCameras.Camera3):
					ManageRtspStatus(LocalSettings.Singleton.AllCameras.Camera3, 3);
					_rtspClient[3].UpdateConnectionSettings();
					break;
				case nameof(LocalSettings.AllCameras.Camera4):
					ManageRtspStatus(LocalSettings.Singleton.AllCameras.Camera4, 4);
					_rtspClient[4].UpdateConnectionSettings();
					break;
				case nameof(LocalSettings.AllCameras.Camera5):
					ManageRtspStatus(LocalSettings.Singleton.AllCameras.Camera5, 5);
					_rtspClient[5].UpdateConnectionSettings();
					break;
				default:
					break;
			}
		}

		void OnSettingsPropertyChanged(StringName category, StringName name, Variant oldValue, Variant newValue)
		{
			if (name == nameof(LocalSettings.AllCameras.Camera0.EnablePtzControl))
			{
				switch (category)
				{
					case nameof(LocalSettings.AllCameras.Camera0):
						ManagePtzStatus();
						break;
					default:
						break;
				}
			}

			if (name == nameof(LocalSettings.AllCameras.Camera1.EnableRtspStream))
			{
				switch (category)
				{
					case nameof(LocalSettings.AllCameras.Camera0):
						ManageRtspStatus(LocalSettings.Singleton.AllCameras.Camera0, 0);
						_rtspClient[0].UpdateConnectionSettings();
						break;
					case nameof(LocalSettings.AllCameras.Camera1):
						ManageRtspStatus(LocalSettings.Singleton.AllCameras.Camera1, 1);
						_rtspClient[1].UpdateConnectionSettings();
						break;
					case nameof(LocalSettings.AllCameras.Camera2):
						ManageRtspStatus(LocalSettings.Singleton.AllCameras.Camera2, 2);
						_rtspClient[2].UpdateConnectionSettings();
						break;
					case nameof(LocalSettings.AllCameras.Camera3):
						ManageRtspStatus(LocalSettings.Singleton.AllCameras.Camera3, 3);
						_rtspClient[3].UpdateConnectionSettings();
						break;
					case nameof(LocalSettings.AllCameras.Camera4):
						ManageRtspStatus(LocalSettings.Singleton.AllCameras.Camera4, 4);
						_rtspClient[4].UpdateConnectionSettings();
						break;
					case nameof(LocalSettings.AllCameras.Camera5):
						ManageRtspStatus(LocalSettings.Singleton.AllCameras.Camera5, 5);
						_rtspClient[5].UpdateConnectionSettings();
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
					_rtspClientWeak[id] = new(_rtspClient[id]);
					imTextureRect[id].Visible = true;
					_rtspClient[id].FrameReceived += RTSPworkHandler;
					break;
				case false when _rtspClient[id] is not null:
					imTextureRect[id].Visible = false;
					_rtspClient[id].Dispose();
					_rtspClient[id] = null;
					break;
			}
		}

		private void ManagePtzStatus()
		{
			switch (LocalSettings.Singleton.AllCameras.Camera0.EnablePtzControl)
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

			RtspStreamClient?[] rtspClient = new RtspStreamClient?[MaxCams];
			OnvifPtzCameraController? ptzClient = null;

			for (int id = 0; id < MaxCams; id++)
			{
				_rtspClientWeak[id]?.TryGetTarget(out rtspClient[id]);
			}
			_ptzClientWeak?.TryGetTarget(out ptzClient);

			Color mqttStatusColor = GetColorForCommunicationState(RoverCommunication?.RoverStatus?.CommunicationState);

			Color[] rtspStatusColor = new Color[MaxCams];
			Color ptzStatusColor = GetColorForCommunicationState(ptzClient?.State);

			for (int id = 0; id < MaxCams; id++)
			{
				rtspStatusColor[id] = GetColorForCommunicationState(rtspClient[id]?.State);
			}


			Color[] rtspAgeColor = new Color[MaxCams];
			for (int id = 0; id < MaxCams; id++)
			{
				if (rtspClient[id]?.ElapsedSecondsOnCurrentState < 1.0f)
					rtspAgeColor[id] = Colors.LightGreen;
				else
					rtspAgeColor[id] = Colors.Orange;
			}

			string[]? rtspAge = new string[MaxCams];
			for (int id = 0; id < MaxCams; id++)
			{
				rtspAge[id] = rtspClient[id]?.ElapsedSecondsOnCurrentState.ToString("f2", new CultureInfo("en-US"));
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

			if (!_rtspHidden)
			{
				for (int id = 0; id < MaxCams; id++)
				{
					if (rtspClient[id]?.State == CommunicationState.Opened)
						FancyDebugViewRLab.AppendText($"RTSP {id}: Frame is [color={rtspAgeColor[id].ToHtml(false)}]{rtspAge[id]}s[/color] old\n");
					else
						FancyDebugViewRLab.AppendText($"RTSP {id}: [color={rtspStatusColor[id].ToHtml(false)}]{rtspClient[id]?.State ?? CommunicationState.Closed}[/color], Time: {rtspAge[id] ?? "N/A "}s\n");
				}
			}
			else
			{
				FancyDebugViewRLab.AppendText("RTSP: ");
				for (int id = 0; id < MaxCams; id++)
				{
					string temp = rtspClient[id]?.State.ToString();
					if(temp is null)
						temp = "N/A";
					FancyDebugViewRLab.AppendText($"{id}: [color={rtspStatusColor[id].ToHtml(false)}]{temp[0]}[/color]{(id == MaxCams-1 ? "" : ", ")}");
				}
				FancyDebugViewRLab.AppendText("\n");
			}

			if (ptzClient?.State == CommunicationState.Opened)
			{
				FancyDebugViewRLab.AppendText($"PTZ : Since last move request: {ptzAge}s\n");
				FancyDebugViewRLab.AppendText($"PTZ : Move vector: {ptzClient.CameraMotion}\n");
			}
			else
			{
				FancyDebugViewRLab.AppendText(
					$"PTZ : [color={ptzStatusColor.ToHtml(false)}]{ptzClient?.State ?? CommunicationState.Closed}[/color], Time: {ptzAge ?? "N/A "}s\n");
			}
			
			
			//else
			//{
			//	FancyDebugViewRLab.AppendText("PTZ : ");
			//	for (int id = 0; id < MaxCams; id++)
			//	{
			//		string temp = ptzClient[id]?.State.ToString();
			//		if (temp is null)
			//			temp = "N/A";
			//		FancyDebugViewRLab.AppendText($"{id}: [color={ptzStatusColor[id].ToHtml(false)}]{temp[0]}[/color]{(id == MaxCams - 1 ? "" : ", ")}");
			//	}
			//	FancyDebugViewRLab.AppendText("\n");
			//}

			ShowRTSPflipper.Position = new Vector2(0, 25 + ((RoverCommunication?.RoverStatus?.ControlMode == MqttClasses.ControlMode.EStop || RoverCommunication?.RoverStatus?.ControlMode == MqttClasses.ControlMode.Autonomy) ? 0 : 23));
			ShowRTSPflipper.Size = new Vector2(ShowRTSPflipper.Size.X, 20 + (_rtspHidden ? 0 : 5*23));

			ShowPTZflipper.Position = new Vector2(ShowRTSPflipper.Position.X, ShowRTSPflipper.Position.Y + (_rtspHidden ? 20 : 20 + 5*23));
			ShowPTZflipper.Size = new Vector2(ShowPTZflipper.Size.X, 20 + (_ptzHidden ? 0 : 5*23));
		}

		public async Task<bool> CaptureCameraImage(string subfolder = "CapturedImages", string? fileName = null, string fileExtension = "jpg")
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

		private void FlipBool(int which)
		{
			switch (which)
			{
				case 0:
					_rtspHidden = !_rtspHidden;
					break;
				case 1:
					_ptzHidden = !_ptzHidden;
					break;
			}
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
