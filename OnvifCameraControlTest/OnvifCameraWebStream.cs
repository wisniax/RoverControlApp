using RtspClientSharp.Rtsp;
using RtspClientSharp;
using RtspClientSharp.RawFrames.Video;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Text.Json;
using System.Drawing.Imaging;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.Util;
using Godot;
using RtspClientSharp.RawFrames;

namespace OnvifCameraControlTest
{
	public class OnvifCameraWebStream
	{
		static readonly object Identity = new object();
		public VideoCapture Capture { get; private set; }
		private Texture2D _latestImage;
		public volatile bool NewFrameSaved;

		public Texture2D LatestImage
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

		//public Bitmap LatestBitmap { get; private set; }
		public OnvifCameraWebStream()
		{
			var configTuple = new Tuple<CapProp, int>(CapProp.HwAcceleration, (int)VideoAccelerationType.Any);
			//Capture = new VideoCapture("rtsp://admin:admin@192.168.5.35:554/live/0/MAIN", VideoCapture.API.Ffmpeg, configTuple);
			Capture = new VideoCapture("rtsp://admin:admin@192.168.5.35:554/live/0/MAIN", VideoCapture.API.Ffmpeg, configTuple);
			Capture.Set(CapProp.Buffersize, 0);
			//Capture.Set(CapProp.)
			Capture.ExceptionMode = true;
			//Capture.Set(CapProp.ConvertRgb, 1);
			Capture.ImageGrabbed += CurrentDevice_ImageGrabbed;

			//capture.
			//capture.SetCaptureProperty(CapProp.Fps, 0);
			var ex = new EmguExceptionHandler();
			Capture.Start(ex);
		}

		private void CurrentDevice_ImageGrabbed(object? sender, EventArgs e)
		{
			//Console.WriteLine("Frame Received");
			using Mat m = new Mat();
			//using Mat m2 = new Mat();
			if (!Capture.Retrieve(m, 0)) return;
			//while (Capture.Retrieve(m2)) ;
			byte[] arr = new byte[(int)m.Total * m.NumberOfChannels];
			//m.ToBitmap()
			//m.GetData(false);
			//m.ConvertTo(m, DepthType.Cv8U);
			Emgu.CV.CvInvoke.CvtColor(m, m, ColorConversion.Bgr2Rgb);
			m.CopyTo(arr);
			//m.ToImage<>();
			var image = Image.CreateFromData(m.Width, m.Height, false, Image.Format.Rgb8, arr);
			LatestImage = ImageTexture.CreateFromImage(image);

			//LatestBitmap = m.ToBitmap();
			//using MemoryStream ms = new MemoryStream();
			//LatestBitmap.Save(ms, ImageFormat.Bmp);
			//cos.Save(@"C:\Users\Admin\source\repos\doUsuniecia\doUsuniecia\latestFrame.bmp");

		}


		class EmguExceptionHandler : ExceptionHandler
		{
			public override bool HandleException(Exception ex)
			{
				// Handle the exception as needed
				GD.Print($"Exception caught by my fancy class: {ex.Message}");
				//Console.WriteLine($"Exception caught by my fancy class: {ex.Message}");

				// Return true if the exception has been handled, or false if it should be rethrown
				// and the application terminated.
				return true;
			}
		}


	}
}
