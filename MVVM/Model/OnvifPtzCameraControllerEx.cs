using Godot;
using Onvif.Core.Client;
using Onvif.Core.Client.Common;
using RoverControlApp.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.ServiceModel;
using System.Threading;
using Mutex = System.Threading.Mutex;

namespace RoverControlApp.MVVM.Model
{
	public class OnvifPtzCameraControllerEx
	{
		private readonly long COM_TIME_RANGE = 1000; //ms
		private readonly int COM_LIMIT_IN_RANGE = 4; //count

		private struct MoveUpdateData
		{
			public Vector4 TiltAndZoom = Vector4.Zero;
			public bool StopTilt = false;
			public bool StopZoom = false;

			public MoveUpdateData(Vector4 tiltAndZoom, bool stopTilt, bool stopZoom)
			{
				TiltAndZoom = tiltAndZoom;
				StopTilt = stopTilt;
				StopZoom = stopZoom;
			}

			public static explicit operator PTZSpeed(MoveUpdateData data)
			{
				return new()
				{
					PanTilt =
					new()
					{
						x = data.TiltAndZoom.X,
						y = data.TiltAndZoom.Y
					},
					Zoom =
					new()
					{
						x = data.TiltAndZoom.Z
					}
				};
			}

			public int ComWeight => (StopTilt || StopZoom ? 1 : 0)
				+ (TiltAndZoom.IsZeroApprox() ? 1 : 0);

		}

		private Camera? _camera = null;

		private Vector4 _cameraMotionRequest = Vector4.Zero;
		private Vector4 _cameraMotionLast = Vector4.Zero;

		private List<System.DateTimeOffset> _comHistory = new();
		private Queue<MoveUpdateData> _comQueue = new();
		private Barrier _requestBarrier = new(2);

		private CancellationTokenSource _cts;

		private readonly Mutex _dataMutex = new();

		private volatile Stopwatch _generalPurposeStopwatch;
		private volatile Stopwatch _queueStopwatch;


		private Thread _ptzThread;
		private Exception? _ptzThreadError = null;

		private volatile CommunicationState _state;

		public void CameraMotionRequestSubscriber(object? sender, Vector4 vector)
		{
			CameraMotionRequest = vector;
		}

		public OnvifPtzCameraControllerEx()
		{
			_generalPurposeStopwatch = Stopwatch.StartNew();
			_queueStopwatch =  Stopwatch.StartNew();
			_cts = new CancellationTokenSource();
			_ptzThread = new Thread(ThreadWork)
			{
				IsBackground = true,
				Name = "PtzController_Thread",
				Priority = ThreadPriority.AboveNormal
			};
			_ptzThread.Start();
		}

		private void CreateCamera()
		{
			if (_camera != null)
				EndCamera();
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
				EventLogger.LogMessage("OnvifPtzCameraControllerEx", EventLogger.LogLevel.Error, $"Connecting to camera failed after " 
					+ $"{(int)_generalPurposeStopwatch.Elapsed.TotalSeconds}s with error: {_ptzThreadError}");
				State = CommunicationState.Faulted;
				return;
			}

			State = CommunicationState.Opening;
			_camera?.Ptz.StopAsync(_camera.Profile.token, true, true).Wait();
			State = CommunicationState.Opened;
			EventLogger.LogMessage("OnvifPtzCameraControllerEx", EventLogger.LogLevel.Info, $"Connecting to camera succeeded in {(int)_generalPurposeStopwatch.Elapsed.TotalSeconds}s");
		}

		private void EndCamera()
		{
			_ptzThreadError = null;
			_camera = null;
		}

		private void ThreadWork()
		{
			EventLogger.LogMessage("OnvifPtzCameraControllerEx", EventLogger.LogLevel.Verbose, "Thread started");

			while (!_cts.IsCancellationRequested)
			{
				DoWork();
			}
		}

		private void DoWork()
		{
			switch (State)
			{

				case CommunicationState.Opened:
					_generalPurposeStopwatch.Restart();

					bool errCaught = false;

					try
					{
						TryMoveCamera();
					}
					catch (AggregateException e)
					{
						EventLogger.LogMessage("OnvifPtzCameraControllerEx", EventLogger.LogLevel.Error, $"Handled exception {e} caught");
						errCaught = true;
					}

					if (_generalPurposeStopwatch.Elapsed.TotalSeconds > 10 || errCaught)
					{
						EventLogger.LogMessage("OnvifPtzCameraControllerEx", EventLogger.LogLevel.Error, $"Camera connection lost ;( Sending a move request took {(int)_generalPurposeStopwatch.Elapsed.TotalSeconds}s");
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
					if (!_cts.IsCancellationRequested)
						CreateCamera();
					break;
				case CommunicationState.Faulted:
					_generalPurposeStopwatch.Restart();
					Thread.Sleep(TimeSpan.FromSeconds(10));
					State = CommunicationState.Closed;
					break;
				default:
					State = CommunicationState.Closing;
					break;
			}
		}


		private void TryMoveCamera()
		{
			if (_comQueue.Count > 0)
				_requestBarrier.SignalAndWait(200);
			else
				_requestBarrier.SignalAndWait(5000);

			//populate queue
			if (UpdateMotion(_cameraMotionLast, CameraMotionRequest, out MoveUpdateData moveUpdateData) || _queueStopwatch.Elapsed.TotalSeconds >= 5)
			{
				EventLogger.LogMessage("OnvifPtzCameraControllerEx", EventLogger.LogLevel.Verbose, $"Enqueued: Vec: {moveUpdateData.TiltAndZoom} TiltStop: {moveUpdateData.StopTilt} ZoomStop: {moveUpdateData.StopZoom}");
				_comQueue.Enqueue(moveUpdateData);
				_queueStopwatch.Restart();
			}
			_cameraMotionLast = CameraMotionRequest;

			//when move is stopped, clear entire queue
			if ((moveUpdateData.StopTilt || moveUpdateData.StopZoom) && _queueStopwatch.Elapsed.TotalMilliseconds >= 200)
			{
				_comQueue.Clear();
				_comQueue.Enqueue(moveUpdateData);
				_queueStopwatch.Restart();
			}

			if (_comQueue.Count == 0)
				return;

			while (_comQueue.Count > 4)
				_comQueue.Dequeue();

			//clear older than 1000ms
			foreach (DateTimeOffset offset in _comHistory.ToList())
			{
				if (offset.ToUnixTimeMilliseconds() + COM_TIME_RANGE < DateTimeOffset.Now.ToUnixTimeMilliseconds())
					_comHistory.Remove(offset);
			}

			if (COM_LIMIT_IN_RANGE - _comHistory.Count >= _comQueue.Peek().ComWeight)
			{
				MoveUpdateData nextMove = _comQueue.Dequeue();
				if (nextMove.StopTilt || nextMove.StopZoom)
				{
					EventLogger.LogMessage("OnvifPtzCameraControllerEx", EventLogger.LogLevel.Verbose, $"CameraMotion send: {(nextMove.StopTilt ? "StopTilt" : string.Empty)} {(nextMove.StopZoom ? "StopZoom" : string.Empty)}");
					_camera?.Ptz.StopAsync(_camera.Profile.token, nextMove.StopTilt, nextMove.StopZoom).Wait();
					_generalPurposeStopwatch.Restart();
					_comHistory.Add(DateTimeOffset.Now);
				}

				if (!nextMove.TiltAndZoom.IsZeroApprox())
				{
					EventLogger.LogMessage("OnvifPtzCameraControllerEx", EventLogger.LogLevel.Verbose, $"CameraMotion send: Tilt.X={nextMove.TiltAndZoom.X}, Tilt.Y={nextMove.TiltAndZoom.Y} Zoom={nextMove.TiltAndZoom.Z}");
					_camera?.Ptz.ContinuousMoveAsync(_camera.Profile.token, (PTZSpeed)nextMove, string.Empty).Wait();
					_generalPurposeStopwatch.Restart();
					_comHistory.Add(DateTimeOffset.Now);
				}
			}
		}

		private bool UpdateMotion(Vector4 moveOld, Vector4 moveNew, out MoveUpdateData data)
		{
			data = new();

			data.TiltAndZoom = LocalSettings.Singleton.AllCameras.Camera0.InverseAxis
				? new Vector4(-moveNew.X, -moveNew.Y, moveNew.Z, moveNew.W)
				: moveNew;

			//Have to make sure none scalar is |x| <= 0.1f bc camera treats it as a MAX SPEED
			if (Mathf.IsEqualApprox(data.TiltAndZoom.X, 0f, 0.1f))
				data.TiltAndZoom.X = 0f;
			if (Mathf.IsEqualApprox(data.TiltAndZoom.Y, 0f, 0.1f))
				data.TiltAndZoom.Y = 0f;
			if (Mathf.IsEqualApprox(data.TiltAndZoom.Z, 0f, 0.1f))
				data.TiltAndZoom.Z = 0f;
			data.TiltAndZoom = data.TiltAndZoom.Clamp(new Vector4(-1f, -1f, -1f, -1f), new Vector4(1f, 1f, 1f, 1f));

			bool xNow = !Mathf.IsEqualApprox(moveNew.X, 0f, 0.1f); // Is currently moving on x axis
			bool yNow = !Mathf.IsEqualApprox(moveNew.Y, 0f, 0.1f); // Is currently moving on y axis
			bool xBefore = !Mathf.IsEqualApprox(_cameraMotionLast.X, 0f, 0.1f); // Was moving on x axis b4?
			bool yBefore = !Mathf.IsEqualApprox(_cameraMotionLast.Y, 0f, 0.1f); // Was moving on y axis b4?

			data.StopTilt = (!xNow && xBefore) || (!yNow && yBefore) || (!xNow && !yNow); //When to stop camera :)
			data.StopZoom = Mathf.IsEqualApprox(moveNew.Z, 0f, 0.1f) && !Mathf.IsEqualApprox(_cameraMotionLast.Z, 0f, 0.1f);

			if (moveNew.IsEqualApprox(moveOld))
				return false;

			return true;
		}

		public Vector4 CameraMotionRequest
		{
			get
			{
				_dataMutex.WaitOne();
				var copy = _cameraMotionRequest;
				_dataMutex.ReleaseMutex();
				return copy;
			}
			private set
			{
				EventLogger.LogMessage("OnvifPtzCameraControllerEx", EventLogger.LogLevel.Verbose, $"CameraMotion update: {value}");
				_dataMutex.WaitOne();
				_cameraMotionRequest = value;
				_dataMutex.ReleaseMutex();
				bool success = _requestBarrier.SignalAndWait(100);
				if (!success)
					EventLogger.LogMessage("OnvifPtzCameraControllerEx", EventLogger.LogLevel.Warning, $"Barrier timeout! Last input ignored!");
			}
		}

		public double ElapsedSecondsOnCurrentState => _generalPurposeStopwatch.Elapsed.TotalSeconds;

		public CommunicationState State
		{
			get => _state;
			private set
			{
				EventLogger.LogMessage("OnvifPtzCameraControllerEx", EventLogger.LogLevel.Verbose, $"CommunicationState update: {value}");
				_state = value;
			}
		}
	}
}
