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
using RtspClientSharp.RawFrames;

namespace OnvifCameraControlTest
{
	public  class OnvifCameraWebStream
	{
		public OnvifCameraWebStream()
		{
			var serverUri = new Uri("rtsp://192.168.5.35:554/live/0/MAIN");
			//var serverUri = new Uri("rtsp://admin:admin@192.168.5.35:554/live/0/MAIN");
			var credentials = new NetworkCredential("admin", "admin");

			var connectionParameters = new ConnectionParameters(serverUri, credentials);
			var cancellationTokenSource = new CancellationTokenSource();

			Task connectTask = ConnectAsync(connectionParameters, cancellationTokenSource.Token);


			Console.WriteLine("Press any key to cancel");
			Console.ReadLine();

			cancellationTokenSource.Cancel();

			Console.WriteLine("Canceling");
			connectTask.Wait(CancellationToken.None);
		}

		List<RawFrame>? ReadFromSavedFrames(string str)
		{
			return JsonSerializer.Deserialize<List<RawFrame>>(str);
		}

		static void OnFrameReceived(object? sender, RawFrame frame)
		{
			//Do smth
		}

		private static async Task ConnectAsync(ConnectionParameters connectionParameters, CancellationToken token)
		{
			try
			{
				TimeSpan delay = TimeSpan.FromSeconds(5);

				using (var rtspClient = new RtspClient(connectionParameters))
				{
					rtspClient.FrameReceived +=
						(sender, frame) => Console.WriteLine($"New frame {frame.Timestamp}: {frame.GetType().Name}");
					rtspClient.FrameReceived += OnFrameReceived;

					while (true)
					{
						Console.WriteLine("Connecting...");

						try
						{
							await rtspClient.ConnectAsync(token);
						}
						catch (OperationCanceledException)
						{
							return;
						}
						catch (RtspClientException e)
						{
							Console.WriteLine(e.ToString());
							await Task.Delay(delay, token);
							continue;
						}

						Console.WriteLine("Connected.");

						try
						{
							await rtspClient.ReceiveAsync(token);
							//rtspClient.
						}
						catch (OperationCanceledException)
						{
							return;
						}
						catch (RtspClientException e)
						{
							Console.WriteLine(e.ToString());
							await Task.Delay(delay, token);
						}
					}
				}
			}
			catch (OperationCanceledException)
			{
			}
		}


		//static async Task Main(string[] args)
		//{
		//	Uri RTSPURL = new Uri("http://81.187.169.213/mjpg/1/video.mjpg?camera=1&timestamp=1668882353161");
		//	CancellationToken token = new CancellationToken();
		//	var credentials = new NetworkCredential("", "");
		//	var connectionParameters = new ConnectionParameters(RTSPURL, credentials);
		//	connectionParameters.RtpTransport = RtpTransportProtocol.TCP;

		//	using (var rtspClient = new RtspClient(connectionParameters))
		//	{
		//		await rtspClient.ConnectAsync(token);
		//		await rtspClient.ReceiveAsync(token);
		//		Console.WriteLine("Using RTSPClient");

		//		rtspClient.FrameReceived += (sender, frame) =>
		//		{
		//			Console.WriteLine("Got Frame");
		//			using (MemoryStream memStream = new MemoryStream(frame.FrameSegment.Array, 0, frame.FrameSegment.Array.Count(), true))
		//			{

		//				var bmp = new Bitmap(memStream);
		//			}

		//		};
		//	}
		//}
	}
}
