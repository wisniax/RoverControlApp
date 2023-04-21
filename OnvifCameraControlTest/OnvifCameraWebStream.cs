using Godot;
using OpenCvSharp;
using OpenCvSharp.Internal;
using System.Runtime.InteropServices;
using System.Timers;
using Timer = System.Timers.Timer;

namespace OnvifCameraControlTest
{
	public class OnvifCameraWebStream
	{
		static readonly object Identity = new object();
		public VideoCapture? Capture { get; private set; }
		private Texture2D? _latestImage;
		public volatile bool NewFrameSaved;
		private static Timer _timer;

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
			//var configTuple = new Tuple<CapProp, int>(CapProp.HwAcceleration, (int)VideoAccelerationType.Any);
			Capture = new VideoCapture($"{_protocol}://{_login}:{_password}@{_ip}:{_port}{_pathToStream}", VideoCaptureAPIs.FFMPEG);
			//Capture = new VideoCapture($"rtsp://admin:admin@192.168.5.35/live/0/MAIN", VideoCapture.API.Ffmpeg, configTuple);
			//Capture.Set(CapProp.XiColorFilterArray, 4);
			Capture.Set(VideoCaptureProperties.BufferSize, 0);
			//Capture.SetExceptionMode(true);
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
			_timer = new Timer(19);
			_timer.Elapsed += CurrentDevice_ImageGrabbed;
			_timer.Start();
		}

		public void CurrentDevice_ImageGrabbed(object? sender, EventArgs e)
		{
			_timer.Stop();
			using Mat m = new Mat();
			if (Capture == null)
			{
				_timer.Start();
				return;
			}

			if (!Capture.Read(m))
			{
				_timer.Start();
				return;
			}

			byte[] arr = new byte[m.Total() * m.Channels()];
			Cv2.CvtColor(m, m, ColorConversionCodes.BGR2RGB);
			//m.CopyTo(arr);
			//m.ToMemoryStream(".bmp", new ImageEncodingParam(IMwri))
			Marshal.Copy(m.Data, arr, 0, (int)m.Total() * m.Channels());
			var image = Image.CreateFromData(m.Width, m.Height, false, Image.Format.Rgb8, arr);
			LatestImage = ImageTexture.CreateFromImage(image);
			_timer.Start();
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
