using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.ServiceModel;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Godot;
using OpenCvSharp;
using RoverControlApp.MVVM.ViewModel;
using RoverControlApp.Core;

namespace RoverControlApp.MVVM.Model
{
	public class RtspStreamClient : IDisposable
	{
		public VideoCapture? Capture { get; private set; }
		private Image? _latestImage;
		private Mat? m;


		public Image LatestImage
		{
			get
			{
				NewFrameSaved = false;
				return _latestImage;
			}
			private set
			{
				NewFrameSaved = true;
				_latestImage = value;
			}
		}

		private volatile Stopwatch _generalPurposeStopwatch;
		public double ElapsedSecondsOnCurrentState => _generalPurposeStopwatch.Elapsed.TotalSeconds;

		public volatile bool NewFrameSaved;

		private Thread? _rtspThread;

		public CommunicationState State
		{
			get => _state;
			private set
			{
				EventLogger.LogMessage("RtspStreamClient", EventLogger.LogLevel.Info, $"CommunicationState update: {value}");
				_state = value;
			}
		}

		private volatile CommunicationState _state = CommunicationState.Closed;

		private CancellationTokenSource _cts;

		public RtspStreamClient()
		{
			_generalPurposeStopwatch = Stopwatch.StartNew();
			_cts = new CancellationTokenSource();
			_rtspThread = new Thread(ThreadWork) { IsBackground = true, Name = "RtspStream_Thread", Priority = ThreadPriority.BelowNormal };
			_rtspThread.Start();
		}

		private void ThreadWork()
		{
			EventLogger.LogMessage("RtspStreamClient", EventLogger.LogLevel.Verbose, "Thread started");
			while (!_cts.IsCancellationRequested)
			{
				DoWork();
			}
			State = CommunicationState.Closing;
			DoWork();
		}

		public void Dispose()
		{
			EventLogger.LogMessage("RtspStreamClient", EventLogger.LogLevel.Verbose, "Dispose called... Closing client");
			_cts.Cancel();
			_rtspThread?.Join(1000);
			_cts.Dispose();
			_rtspThread = null;
		}

		private void EndCapture()
		{
			Capture?.Release();
			Capture?.Dispose();
			Capture = null;
			m?.Dispose();
		}

		private void CreateCapture()
		{
			if (Capture != null) EndCapture();
			State = CommunicationState.Created;
			var task = 
				Task.Run(() => Capture = new VideoCapture
				(
					$"rtsp://{LocalSettings.Singleton.Camera.ConnectionSettings.Login}:{LocalSettings.Singleton.Camera.ConnectionSettings.Login}"
					+ $"@{LocalSettings.Singleton.Camera.ConnectionSettings.Ip}:{LocalSettings.Singleton.Camera.ConnectionSettings.RtspPort}{LocalSettings.Singleton.Camera.ConnectionSettings.RtspStreamPath}")
				);
			m = new Mat();
			_generalPurposeStopwatch.Restart();
			State = CommunicationState.Opening;
			if (!task.Wait(TimeSpan.FromSeconds(15)) || Capture == null || !Capture.IsOpened())
			{
				EventLogger.LogMessage("RtspStreamClient", EventLogger.LogLevel.Error, $"RTSP: Connecting to camera failed after {(int)_generalPurposeStopwatch.Elapsed.TotalSeconds}s");
				State = CommunicationState.Faulted;
				EndCapture();
				return;
			}

			EventLogger.LogMessage("RtspStreamClient", EventLogger.LogLevel.Info, $"Connecting to camera succeeded in {(int)_generalPurposeStopwatch.Elapsed.TotalSeconds}s");

			Capture?.Set(VideoCaptureProperties.XI_Timeout, 5000);
			Capture?.Set(VideoCaptureProperties.BufferSize, 0);
			Capture?.SetExceptionMode(false);
			State = CommunicationState.Opened;
		}

		private void DoWork()
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
					var ret = TryGrabImage();

					if (!ret || _generalPurposeStopwatch.Elapsed.TotalSeconds > 5)
					{
						EventLogger.LogMessage("RtspStreamClient", EventLogger.LogLevel.Error, $"RTSP: Camera connection lost ;( Grabbing a frame took {(int)_generalPurposeStopwatch.Elapsed.TotalSeconds}s");
						State = CommunicationState.Faulted;
						EndCapture();
						return;
					}
					break;
				case CommunicationState.Closing:
					EndCapture();
					State = CommunicationState.Closed;
					break;
				case CommunicationState.Closed:
					if (!_cts.IsCancellationRequested) CreateCapture();
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

		private System.Threading.Mutex _grabFrameMutex = new();

		public void LockGrabbingFrames()
		{
			_grabFrameMutex.WaitOne();
		}
		public void UnLockGrabbingFrames()
		{
			_grabFrameMutex.ReleaseMutex();
		}


		private byte[]? _arr;

		private bool TryGrabImage()
		{
			if (Capture == null || m == null) return false;

			try
			{
				if (!Capture.Grab()) return false;
				if (!Capture.Retrieve(m)) return false;
			}
			catch (Exception e)
			{
				EventLogger.LogMessage("RtspStreamClient", EventLogger.LogLevel.Error, e.ToString());
				return false;
			}


			Cv2.CvtColor(m, m, ColorConversionCodes.BGR2RGB);

			if (_arr?.Length != m.Total() * m.Channels())
				_arr = new byte[m.Total() * m.Channels()];



			Marshal.Copy(m.Data, _arr, 0, (int)m.Total() * m.Channels());

			LockGrabbingFrames();
			if (LatestImage?.GetWidth() != m.Width && LatestImage?.GetHeight() != m.Height)
				LatestImage = Image.CreateFromData(m.Width, m.Height, false, Image.Format.Rgb8, _arr);
			else
				LatestImage.SetData(m.Width, m.Height, false, Image.Format.Rgb8, _arr);
			NewFrameSaved = true;
			UnLockGrabbingFrames();

			EventLogger.LogMessageDebug("RtspStreamClient", EventLogger.LogLevel.Verbose, $"Frame received in: {_generalPurposeStopwatch.ElapsedMilliseconds}ms");

			return true;
		}
	}
}