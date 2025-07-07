using System;
using System.Collections.Generic;
using System.Globalization;
using System.ServiceModel;
using System.Text.Json;
using System.Threading.Tasks;

using Godot;

using RoverControlApp.Core;
using RoverControlApp.Core.RoverControllerPresets;
using RoverControlApp.MVVM.Model;
using RoverControlApp.MVVM.ViewModel.MainView_Panel;

namespace RoverControlApp.MVVM.ViewModel
{
	public partial class MainViewModel : Control
	{
		private enum InputHelpHintMode
		{
			Hidden = 0,
			All = 1,
			SkipCamera = 2,
			SkipCameraAndNotes = 3,
		}

		private enum TopPanelMode
		{
			Full = 0,
			FullExtra = 1,
			Mini = 2,
			MiniExtra = 3,
		}

		private WeakReference<RtspStreamClient>? _rtspClientWeak;
		private WeakReference<OnvifPtzCameraController>? _ptzClientWeak;

		private RtspStreamClient? _rtspClient;
		private OnvifPtzCameraController? _ptzClient;

		private JoyVibrato _joyVibrato = new();
		private BackCapture _backCapture = new();

		private ImageTexture? _imTexture;

		private InputHelpHintMode _inputHelpHintMode = InputHelpHintMode.Hidden;
		private TopPanelMode _mainTopPanelMode = TopPanelMode.Full;

		[Export]
		private TextureRect imTextureRect = null!;

		[Export]
		private Button ShowSettingsBtn = null!, ShowVelMonitor = null!, ShowMissionControlBrn = null!;
		[Export]
		private SettingsManager SettingsManagerNode = null!;
		[Export]
		private MissionControl MissionControlNode = null!;

		[Export]
		private RichTextLabel FancyDebugViewRLab = null!;


		[Export]
		private ZedMonitor ZedMonitor = null!;
		[Export]
		private BatteryMonitor BatteryMonitor = null!;

		[Export]
		private InputHelpMaster InputHelpMaster = null!;

		[Export]
		private TopPanelBase MainPanelTopMini = null!;
		[Export]
		private TopPanelBase MainPanelTopFull = null!;

		[Export]
		private Timer _cameraDataPulser = null!;

		[Signal]
		public delegate void RtspConnectionChangeEventHandler(CommunicationState state);

		[Signal]
		public delegate void PtzConnectionChangeEventHandler(CommunicationState state);

		[Signal]
		public delegate void CameraDataPulseEventHandler(float rtspDelay, float ptzDelay);


		public override void _EnterTree()
		{
			SettingsManagerNode.Target = LocalSettings.Singleton;

			PressedKeys.Singleton.OnControlModeChanged += _joyVibrato.ControlModeChangedSubscriber;
			PressedKeys.Singleton.OnControlModeChanged += InputHelp_HandleControlModeChanged;
			PressedKeys.Singleton.ControllerPresetChanged += InputHelp_HandleInputPresetChanged;
			MissionStatus.Singleton.OnRoverMissionStatusChanged += MissionControlNode.MissionStatusUpdatedSubscriber;

			InputHelp_HandleControlModeChanged(PressedKeys.Singleton.ControlMode);
			Task.Run(async () => await _joyVibrato.ControlModeChangedSubscriber(PressedKeys.Singleton.ControlMode));
		}

		// Called when the node enters the scene tree for the first time.
		public override void _Ready()
		{
			MissionControlNode.LoadSizeAndPos();
			MissionControlNode.SMissionControlVisualUpdate();

			ManagePtzStatus();
			ManageRtspStatus();
			OnPtzStateChange(CommunicationState.Closed);
			OnRtspStateChange(CommunicationState.Closed);


			InputHelpMaster.GenerateHints();
			InputHelpMaster.HintType = PressedKeys.PadConnected ? InputHelpHint.HintVisibility.Joy : InputHelpHint.HintVisibility.Kb;

			LocalSettings.Singleton.Connect(LocalSettings.SignalName.CategoryChanged, Callable.From<StringName>(OnSettingsCategoryChanged));
			LocalSettings.Singleton.Connect(LocalSettings.SignalName.PropagatedPropertyChanged, Callable.From<StringName, StringName, Variant, Variant>(OnSettingsPropertyChanged));
		}

		public override void _ExitTree()
		{
			ShowSettingsBtn.ButtonPressed = ShowMissionControlBrn.ButtonPressed = ShowVelMonitor.ButtonPressed = false;

			PressedKeys.Singleton.OnControlModeChanged -= _joyVibrato.ControlModeChangedSubscriber;
			PressedKeys.Singleton.OnControlModeChanged -= InputHelp_HandleControlModeChanged;
			PressedKeys.Singleton.ControllerPresetChanged -= InputHelp_HandleInputPresetChanged;
			MissionStatus.Singleton.OnRoverMissionStatusChanged -= MissionControlNode.MissionStatusUpdatedSubscriber;

			LocalSettings.Singleton.Disconnect(LocalSettings.SignalName.CategoryChanged, Callable.From<StringName>(OnSettingsCategoryChanged));
			LocalSettings.Singleton.Disconnect(LocalSettings.SignalName.PropagatedPropertyChanged, Callable.From<StringName, StringName, Variant, Variant>(OnSettingsPropertyChanged));
		}

		protected override void Dispose(bool disposing)
		{
			_joyVibrato?.Dispose();
			_rtspClient?.Dispose();
			_ptzClient?.Dispose();
			base.Dispose(disposing);
		}

		public override void _UnhandledInput(InputEvent @event)
		{
			if (@event is InputEventKey inputEvKey && inputEvKey.Keycode != Key.Shift)
				InputHelpMaster.HintType = InputHelpHint.HintVisibility.Kb;
			else if (@event is InputEventJoypadButton)
				InputHelpMaster.HintType = InputHelpHint.HintVisibility.Joy;

			if (@event is not (InputEventKey or InputEventJoypadButton or InputEventJoypadMotion)) return;


			if (@event.IsActionPressed("app_backcapture_save"))
			{
				if (_backCapture.SaveHistory())
					EventLogger.LogMessage("MainViewModel/BackCapture", EventLogger.LogLevel.Info, "Saved capture!");
				else
					EventLogger.LogMessage("MainViewModel/BackCapture", EventLogger.LogLevel.Error, "Save failed!");
				GetViewport().SetInputAsHandled();
			}

			if (InputHelp_HandleInput(@event))
			{
				GetViewport().SetInputAsHandled();
				return;
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
				imTextureRect.Texture = _imTexture;
				_rtspClient.MarkFrameOld();
			}
			UpdateLabel();
		}

		/*
		 * Settings event handlers
		 */

		void OnSettingsCategoryChanged(StringName property)
		{
			if (property != nameof(LocalSettings.Camera)) return;

			ManagePtzStatus();
			ManageRtspStatus();
		}

		void OnSettingsPropertyChanged(StringName category, StringName name, Variant oldValue, Variant newValue)
		{
			if (category != nameof(LocalSettings.Camera)) return;

			switch (name)
			{
				case nameof(LocalSettings.Camera.EnablePtzControl):
					ManagePtzStatus();
					break;
				case nameof(LocalSettings.Camera.EnableRtspStream):
					ManageRtspStatus();
					break;
			}
		}

		/*
		 * settings handlers end
		 */

		private void ManageRtspStatus()
		{
			switch (LocalSettings.Singleton.Camera.EnableRtspStream)
			{
				case true when _rtspClient is null:
					_rtspClient = new();
					_rtspClientWeak = new(_rtspClient);
					imTextureRect.Visible = true;
					_rtspClient.StateChange += OnRtspStateChange;
					break;
				case false when _rtspClient is not null:
					_rtspClient.StateChange -= OnRtspStateChange;
					OnRtspStateChange(CommunicationState.Closed);
					_rtspClient.Dispose();
					_rtspClient = null;
					imTextureRect.Visible = false;
					break;
			}
		}

		private void ManagePtzStatus()
		{
			switch (LocalSettings.Singleton.Camera.EnablePtzControl)
			{
				case true when _ptzClient is null:
					_ptzClient = new OnvifPtzCameraController();
					_ptzClientWeak = new(_ptzClient);
					PressedKeys.Singleton.CameraMoveVectorChanged += _ptzClient.ChangeMoveVector;
					_ptzClient.StateChange += OnPtzStateChange;
					break;
				case false when _ptzClient is not null:
					PressedKeys.Singleton.CameraMoveVectorChanged -= _ptzClient.ChangeMoveVector;
					_ptzClient.StateChange -= OnPtzStateChange;
					OnPtzStateChange(CommunicationState.Closed);
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

			Color mqttStatusColor = GetColorForCommunicationState(RoverCommunication.Singleton.RoverStatus?.CommunicationState);

			Color rtspStatusColor = GetColorForCommunicationState(rtspClient?.State);

			Color ptzStatusColor = GetColorForCommunicationState(ptzClient?.State);

			Color rtspAgeColor;
			if (rtspClient?.ElapsedSecondsOnCurrentState < 1.0f)
				rtspAgeColor = Colors.LightGreen;
			else
				rtspAgeColor = Colors.Orange;

			string? rtspAge = rtspClient?.ElapsedSecondsOnCurrentState.ToString("f2", new CultureInfo("en-US"));
			string? ptzAge = ptzClient?.ElapsedSecondsOnCurrentState.ToString("f2", new CultureInfo("en-US"));

			FancyDebugViewRLab.AppendText($"MQTT: Control Mode: {RoverCommunication.Singleton.RoverStatus?.ControlMode}, " +
			              $"{(RoverCommunication.Singleton.RoverStatus?.ControlMode == MqttClasses.ControlMode.Rover ? $"Kinematics change: {(LocalSettings.Singleton.Joystick.ToggleableKinematics ? "Toggle" : "Hold")}, " : "")}" +
						  $"Connection: [color={mqttStatusColor.ToHtml(false)}]{RoverCommunication.Singleton.RoverStatus?.CommunicationState}[/color], " +
						  $"Pad connected: {RoverCommunication.Singleton.RoverStatus?.PadConnected}\n");
			switch (RoverCommunication.Singleton.RoverStatus?.ControlMode)
			{
				case MqttClasses.ControlMode.Rover:
					var vecc = new Vector3((float)PressedKeys.Singleton.RoverMovement.Vel, (float)PressedKeys.Singleton.RoverMovement.XAxis,
						(float)PressedKeys.Singleton.RoverMovement.YAxis);

					FancyDebugViewRLab.AppendText($"PressedKeys: Rover Mov: Vel: {vecc.X:F2}, XAxis: {vecc.Y:F2}, YAxis: {vecc.Z:F2}, Mode: {PressedKeys.Singleton.RoverMovement.Mode}\n");

					break;
				case MqttClasses.ControlMode.Manipulator:
					FancyDebugViewRLab.AppendText($"PressedKeys.Singleton: Manipulator Mov: {JsonSerializer.Serialize(PressedKeys.Singleton.ManipulatorMovement)}\n");
					break;
				case MqttClasses.ControlMode.Sampler:
					FancyDebugViewRLab.AppendText($"PressedKeys: Sampler DrillAction: {PressedKeys.Singleton.SamplerMovement.DrillAction:F2}, " +
					                              $"DrillMov: {PressedKeys.Singleton.SamplerMovement.DrillMovement:F2}, " +
					                              $"PlatformMov: {PressedKeys.Singleton.SamplerMovement.PlatformMovement:F2}, " +
												  $"{(LocalSettings.Singleton.Sampler.Container0.CustomName == "-" ? "Container0" : LocalSettings.Singleton.Sampler.Container0.CustomName)}" +
																$": {PressedKeys.Singleton.SamplerMovement.ContainerDegrees0:F1}, " +
												  $"{(LocalSettings.Singleton.Sampler.Container1.CustomName == "-" ? "Container1" : LocalSettings.Singleton.Sampler.Container1.CustomName)}" +
																$": {PressedKeys.Singleton.SamplerMovement.ContainerDegrees1:F1}, " +
					                              $"{(LocalSettings.Singleton.Sampler.Container2.CustomName == "-" ? "Container2" : LocalSettings.Singleton.Sampler.Container2.CustomName)}" +
																$": {PressedKeys.Singleton.SamplerMovement.ContainerDegrees2:F1}\n");
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


			Godot.Collections.Dictionary timeDictEStop;
			if (PressedKeys.Singleton.TimeToAutoEStopMsec > 0)
			{
				timeDictEStop = Time.GetTimeDictFromUnixTime(PressedKeys.Singleton.TimeToAutoEStopMsec / 1000);
				// for number to stop jumping
				if (LocalSettings.Singleton.General.NoInputSecondsToEstop <= 30)
					timeDictEStop["second"] = Math.Min(LocalSettings.Singleton.General.NoInputSecondsToEstop - 1, timeDictEStop["second"].AsUInt32());
			}
			else
				timeDictEStop = Time.GetTimeDictFromUnixTime(PressedKeys.Singleton.TimeToAutoEStopMsec / -1000);

			switch (LocalSettings.Singleton.General.NoInputSecondsToEstop)
			{
				case 0:
					FancyDebugViewRLab.AppendText($"Auto-EStop: [color={Colors.Red.ToHtml()}]DISABLED[/color]\n");
					break;
				case var x when x > 0 && PressedKeys.Singleton.TimeToAutoEStopMsec < 0:
					FancyDebugViewRLab.AppendText($"Auto-EStop: [color={Colors.Green.ToHtml()}]ACTIVE[/color] ({timeDictEStop["hour"].AsUInt32():D2}:{timeDictEStop["minute"].AsUInt32():D2}:{timeDictEStop["second"].AsUInt32():D2} since activation)\n");
					break;
				case var x when x > 30 && PressedKeys.Singleton.TimeToAutoEStopMsec >= LocalSettings.Singleton.General.NoInputSecondsToEstop * 1000 - 10000:
					FancyDebugViewRLab.AppendText($"Auto-EStop: [color={Colors.LightCyan.ToHtml()}]INACTIVE[/color] (recent input)\n");
					break;
				case var x when x <= 30 || PressedKeys.Singleton.TimeToAutoEStopMsec < LocalSettings.Singleton.General.NoInputSecondsToEstop * 1000 - 10000:
					FancyDebugViewRLab.AppendText($"Auto-EStop: [color={Colors.LightCyan.ToHtml()}]INACTIVE[/color] ({timeDictEStop["minute"].AsUInt32():D2}:{timeDictEStop["second"].AsUInt32():D2} left)\n");
					break;
			}
		}

		private void InputHelp_HandleInputPresetChanged()
		{
			InputHelp_HandleControlModeChanged(PressedKeys.Singleton.ControlMode);
		}

		private Task InputHelp_HandleControlModeChanged(MqttClasses.ControlMode controlMode)
		{
			if (_inputHelpHintMode == InputHelpHintMode.Hidden)
				return Task.CompletedTask;
			InputHelpMaster.ActionAwareControllers = InputHelp_HintsToShow(controlMode);
			return Task.CompletedTask;
		}

		private IActionAwareController[] InputHelp_HintsToShow(MqttClasses.ControlMode controlMode)
		{
			List<IActionAwareController> wipList = [PressedKeys.Singleton.RoverModeControllerPreset];

			if (_inputHelpHintMode == InputHelpHintMode.All)
			{
				wipList.Add(PressedKeys.Singleton.RoverCameraControllerPreset);
			}

			switch (controlMode)
			{
				case MqttClasses.ControlMode.EStop:
					break; //empty
				case MqttClasses.ControlMode.Rover:
					wipList.Add(PressedKeys.Singleton.RoverDriveControllerPreset);
					break;
				case MqttClasses.ControlMode.Manipulator:
					wipList.Add(PressedKeys.Singleton.RoverManipulatorControllerPreset);
					break;
				case MqttClasses.ControlMode.Sampler:
					wipList.Add(PressedKeys.Singleton.RoverSamplerControllerPreset);
					break;
				case MqttClasses.ControlMode.Autonomy:
					break; //empty
			}

			return [.. wipList];
		}

		private bool InputHelp_HandleInput(InputEvent @event)
		{
			if (@event.IsActionPressed("input_help_show", exactMatch: true))
			{
				_inputHelpHintMode++;
				if (_inputHelpHintMode > InputHelpHintMode.SkipCameraAndNotes)
				{
					_inputHelpHintMode = InputHelpHintMode.Hidden;
				}



				InputHelpMaster.ShowAdditionalNotes = _inputHelpHintMode < InputHelpHintMode.SkipCameraAndNotes;
				InputHelpMaster.Visible = _inputHelpHintMode != InputHelpHintMode.Hidden;
				InputHelp_HandleControlModeChanged(PressedKeys.Singleton.ControlMode);
				return true;
			}
			return false;
		}

		private void OnMainPanelTop_LayoutChange()
		{
			switch (Input.IsKeyPressed(Key.Ctrl))
			{
				case true when _mainTopPanelMode == TopPanelMode.Full:
					_mainTopPanelMode = TopPanelMode.FullExtra;
					break;
				case true when _mainTopPanelMode == TopPanelMode.FullExtra:
					_mainTopPanelMode = TopPanelMode.Full;
					break;
				case true when _mainTopPanelMode == TopPanelMode.Mini:
					_mainTopPanelMode = TopPanelMode.MiniExtra;
					break;
				case true when _mainTopPanelMode == TopPanelMode.MiniExtra:
					_mainTopPanelMode = TopPanelMode.Mini;
					break;
				default:
					_mainTopPanelMode = (TopPanelMode)(((int)_mainTopPanelMode + 2) % 4);
					break;
			}

			MainPanelTop_LayoutApply();
		}

		private void MainPanelTop_LayoutApply()
		{
			if (_mainTopPanelMode == TopPanelMode.Full || _mainTopPanelMode == TopPanelMode.FullExtra)
			{
				MainPanelTopFull.PanelVisible = true;
				MainPanelTopMini.PanelVisible = false;
			}
			else
			{
				MainPanelTopFull.PanelVisible = false;
				MainPanelTopMini.PanelVisible = true;
			}

			if (_mainTopPanelMode == TopPanelMode.FullExtra || _mainTopPanelMode == TopPanelMode.MiniExtra)
			{
				FancyDebugViewRLab.Visible = true;
			}
			else
			{
				FancyDebugViewRLab.Visible = false;
			}
		}

		private void OnRtspStateChange(CommunicationState state) => CallDeferred(MethodName.EmitSignal, SignalName.RtspConnectionChange, (int)state);
		private void OnPtzStateChange(CommunicationState state) => CallDeferred(MethodName.EmitSignal, SignalName.PtzConnectionChange, (int)state);

		private void OnCameraDataPulserTimeout()
		{
			if (!IsNodeReady())
				return;

			EmitSignal(
				SignalName.CameraDataPulse,
				_rtspClient?.ElapsedSecondsOnCurrentState ?? -1.0,
				_ptzClient?.ElapsedSecondsOnCurrentState ?? -1.0
			);
		}

		public bool CaptureCameraImage(string subfolder = "CapturedImages", string? fileName = null, string fileExtension = "jpg")
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
			Task.Run(() => CaptureCameraImage(subfolder: "Screenshots", fileName: DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString()));
		}
	}
}
