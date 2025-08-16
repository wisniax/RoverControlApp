using Godot;
using Microsoft.MixedReality.WebRTC;
using RoverControlApp.Core;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace RoverControlApp.MVVM.Model
{
	public class WebRtcClient
	{
		private PeerConnection _peerConnection;
		private RemoteVideoTrack _remoteVideoTrack;
		private byte[] _frameBuffer;
		private Image _latestImage;
		private bool _newFrameAvailable;

		public Image LatestImage => _latestImage;
		public bool NewFrameAvailable
		{
			get => _newFrameAvailable;
			set => _newFrameAvailable = value;
		}

		public async Task InitializeAsync(string stunServer)
		{
			if (string.IsNullOrEmpty(stunServer))
				stunServer = LocalSettings.Singleton.WebRTCStream.IceServer;

			_peerConnection = new PeerConnection();

			var config = new PeerConnectionConfiguration
			{
				IceServers = new List<IceServer>
				{
					new IceServer { Urls = { stunServer } }
				}
			};

			await _peerConnection.InitializeAsync(config);

			_peerConnection.VideoTrackAdded += (RemoteVideoTrack track) =>
			{
				EventLogger.LogMessage("WebRtcClient", EventLogger.LogLevel.Info, $"Video Track Added");
				_remoteVideoTrack = track;
				track.I420AVideoFrameReady += OnFrameReady;
			};
		}

		public async Task<string> CreateOfferAsync()
		{
			var tcs = new TaskCompletionSource<string>();

			void Handler(SdpMessage message)
			{
				if (message.Type == SdpMessageType.Offer)
				{
					tcs.TrySetResult(message.Content);
				}
			}

			_peerConnection.LocalSdpReadytoSend += Handler;

			bool success = _peerConnection.CreateOffer();
			if (!success)
			{
				_peerConnection.LocalSdpReadytoSend -= Handler;
				throw new Exception("Cannot craete SDP offer");
			}

			var offer = await tcs.Task;
			_peerConnection.LocalSdpReadytoSend -= Handler;
			return offer;
		}

		public async Task SetRemoteAnswerAsync(string answerSdp)
		{
			var remoteDesc = new SdpMessage
			{
				Type = SdpMessageType.Answer,
				Content = answerSdp
			};
			await _peerConnection.SetRemoteDescriptionAsync(remoteDesc);
		}

		private void OnFrameReady(I420AVideoFrame frame)
		{
			try
			{
				int width = (int)frame.width;
				int height = (int)frame.height;

				if (_frameBuffer == null || _frameBuffer.Length != width * height * 3)
					_frameBuffer = new byte[width * height * 3];

				_latestImage = ConvertI420ToRgb(frame);
				_newFrameAvailable = true;
			}
			catch (Exception ex)
			{
				throw new Exception($"OnFrameReady error: {ex}");
			}
		}

		private Image ConvertI420ToRgb(I420AVideoFrame frame)
		{
			int width = (int)frame.width;
			int height = (int)frame.height;

			IntPtr yPlane = frame.dataY;
			IntPtr uPlane = frame.dataU;
			IntPtr vPlane = frame.dataV;

			int strideY = (int)frame.strideY;
			int strideU = (int)frame.strideU;
			int strideV = (int)frame.strideV;

			byte[] rgbData = new byte[width * height * 3];
			int rgbIndex = 0;

			for (int j = 0; j < height; j++)
			{
				int yRow = j * strideY;
				int uRow = (j / 2) * strideU;
				int vRow = (j / 2) * strideV;

				for (int i = 0; i < width; i++)
				{
					byte Y = Marshal.ReadByte(yPlane, yRow + i);
					byte U = Marshal.ReadByte(uPlane, uRow + (i / 2));
					byte V = Marshal.ReadByte(vPlane, vRow + (i / 2));

					int C = Y - 16;
					int D = U - 128;
					int E = V - 128;

					int R = (298 * C + 409 * E + 128) >> 8;
					int G = (298 * C - 100 * D - 208 * E + 128) >> 8;
					int B = (298 * C + 516 * D + 128) >> 8;

					rgbData[rgbIndex++] = (byte)Math.Clamp(R, 0, 255);
					rgbData[rgbIndex++] = (byte)Math.Clamp(G, 0, 255);
					rgbData[rgbIndex++] = (byte)Math.Clamp(B, 0, 255);
				}
			}

			Image img = Image.CreateFromData(width, height, false, Image.Format.Rgb8, rgbData);
			return img;
		}

		public async Task<string> ExchangeOfferWithServerAsync(string serverUrl)
		{
			_peerConnection.AddTransceiver(MediaKind.Video, new TransceiverInitSettings
			{
				Name = "video_recv", 
				InitialDesiredDirection = Transceiver.Direction.ReceiveOnly
			});
			
			var localOffer = await CreateOfferAsync();

			var httpClient = new Godot.HttpClient();
			var url = "http://"+serverUrl;
			var headers = new string[] { "Content-Type: application/json" };
			var payload = new { sdp = localOffer, type = "offer" };
			var json = JsonSerializer.Serialize(payload);
			var body = Encoding.UTF8.GetBytes(json);

			var uri = new Uri(url);
			var error = httpClient.ConnectToHost(uri.Host, uri.Port, uri.Scheme == "https" ? Godot.TlsOptions.ClientUnsafe() : null);
			if (error != Error.Ok)
				throw new Exception("Nie można połączyć z serwerem: " + error);

			EventLogger.LogMessageDebug("WebRtcClient", EventLogger.LogLevel.Verbose, "Connecting to server");
			while (httpClient.GetStatus() == Godot.HttpClient.Status.Connecting || httpClient.GetStatus() == Godot.HttpClient.Status.Resolving)
			{
				httpClient.Poll();
				await Task.Delay(10); 
			}

			if (httpClient.GetStatus() != Godot.HttpClient.Status.Connected)
			{
				throw new Exception($"Unable to connect to server. Status: {httpClient.GetStatus()}");
			}

			string path = uri.AbsolutePath;
			GD.Print(path);
			GD.Print(uri);
			GD.Print(headers);
			GD.Print(body);
			error = httpClient.RequestRaw(Godot.HttpClient.Method.Post, "/offer", headers, body);
			if (error != Error.Ok)
				throw new Exception("Error sending request: " + error);

			while (httpClient.GetStatus() != Godot.HttpClient.Status.Body)
			{
				httpClient.Poll();
				await Task.Delay(10);
			}

			var responseCode = httpClient.GetResponseCode();
			if (responseCode != (int)Godot.HttpClient.ResponseCode.Ok)
				throw new Exception("Error server response: " + responseCode);

			var responseBytes = new List<byte>();
			while (httpClient.GetStatus() == Godot.HttpClient.Status.Body)
			{
				httpClient.Poll();
				var chunk = httpClient.ReadResponseBodyChunk();
				if (chunk != null && chunk.Length > 0)
				{
					responseBytes.AddRange(chunk);
				}
				else
				{
					await Task.Delay(10);
				}
			}
			
			if (responseBytes.Count == 0)
			{
				throw new Exception("Get empty response from server.");
			}
	
			var respText = Encoding.UTF8.GetString(responseBytes.ToArray());
			
			var dict = JsonSerializer.Deserialize<Dictionary<string, string>>(respText);
			if (dict != null && dict.TryGetValue("sdp", out string answerSdp))
			{
				await SetRemoteAnswerAsync(answerSdp);
				return answerSdp;
			}
			else
			{
				throw new Exception("Lack of field sdp in server answer");
			}
		}

		public void Dispose()
		{
			try
			{
				if (_remoteVideoTrack != null)
					_remoteVideoTrack.I420AVideoFrameReady -= OnFrameReady;
			}
			catch { }
			_peerConnection?.Close();
			_peerConnection?.Dispose();
		}
	}
}
