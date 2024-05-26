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
		public static EventLogger? EventLogger { get; private set; }
		public static LocalSettings? Settings { get; private set; }
		public static PressedKeys? PressedKeys { get; private set; }
		public static RoverCommunication? RoverCommunication { get; private set; }
		public static MissionStatus? MissionStatus { get; private set; }
		public static MqttClient? MqttClient { get; private set; }
		public static MissionSetPoint? MissionSetPoint { get; private set; }

		//Main camera + 5 preview cameras 
		private const int numCameras = 6;

		private RtspStreamClient[]? _rtspClient = new RtspStreamClient[numCameras];


		private OnvifPtzCameraController? _ptzClient;
		private JoyVibrato? _joyVibrato;
		private BackCapture _backCapture = new();

		private TextureRect[]? _imTextureRect = new TextureRect[numCameras];
		
		private ImageTexture[]? _imTexture = new ImageTexture[numCameras];

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

			if (Settings.Settings.Camera0.EnablePtzControl)
				_ptzClient = new OnvifPtzCameraController(
					Settings.Settings.Camera0.Ip,
					Settings.Settings.Camera0.PtzPort,
					Settings.Settings.Camera0.Login,
					Settings.Settings.Camera0.Password);

			if (Settings.Settings.Camera0.EnableRtspStream)
				_rtspClient[0] = new RtspStreamClient(
								0,				
								Settings.Settings.Camera0.Login,
								Settings.Settings.Camera0.Password,
								Settings.Settings.Camera0.RtspStreamPathHQ,
								Settings.Settings.Camera0.RtspStreamPathLQ,
								Settings.Settings.Camera0.Ip,
								"rtsp",
								Settings.Settings.Camera0.RtspPort);
			
			if (Settings.Settings.Camera1.EnableRtspStream)
				_rtspClient[1] = new RtspStreamClient(
								1,				
								Settings.Settings.Camera1.Login,
								Settings.Settings.Camera1.Password,
								Settings.Settings.Camera1.RtspStreamPathHQ,
								Settings.Settings.Camera1.RtspStreamPathLQ,
								Settings.Settings.Camera1.Ip,
								"rtsp",
								Settings.Settings.Camera1.RtspPort);
			
			for(int i = 0; i < numCameras; i++)
			{
				_rtspClient[i] = new RtspStreamClient(
								i,
								Settings.Settings.Camera1.Login,
								Settings.Settings.Camera1.Password,
								Settings.Settings.Camera1.RtspStreamPathHQ,
								Settings.Settings.Camera1.RtspStreamPathLQ,
								Settings.Settings.Camera1.Ip,
								"rtsp",
								Settings.Settings.Camera1.RtspPort);
			}
					
			_imTextureRect[0] = GetNode<TextureRect>("CameraHD");
			_imTextureRect[1] = GetNode<TextureRect>("PreviewCameras/CameraA");
			_imTextureRect[2] = GetNode<TextureRect>("PreviewCameras/CameraB");
			_imTextureRect[3] = GetNode<TextureRect>("PreviewCameras/CameraC");
			_imTextureRect[4] = GetNode<TextureRect>("PreviewCameras/CameraD");
			_imTextureRect[5] = GetNode<TextureRect>("PreviewCameras/CameraE");


			if (_ptzClient != null) PressedKeys.OnAbsoluteVectorChanged += _ptzClient.ChangeMoveVector;
			if (_joyVibrato is not null) PressedKeys.OnControlModeChanged += _joyVibrato.ControlModeChangedSubscriber;

			SettingsManagerNode.Target = Settings;
			MissionStatus.OnRoverMissionStatusChanged += MissionControlNode!.MissionStatusUpdatedSubscriber;
			MissionControlNode.LoadSizeAndPos();
			MissionControlNode.SMissionControlVisualUpdate();

			MqttClient.OnMessageReceivedAsync += VelMonitor.MqttSubscriber;
            MqttClient.OnMessageReceivedAsync += ZedMonitor.OnGyroscopeChanged;

			_rtspClient[0].OnFrameReceived += MainRtspWrapper;
			_rtspClient[1].OnFrameReceived += PreviewARtspWrapper;
			_rtspClient[2].OnFrameReceived += PreviewBRtspWrapper;
			_rtspClient[3].OnFrameReceived += PreviewCRtspWrapper;
			_rtspClient[4].OnFrameReceived += PreviewDRtspWrapper;
			_rtspClient[5].OnFrameReceived += PreviewERtspWrapper;

            //UIDis
            RoverModeUIDis.ControlMode = (int)PressedKeys.ControlMode;
			PressedKeys.OnControlModeChanged += RoverModeUIDis.ControlModeChangedSubscriber;
			GrzybUIDis.MqttSubscriber(Settings.Settings.Mqtt.TopicEStopStatus, MqttClient.GetReceivedMessageOnTopic(Settings.Settings.Mqtt.TopicEStopStatus));
			MqttClient.OnMessageReceivedAsync += GrzybUIDis.MqttSubscriber;
			MissionStatus.OnRoverMissionStatusChanged += MissionStatusUIDis.StatusChangeSubscriber;

			//state new mode
			_joyVibrato?.ControlModeChangedSubscriber(PressedKeys.ControlMode);

			_backCapture.HistoryLength = Settings.Settings.BackCaptureLength;

			
		}

		// Called when the node enters the scene tree for the first time.
		public override void _Ready()
		{
			if (MainViewModelInstance is not null)
				throw new Exception("MainViewModel must have single instance!!!");
			MainViewModelInstance = this;

			Thread.CurrentThread.Name = "MainUI_Thread";
			StartUp();
		}

		private void VirtualRestart()
		{
			//UIDis
			MqttClient.OnMessageReceivedAsync -= GrzybUIDis.MqttSubscriber;
			MissionStatus.OnRoverMissionStatusChanged -= MissionStatusUIDis.StatusChangeSubscriber;
			MqttClient!.OnMessageReceivedAsync -= VelMonitor.MqttSubscriber;
			MqttClient!.OnMessageReceivedAsync -= ZedMonitor.OnGyroscopeChanged;

			_rtspClient[0].OnFrameReceived -= MainRtspWrapper;
			_rtspClient[1].OnFrameReceived -= PreviewARtspWrapper;
			_rtspClient[2].OnFrameReceived -= PreviewBRtspWrapper;
			_rtspClient[3].OnFrameReceived -= PreviewCRtspWrapper;
			_rtspClient[4].OnFrameReceived -= PreviewDRtspWrapper;
			_rtspClient[5].OnFrameReceived -= PreviewERtspWrapper;


			ShowSettingsBtn.ButtonPressed = ShowMissionControlBrn.ButtonPressed = ShowVelMonitor.ButtonPressed = false;
			if (_ptzClient != null)
			{
				if (PressedKeys != null) PressedKeys.OnAbsoluteVectorChanged -= _ptzClient.ChangeMoveVector;
				_ptzClient?.Dispose();
			}
			PressedKeys.OnControlModeChanged -= RoverModeUIDis!.ControlModeChangedSubscriber;
			_ptzClient = null;
			for (int i = 0; i < numCameras; i++)
			{
				_rtspClient[i]?.Dispose();
				_rtspClient[i] = null;
			}
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

			for(int i = 0; i < numCameras; i++)
				_rtspClient[i]?.Dispose();

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
					EventLogger?.LogMessage($"BackCapture INFO: Saved capture!");
				else
					EventLogger?.LogMessage($"BackCapture ERROR: Save failed!");
			}
		}

		

		

		// Called every frame. 'delta' is the elapsed time since the previous frame.
		public override void _Process(double delta)
		{
			//IsRtspResOk();
			UpdateLabel();

			GrzybUIDis.MqttSubscriber("", null);
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

			Color rtspStatusColor = GetColorForCommunicationState(_rtspClient[0]?.State);

			Color ptzStatusColor = GetColorForCommunicationState(_ptzClient?.State);

			Color rtspAgeColor;
			if (_rtspClient[0]?.ElapsedSecondsOnCurrentState < 1.0f)
				rtspAgeColor = Colors.LightGreen;
			else
				rtspAgeColor = Colors.Orange;

			string? rtspAge = _rtspClient[0]?.ElapsedSecondsOnCurrentState.ToString("f2", new CultureInfo("en-US"));
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

			if (_rtspClient[0]?.State == CommunicationState.Opened)
				FancyDebugViewRLab.AppendText($"RTSP: Frame is [color={rtspAgeColor.ToHtml(false)}]{rtspAge}s[/color] old\n");
			else
				FancyDebugViewRLab.AppendText($"RTSP: [color={rtspStatusColor.ToHtml(false)}]{_rtspClient[0]?.State ?? CommunicationState.Closed}[/color], Time: {rtspAge ?? "N/A "}s\n");

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
			_rtspClient[0].LockGrabbingFrames();
			if (_rtspClient[0].LatestImage is not null && !_rtspClient[0].LatestImage.IsEmpty())
				img.CopyFrom(_rtspClient[0].LatestImage);
			_rtspClient[0].UnLockGrabbingFrames();

			if (img.IsEmpty())
			{
				EventLogger?.LogMessage($"CaptureCameraImage ERROR: No image to capture!");
				return false;
			}

			bool saveAsJpg;
			if (fileExtension.Equals("jpg", StringComparison.InvariantCultureIgnoreCase))
				saveAsJpg = true;
			else if (fileExtension.Equals("png", StringComparison.InvariantCultureIgnoreCase))
				saveAsJpg = false;
			else
			{
				EventLogger?.LogMessage($"CaptureCameraImage ERROR: \"{fileExtension}\" is not valid extension! (png or jpg)");
				return false;
			}

			fileName ??= DateTime.Now.ToString("yyyyMMdd_hhmmss");
			string pathToFile = $"user://{subfolder}";

			if (!DirAccess.DirExistsAbsolute(pathToFile))
			{
				EventLogger?.LogMessage($"CaptureCameraImage INFO: Subfolder \"{pathToFile}\" not exists yet, creating.");
				var err = DirAccess.MakeDirAbsolute(pathToFile);
				if (err != Error.Ok)
				{
					EventLogger?.LogMessage($"CaptureCameraImage ERROR: Creating subfolder \"{pathToFile}\" failed. ({err.ToString()})");
					return false;
				}
			}

			pathToFile += $"/{fileName}.{fileExtension}";

			if (FileAccess.FileExists(pathToFile))
				EventLogger?.LogMessage($"CaptureCameraImage WARNING: \"{pathToFile}\" already exists and will be overwrited!");

			Error imgSaveErr;
			if (saveAsJpg)
				imgSaveErr = img.SaveJpg(pathToFile);
			else
				imgSaveErr = img.SavePng(pathToFile);

			if (imgSaveErr != Error.Ok)
			{
				EventLogger?.LogMessage($"CaptureCameraImage ERROR: Creating subfolder \"{pathToFile}\" failed. ({imgSaveErr.ToString()})");
				return false;
			}

			return true;
		}


		private void OnBackCapture()
		{
			if (_backCapture.SaveHistory())
				EventLogger?.LogMessage($"BackCapture INFO: Saved capture!");
			else
				EventLogger?.LogMessage($"BackCapture ERROR: Save failed!");
		}

		private void OnRTSPCapture()
		{
			CaptureCameraImage(subfolder: "Screenshots", fileName: DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString());
		}

		void MainRtspWrapper(){CallDeferred(MethodName.RtspUpdate, _imTexture[0], _imTextureRect[0], 0);}
		void PreviewARtspWrapper(){CallDeferred(MethodName.RtspUpdate, _imTexture[1], _imTextureRect[1], 1);}
		void PreviewBRtspWrapper(){CallDeferred(MethodName.RtspUpdate, _imTexture[2], _imTextureRect[2], 2);}
		void PreviewCRtspWrapper(){CallDeferred(MethodName.RtspUpdate, _imTexture[3], _imTextureRect[3], 3);}
		void PreviewDRtspWrapper(){CallDeferred(MethodName.RtspUpdate, _imTexture[4], _imTextureRect[4], 4);}
		void PreviewERtspWrapper(){CallDeferred(MethodName.RtspUpdate, _imTexture[5], _imTextureRect[5], 5);}


		public void RtspUpdate(ImageTexture _imTexture, TextureRect _imTextureRect, int i)
		{
			if (_rtspClient[i] is { NewFrameSaved: true })
			{
				_backCapture.CleanUpHistory();

				_rtspClient[i].LockGrabbingFrames();
				if (_imTexture == null || _imTexture._GetHeight() != _rtspClient[i].LatestImage.GetHeight() || _imTexture._GetWidth() != _rtspClient[i].LatestImage.GetWidth()) _imTexture = ImageTexture.CreateFromImage(_rtspClient[i].LatestImage);
				else _imTexture.Update(_rtspClient[i].LatestImage);

				_backCapture.FrameFeed(_rtspClient[i].LatestImage);

				_rtspClient[i].UnLockGrabbingFrames();
				_imTextureRect!.Texture = _imTexture;
				_rtspClient[i].NewFrameSaved = false;
			}
		}
	}
}
