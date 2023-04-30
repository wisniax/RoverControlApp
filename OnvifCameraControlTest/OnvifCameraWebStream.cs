﻿using System.Diagnostics;
using Godot;
using OpenCvSharp;
using OpenCvSharp.Internal;
using System.Runtime.InteropServices;

namespace OnvifCameraControlTest
{
	public class OnvifCameraWebStream
	{
		static readonly object Identity = new object();
		public VideoCapture? Capture { get; private set; }
		private Texture2D? _latestImage;
		public volatile bool NewFrameSaved;
		

		ImageTexture? imageTexture;


		private Task? _timerTask;
		private readonly PeriodicTimer _timer;
		private readonly CancellationTokenSource _cts = new();
		//private static Timer _timer;
		private Thread _thread;
		//private Mat m;
		private string _ip;
		private string _port;
		private string _protocol;
		private string _login;
		private string _password;
		private string _pathToStream;
		//public static EventHandler<VideoCapture?> OnCaptureThreadCrashed;

		public Texture2D? LatestImage
		{
			get
			{
				lock (Identity)
				{
					return _latestImage;
				}
			}
			set
			{
				lock (Identity)
				{
					NewFrameSaved = true;
					_latestImage = value;
				}
			}
		}

		public void StartCapture()
		{
			//var m = new Mat();

			//var configTuple = new Tuple<CapProp, int>(CapProp.HwAcceleration, (int)VideoAccelerationType.Any);
			Capture = new VideoCapture($"{_protocol}://{_login}:{_password}@{_ip}:{_port}{_pathToStream}", VideoCaptureAPIs.FFMPEG);
			//Capture = new VideoCapture($"rtsp://admin:admin@192.168.5.35/live/0/MAIN", VideoCapture.API.Ffmpeg, configTuple);
			//Capture.Set(CapProp.XiColorFilterArray, 4);
			Capture.Set(VideoCaptureProperties.HwAcceleration, 1);
			Capture.Set(VideoCaptureProperties.BufferSize, 1);
			//Capture.Set(VideoCaptureProperties.OPENNI2_Sync, 0);
			//Capture.Set(VideoCaptureProperties.GStreamerQueueLength, 0);
			//Capture.Set(VideoCaptureProperties.Fps, 2);
			Capture.SetExceptionMode(true);
			//Capture.Set(VideoCaptureProperties.Mode, 1);
			//Capture.Set(CapProp.Buffersize, 0);
			//Capture.ExceptionMode = true;



			//Capture
			//Capture.ImageGrabbed += CurrentDevice_ImageGrabbed;
			//var ex = new EmguExceptionHandler(this);
			//Capture.Start(ex);
		}


		/// <summary>
		/// The form is {protocol}://{login}:{password}@{ip}:{port}{pathToStream}
		/// </summary>
		/// <param name="login"></param>
		/// <param name="password"></param>
		/// <param name="pathToStream"></param>
		/// <param name="ip"></param>
		/// <param name="protocol"></param>
		/// <param name="port"></param>
		public OnvifCameraWebStream(string login, string password, string pathToStream, string ip, string protocol = "rtsp", string port = "554")
		{
			this._ip = ip;
			this._port = port;
			this._protocol = protocol;
			this._login = login;
			this._password = password;
			this._pathToStream = pathToStream;
			StartCapture();
			_thread = new Thread(KeepGrabbingFrames);
			//_thread.IsBackground = true;
			_thread.Priority = ThreadPriority.AboveNormal;
			_thread.Start();
			//_timer = new(TimeSpan.FromMilliseconds(20));
			//StartTimer();


		}

		//public void CurrentDevice_ImageGrabbed(object? sender, EventArgs e)
		//{
		//	_timer.Stop();
		//	GD.Print("Timer Ticked");
		//	using Mat m = new Mat();
		//	if (Capture == null)
		//	{
		//		_timer.Start();
		//		return;
		//	}
		//	GD.Print("Frame read attempted");
		//	if (!Capture.Read(m))
		//	{
		//		_timer.Start();
		//		return;
		//	}

		//	GD.Print("Frame Received");
		//	byte[] arr = new byte[m.Total() * m.Channels()];
		//	Cv2.CvtColor(m, m, ColorConversionCodes.BGR2RGB);
		//	GD.Print("Frame Converted");
		//	//m.CopyTo(arr);
		//	//m.ToMemoryStream(".bmp", new ImageEncodingParam(IMwri))
		//	Marshal.Copy(m.Data, arr, 0, (int)m.Total() * m.Channels());
		//	GD.Print("Frame Copied");
		//	var image = Image.CreateFromData(m.Width, m.Height, false, Image.Format.Rgb8, arr);
		//	LatestImage = ImageTexture.CreateFromImage(image);
		//	_timer.Start();
		//}

		private void KeepGrabbingFrames()
		{
			while (true)
			{
				TryGrabImage();
			}
		}

		private void TryGrabImage()
		{
			using Mat m = new Mat();

			if (Capture == null) return;
			Stopwatch watch = Stopwatch.StartNew();

			if (!Capture.Grab())
				return;

			if (!Capture.Retrieve(m))
				return;

			Cv2.CvtColor(m, m, ColorConversionCodes.BGR2RGB);
			//if (_arr?.Length != m.Total() * m.Channels())
			var _arr = new byte[m.Total() * m.Channels()];
			
			Marshal.Copy(m.Data, _arr, 0, (int)m.Total() * m.Channels());
			
			var image = Image.CreateFromData(m.Width, m.Height, false, Image.Format.Rgb8, _arr);
			if (imageTexture == null) imageTexture = ImageTexture.CreateFromImage(image);
			else imageTexture.Update(image);
			
			LatestImage = imageTexture;
			GD.Print($"{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}: Frame received in: {watch.ElapsedMilliseconds}ms");
		}

		public void StartTimer()
		{
			_timerTask = DoWorkAsync();
		}

		private async Task DoWorkAsync()
		{
			try
			{
				while (await _timer.WaitForNextTickAsync(_cts.Token)) TryGrabImage();
			}
			catch (OperationCanceledException)
			{
			}
		}

		public async Task StopTimerAsync()
		{
			if (_timerTask is null) return;
			_cts.Cancel();
			await _timerTask;
			_cts.Dispose();
			GD.Print("Video Capture Cancelled");
		}

		//class EmguExceptionHandler : ExceptionHandler
		//{
		//	private object? _obj;

		//	public EmguExceptionHandler(object sender)
		//	{
		//		_obj = sender;
		//	}
		//	public override bool HandleException(Exception? ex)
		//	{
		//		// Handle the exception as needed
		//		GD.Print($"Exception caught by my fancy class: {ex?.Message}");
		//		//ex = null;
		//		if (_obj is OnvifCameraWebStream cam && cam.Capture != null)
		//		{
		//			cam.Capture.ImageGrabbed -= cam.CurrentDevice_ImageGrabbed;
		//			cam.Capture.Release();
		//			cam.Capture.Dispose();
		//			cam.Capture = null;
		//		}
		//		Thread.Sleep(500);
		//		(_obj as OnvifCameraWebStream)?.StartCapture();
		//		return true;
		//	}
		//}
	}
}
