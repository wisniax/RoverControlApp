using Godot;
using OpenCvSharp;
using RoverControlApp.Core;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.ServiceModel;
using System.Threading;
using System.Threading.Tasks;

namespace RoverControlApp.MVVM.Model
{
	public class RtspStreamClient : IDisposable
	{
		private readonly CancellationTokenSource _cts;

		private volatile Stopwatch _generalPurposeStopwatch;
		private volatile bool _newFrameSaved;
		private volatile CommunicationState _state = CommunicationState.Closed;

		private Image? _latestImage;
		private Mat? _matrix;
		private Thread? _rtspThread;

		public int id { get; set; }
		public bool isBig = false;

		public VideoCapture? Capture { get; private set; }

		public Image LatestImage
		{
			get
			{
				_newFrameSaved = false;
				return _latestImage;
			}
			private set
			{
				_newFrameSaved = true;
				_latestImage = value;
			}
		}

		public double ElapsedSecondsOnCurrentState => _generalPurposeStopwatch.Elapsed.TotalSeconds;
		
		public bool NewFrameSaved => _newFrameSaved;

		public CommunicationState State
		{
			get => _state;
			private set
			{
				EventLogger.LogMessage("RtspStreamClient", EventLogger.LogLevel.Info, $"CommunicationState update: {value}");
				_state = value;
			}
		}

		public RtspStreamClient(int id)
		{
			this.id = id;
			if (id == 0) isBig = true;

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
			_matrix?.Dispose();
		}

		private void CreateCapture()
		{
			if (Capture != null) EndCapture();
			State = CommunicationState.Created;
			string link = GetNewRTSPLink();
			var task = Task.Run(() => Capture = new VideoCapture(link));
			_matrix = new Mat();
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

		public void SetStateClosing()
		{
			State = CommunicationState.Closing;
		}

		private string GetNewRTSPLink()
		{
			switch (id)
			{
				case 0:
					return LocalSettings.Singleton.Camera0.ConnectionSettings.RtspLink;
				case 1:
					return LocalSettings.Singleton.Camera1.ConnectionSettings.RtspLink;
				default:
					return LocalSettings.Singleton.Camera0.ConnectionSettings.RtspLink;
			}
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

		private readonly System.Threading.Mutex _grabFrameMutex = new();

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
			if (Capture == null || _matrix == null) return false;

			try
			{
				if (!Capture.Grab()) return false;
				if (!Capture.Retrieve(_matrix)) return false;
			}
			catch (Exception e)
			{
				EventLogger.LogMessage("RtspStreamClient", EventLogger.LogLevel.Error, e.ToString());
				return false;
			}


			Cv2.CvtColor(_matrix, _matrix, ColorConversionCodes.BGR2RGB);

			if (_arr?.Length != _matrix.Total() * _matrix.Channels())
				_arr = new byte[_matrix.Total() * _matrix.Channels()];



			Marshal.Copy(_matrix.Data, _arr, 0, (int)_matrix.Total() * _matrix.Channels());

			LockGrabbingFrames();
			if (LatestImage?.GetWidth() != _matrix.Width && LatestImage?.GetHeight() != _matrix.Height)
				LatestImage = Image.CreateFromData(_matrix.Width, _matrix.Height, false, Image.Format.Rgb8, _arr);
			else
				LatestImage.SetData(_matrix.Width, _matrix.Height, false, Image.Format.Rgb8, _arr);
			_newFrameSaved = true;
			UnLockGrabbingFrames();

			EventLogger.LogMessageDebug("RtspStreamClient", EventLogger.LogLevel.Verbose, $"Frame received in: {_generalPurposeStopwatch.ElapsedMilliseconds}ms");

			return true;
		}

		public void MarkFrameOld()
		{
			_newFrameSaved = false;
		}
	}
}