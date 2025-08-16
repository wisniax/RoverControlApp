using Godot;
using RoverControlApp.Core;
using RoverControlApp.MVVM.Model;
using System;
using System.Threading.Tasks;

namespace RoverControlApp.MVVM.ViewModel
{
	public partial class WebRtcStreamDispScript : Node, IDisposable
	{
		private WebRtcClient _webRtcClient;
		private TextureRect _videoDisplay;
		private Button _startStreamButton = null!, _stopStreamButton = null!, _minimiseWindow = null!;
		private bool _streamStarted = false;
		private int _textureMaxWidht = 800;
		private int _textureMaxHeight = 600;

		public override void _Ready()
		{
			_videoDisplay = GetNode<TextureRect>("MarginContainer/VBoxContainer/VideoDisplay");
			GD.Print("WebRtcStreamDispScript " + _videoDisplay.Name);
			var buttonContainer = GetNode<HBoxContainer>("MarginContainer/VBoxContainer/HBoxContainer");


			_startStreamButton = buttonContainer.GetNode<Button>("StartStream");
			_stopStreamButton = buttonContainer.GetNode<Button>("StopStream");
			_minimiseWindow = buttonContainer.GetNode<Button>("MinimiseWindow");		

			_videoDisplay.Resized += OnWindowResized;
			_startStreamButton.Pressed += OnStartStreamButtonPressed;
			_stopStreamButton.Pressed += OnStopStreamButtonPressed;
			_minimiseWindow.Pressed += OnMinimiseWindowPressed;
		}

		private async void OnStartStreamButtonPressed()
		{
			if (_streamStarted) return;
			_streamStarted = true;
			_webRtcClient = new WebRtcClient();
			await _webRtcClient.InitializeAsync(LocalSettings.Singleton.WebRTCStream.IceServer);
			await _webRtcClient.ExchangeOfferWithServerAsync(LocalSettings.Singleton.WebRTCStream.SignalingServer);
			_videoDisplay.Visible = true;
		}

		private void OnStopStreamButtonPressed()
		{
			if (!_streamStarted) return;
			_streamStarted = false;
			_videoDisplay.Visible = false;
			_webRtcClient?.Dispose();
			_webRtcClient = null;
		}

		private void OnMinimiseWindowPressed()
		{
			Window window = GetParent<Window>();

			if(window != null)
			{
				window.Visible = false;
			}
		}

		private void OnWindowResized()
		{
			if (_videoDisplay != null)
			{
				_textureMaxWidht = Math.Max((int)_videoDisplay.Size.X,500);
				_textureMaxHeight = Math.Max((int)_videoDisplay.Size.Y, 500);
				GD.Print("WebRtcStreamDispScript: Window resized, new max width: ", _textureMaxWidht, ", new max height: ", _textureMaxHeight);
			}
			else
			{
				GD.PrintErr("VideoDisplay node not found.");
			}
		}

		public override void _Process(double delta)
		{
			if (_webRtcClient != null && _webRtcClient.NewFrameAvailable)
			{
				var img = _webRtcClient.LatestImage;
				if (img != null)
				{
					if(img.GetWidth() > _textureMaxWidht || img.GetHeight() > _textureMaxHeight)
					{
						float aspectRatio = (float)img.GetWidth() / img.GetHeight();

						int newWidth = _textureMaxWidht;
						int newHeight = (int)(_textureMaxWidht / aspectRatio);

						if(newHeight > _textureMaxHeight)
						{
							newHeight = _textureMaxHeight;
							newWidth = (int)(_textureMaxHeight * aspectRatio);
						}

						img.Resize(newWidth, newHeight, Image.Interpolation.Bilinear);
					}
					//float aspectRatio = (float)img.GetWidth() / img.GetHeight();
					


					var tex = ImageTexture.CreateFromImage(img);
					//GD.Print("WebRtcStream max width: ", _textureMaxWidht, ", max height: ", _textureMaxHeight);
					//GD.Print("WebRtcStreamDispScript: New frame received, width: ", img.GetWidth(), ", height: ", img.GetHeight());

					_videoDisplay.Texture = tex;
				}
				_webRtcClient.NewFrameAvailable = false;
			}
		}

		public void Dispose()
		{
			_webRtcClient?.Dispose();
		}
	}
}
