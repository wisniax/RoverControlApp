using Godot;
using RoverControlApp.Core.JSONConverter;
using System;
using System.Text.Json.Serialization;
//using Microsoft.MixedReality.WebRTC;

namespace RoverControlApp.Core.Settings;

[JsonConverter(typeof(WebRTCStreamConverter))]
public partial class WebRTCStream : SettingBase, ICloneable
{
	public WebRTCStream()
	{
		_iceServer = "stun:stun.l.google.com:19302";
		_signalingServer = "192.168.1.37:8089";
		_maxBitrate = 2000;
		_preferedVideoCodec = "H264";
	}

	public WebRTCStream(string iceServers, string signalingServer, int maxBitrate, string preferedVideoCodec)
	{
		_iceServer = iceServers;
		_signalingServer = signalingServer;
		_maxBitrate = maxBitrate;
		_preferedVideoCodec = preferedVideoCodec;
	}

	public object Clone()
	{
		return new WebRTCStream()
		{
			IceServer = IceServer,
			SignalingServer = SignalingServer,
			MaxBitrate = MaxBitrate,
			PreferedVideoCodec = PreferedVideoCodec
		};
	}

	[SettingsManagerVisible(cellMode: TreeItem.TreeCellMode.String, customName: "ICE Server")]
	public string IceServer
	{
		get => _iceServer;
		set => EmitSignal_SettingChanged(ref _iceServer, value);
	}

	[SettingsManagerVisible(cellMode: TreeItem.TreeCellMode.String)]
	public string SignalingServer
	{
		get => _signalingServer;
		set => EmitSignal_SettingChanged(ref _signalingServer, value);
	}

	[SettingsManagerVisible(cellMode: TreeItem.TreeCellMode.Range, formatData: "100;5000;100;f;i", customName: "Max Bitrate (kbps)")]
	public int MaxBitrate
	{
		get => _maxBitrate;
		set => EmitSignal_SettingChanged(ref _maxBitrate, value);
	}

	[SettingsManagerVisible(cellMode: TreeItem.TreeCellMode.String, customName: "Codec (Must be one of: H264;VP8;VP9)")]
	public string PreferedVideoCodec
	{
		get => _preferedVideoCodec;
		set {
			if (value == "H264" || value == "VP8" || value == "VP9")
			{
				// Only emit signal if the value is valid
				EmitSignal_SettingChanged(ref _preferedVideoCodec, value);
				return;
			}
			EventLogger.LogMessage("Codec settings WebRTC", EventLogger.LogLevel.Error, "No such codec available " + value);
			return;
		}
	}

	private string _iceServer;
	private string _signalingServer;
	private int _maxBitrate;
	private string _preferedVideoCodec;
}
