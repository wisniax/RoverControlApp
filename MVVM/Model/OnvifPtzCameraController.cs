using Godot;
using Onvif.Core.Client;
using Onvif.Core.Client.Common;
using RoverControlApp.Core;
using System;
using System.Diagnostics;
using System.ServiceModel;
using System.Threading;
using DateTime = System.DateTime;
using Mutex = System.Threading.Mutex;

namespace RoverControlApp.MVVM.Model
{
	public class OnvifPtzCameraController : IDisposable
	{
		private Camera? _camera = null;
		private Vector4 _cameraMotion = Vector4.Zero;
		private readonly Mutex _dataMutex = new();
		private volatile CommunicationState _state;
		private volatile Stopwatch _generalPurposeStopwatch;
		private Thread? _ptzThread;
		private Exception? _ptzThreadError = null;
		private CancellationTokenSource _cts;
		private DateTime _lastComTimeStamp = System.DateTime.Now;

		public Vector4 CameraMotion
		{
			get
			{
				_dataMutex.WaitOne();
				var copy = _cameraMotion;
				_dataMutex.ReleaseMutex();
				return copy;
			}
			private set
			{
				EventLogger.LogMessageDebug("OnvifPtzCameraController",EventLogger.LogLevel.Verbose, $"CameraMotion update: {value}");
				_dataMutex.WaitOne();
				_cameraMotion = value;
				_dataMutex.ReleaseMutex();
			}
		}



		public CommunicationState State
		{
			get => _state;
			private set
			{
				EventLogger.LogMessage("OnvifPtzCameraController", EventLogger.LogLevel.Info, $"CommunicationState update: {value}");
				_state = value;
			}
		}


		public double ElapsedSecondsOnCurrentState => _generalPurposeStopwatch.Elapsed.TotalSeconds;

		public TimeSpan MinSpanEveryCom =>
			TimeSpan.FromSeconds(1 / LocalSettings.Singleton.AllCameras.Camera0.PtzRequestFrequency);
		public TimeSpan MaxSpanEveryCom => 1.5 * MinSpanEveryCom;





		public void ChangeMoveVector(object? sender, Vector4 vector)
		{
			CameraMotion = vector;
		}

		public OnvifPtzCameraController()
		{
			_generalPurposeStopwatch = Stopwatch.StartNew();
			_cts = new CancellationTokenSource();
			_ptzThread = new Thread(ThreadWork) { IsBackground = true, Name = $"PtzController_Thread", Priority = ThreadPriority.AboveNormal };
			_ptzThread.Start();
		}

		private void ThreadWork()
		{
			EventLogger.LogMessage("OnvifPtzCameraController", EventLogger.LogLevel.Verbose, "Thread started");
			Vector4 motionLast = _cameraMotion = Vector4.Zero;

			while (!_cts.IsCancellationRequested)
			{
				DoWork(ref motionLast);
			}
			State = CommunicationState.Closing;
			DoWork(ref motionLast);
		}

		private void CreateCamera()
		{
			if (_camera != null) EndCamera();
			_generalPurposeStopwatch.Restart();
			State = CommunicationState.Created;
			var acc = new Account
			(
				LocalSettings.Singleton.AllCameras.Camera0.ConnectionSettings.Ip + ':' + LocalSettings.Singleton.AllCameras.Camera0.ConnectionSettings.PtzPort,
				LocalSettings.Singleton.AllCameras.Camera0.ConnectionSettings.Login,
				LocalSettings.Singleton.AllCameras.Camera0.ConnectionSettings.Password
			);

			_camera = Camera.Create(acc, (e) => _ptzThreadError = e);
			

			if (_ptzThreadError is not null)
			{
				EventLogger.LogMessage("OnvifPtzCameraController", EventLogger.LogLevel.Error, $"Connecting to camera failed after " 
					+ $"{(int)_generalPurposeStopwatch.Elapsed.TotalSeconds}s with error: {_ptzThreadError}");
				State = CommunicationState.Faulted;
				return;
			}

			State = CommunicationState.Opening;
			//_camera?.Ptz.OpenAsync().Wait();
			_camera?.Ptz.StopAsync(_camera.Profile.token, true, true).Wait();
			
			State = CommunicationState.Opened;
			EventLogger.LogMessage("OnvifPtzCameraController", EventLogger.LogLevel.Info, $"PTZ: Connecting to camera succeeded in {(int)_generalPurposeStopwatch.Elapsed.TotalSeconds}s");
		}

		private void EndCamera()
		{
			//_camera?.Ptz.Close();
			_ptzThreadError = null;
			_camera = null;
		}

		private void DoWork(ref Vector4 motionLast)
		{
			switch (State)
			{
				case CommunicationState.Created:
					State = CommunicationState.Closing;
					break;
				case CommunicationState.Opening:
					State = CommunicationState.Closing;
					break;
				case CommunicationState.Opened:
					_generalPurposeStopwatch.Restart();

					bool errCaught = false;

					try
					{
						TryMoveCamera(ref motionLast);
					}
					catch (AggregateException e)
					{
						EventLogger.LogMessage("OnvifPtzCameraController", EventLogger.LogLevel.Error, $"Handled exception {e} caught");
						errCaught = true;
					}

					if (_generalPurposeStopwatch.Elapsed.TotalSeconds > 10 || errCaught)
					{
						EventLogger.LogMessage("OnvifPtzCameraController", EventLogger.LogLevel.Error, $"Camera connection lost ;( Sending a move request took {(int)_generalPurposeStopwatch.Elapsed.TotalSeconds}s");
						State = CommunicationState.Faulted;
						EndCamera();
						return;
					}

					break;
				case CommunicationState.Closing:
					EndCamera();
					State = CommunicationState.Closed;
					break;
				case CommunicationState.Closed:
					if (!_cts.IsCancellationRequested) CreateCamera();
					break;
				case CommunicationState.Faulted:
					_generalPurposeStopwatch.Restart();
					Thread.Sleep(TimeSpan.FromSeconds(10));
					State = CommunicationState.Closed;
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		private void TryMoveCamera(ref Vector4 motionLast)
		{
			//_threadBarrier.SignalAndWait();
			ComSleepTillCanRequest();
			if (!UpdateMotion(motionLast, CameraMotion, out Vector4 moveVector)) return;

			bool x1 = !Mathf.IsEqualApprox(moveVector.X, 0f, 0.1f); // Is currently moving on x axis
			bool y1 = !Mathf.IsEqualApprox(moveVector.Y, 0f, 0.1f); // Is currently moving on y axis
			bool x0 = !Mathf.IsEqualApprox(motionLast.X, 0f, 0.1f); // Was moving on x axis b4?
			bool y0 = !Mathf.IsEqualApprox(motionLast.Y, 0f, 0.1f); // Was moving on y axis b4?

			bool stopTilt = (!x1 && x0) || (!y1 && y0) || (!x1 && !y1); //When to stop camera :)
			bool stopZoom = Mathf.IsEqualApprox(moveVector.Z, 0f, 0.1f) && !Mathf.IsEqualApprox(motionLast.Z, 0f, 0.1f);

			if (stopTilt || stopZoom)
			{
				_camera?.Ptz.StopAsync(_camera.Profile.token, stopTilt, stopZoom).Wait();
				//ComRequestSleep();
			}

			if (moveVector.IsZeroApprox())
			{
				ComRequestSleep();
				motionLast = CameraMotion;
				return;
			}

			PTZSpeed ptzSpeed = new()
			{
				PanTilt = new()
				{
					x = Math.Abs(moveVector.X - motionLast.X) < 0.05f ? 0 : moveVector.X,
					y = Math.Abs(moveVector.Y - motionLast.Y) < 0.05f ? 0 : moveVector.Y
				},
				Zoom = new()
				{
					x = moveVector.Z
					//x = Math.Abs(moveVector.Z - motionLast.Z) < 0.05f ? 0 : moveVector.Z
				}
			};

			ComSleepTillCanRequest();
			_camera?.Ptz.ContinuousMoveAsync(_camera.Profile.token, ptzSpeed, string.Empty).Wait();
			ComRequestSleep();
			motionLast = CameraMotion;
		}

		private bool UpdateMotion(Vector4 old, Vector4 @new, out Vector4 speed)
		{
			speed = Vector4.Zero;
			if (@new.IsEqualApprox(old) && ((_lastComTimeStamp + MaxSpanEveryCom > System.DateTime.Now)))
			{
				Thread.Sleep(100);
				return false;
			}

			speed = LocalSettings.Singleton.AllCameras.Camera0.InverseAxis ? new Vector4(-@new.X, -@new.Y, @new.Z, @new.W) : @new;

			//Have to make sure none scalar is |x| <= 0.1f bc camera treats it as a MAX SPEED
			if (Mathf.IsEqualApprox(speed.X, 0f, 0.1f)) speed.X = 0f;
			if (Mathf.IsEqualApprox(speed.Y, 0f, 0.1f)) speed.Y = 0f;
			if (Mathf.IsEqualApprox(speed.Z, 0f, 0.1f)) speed.Z = 0f;

			speed = speed.Clamp(new Vector4(-1f, -1f, -1f, -1f), new Vector4(1f, 1f, 1f, 1f));

			//speed = Vector2.Normalize(speed);
			return true;
		}

		private void ComSleepTillCanRequest()
		{
			//check if limit not passed
			while (_lastComTimeStamp + MinSpanEveryCom > System.DateTime.Now)
			{
				Thread.Sleep(69);
			}
		}

		private void ComRequestSleep()
		{
			_lastComTimeStamp = System.DateTime.Now;
		}

		public void Dispose()
		{
			_cts.Cancel();
			_ptzThread?.Join(1000);
			_cts.Dispose();
			_ptzThread = null;
			_dataMutex.Dispose();
		}
	}
}
