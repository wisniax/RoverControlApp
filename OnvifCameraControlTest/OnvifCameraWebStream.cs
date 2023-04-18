using RtspClientSharp.Rtsp;
using RtspClientSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;

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

		private static async Task ConnectAsync(ConnectionParameters connectionParameters, CancellationToken token)
		{
			try
			{
				TimeSpan delay = TimeSpan.FromSeconds(5);

				using (var rtspClient = new RtspClient(connectionParameters))
				{
					rtspClient.FrameReceived +=
						(sender, frame) => Console.WriteLine($"New frame {frame.Timestamp}: {frame.GetType().Name}");

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
	}
}
