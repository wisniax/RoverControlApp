using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

using Godot;
using MQTTnet;

using RoverControlApp.Core;
using RoverControlApp.MVVM.Model;

public partial class SamplerFeedbackPanel : StatusPanel
{
	[Export] private Label _scale1 = null!;
	[Export] private Label _scale2 = null!;
	[Export] private Label _platform = null!;
	[Export] private Label _drill = null!;
	[Export] private Label _container = null!;
	[Export] private Label _vacuum = null!;
	[Export] private Label _drillRot = null!;
	[Export] private Label _brush = null!;
	[Export] private Label _save1 = null!;
	[Export] private Label _save2 = null!;

	public override void _EnterTree()
	{
		base._EnterTree();
		MqttNode.Singleton.MessageReceivedAsync += OnWeightChanged;
		MqttNode.Singleton.MessageReceivedAsync += OnSamplerInfo;
	}

	public override void _ExitTree()
	{
		MqttNode.Singleton.MessageReceivedAsync -= OnWeightChanged;
		MqttNode.Singleton.MessageReceivedAsync -= OnSamplerInfo;
	}

	private Task OnWeightChanged(string subTopic, MqttApplicationMessage? msg)
	{
		if (string.IsNullOrEmpty(LocalSettings.Singleton.Mqtt.TopicRotaryFeedback) || subTopic != LocalSettings.Singleton.Mqtt.TopicRotaryFeedback)
			return Task.CompletedTask;

		if (msg is null || msg.PayloadSegment.Count == 0)
		{
			EventLogger.LogMessage("SamplerFeedbackPanel", EventLogger.LogLevel.Error, "Empty payload");
			return Task.CompletedTask;
		}

		try
		{
			var data = JsonSerializer.Deserialize<MqttClasses.RotaryFeedback>(msg.ConvertPayloadToString());
			if (data is null)
				throw new InvalidDataException("Invalid RotaryFeedback payload.");
			Callable.From(() => UpdateRotaryFeedbackInfo(data.weight1, data.weight2)).CallDeferred();
		}
		catch (Exception e)
		{
			EventLogger.LogMessage("SamplerFeedbackPanel", EventLogger.LogLevel.Error, $"Something is wrong with json/deserialization: {e.Message}");
		}

		return Task.CompletedTask;
	}

	private Task OnSamplerInfo(string subTopic, MqttApplicationMessage? msg)
	{
		if (string.IsNullOrEmpty(LocalSettings.Singleton.Mqtt.TopicSamplerFeedback) || subTopic != LocalSettings.Singleton.Mqtt.TopicSamplerFeedback)
			return Task.CompletedTask;

		if (msg is null || msg.PayloadSegment.Count == 0)
		{
			EventLogger.LogMessage("SamplerFeedbackPanel", EventLogger.LogLevel.Error, "Empty payload");
			return Task.CompletedTask;
		}

		try
		{
			var data = JsonSerializer.Deserialize<MqttClasses.SamplerFeedback>(msg.ConvertPayloadToString());
			if (data is null)
				throw new InvalidDataException("Invalid SamplerFeedback payload.");
			Callable.From(() => UpdateSamplerFeedbackInfo(data)).CallDeferred();
		}
		catch (Exception e)
		{
			EventLogger.LogMessage("SamplerFeedbackPanel", EventLogger.LogLevel.Error, $"Something is wrong with json/deserialization: {e.Message}");
		}

		return Task.CompletedTask;
	}

	private void UpdateRotaryFeedbackInfo(double w1, double w2)
	{
		_scale1.Text = w1.ToString("0.000");
		_scale2.Text = w2.ToString("0.000");
	}

	private void UpdateSamplerFeedbackInfo(MqttClasses.SamplerFeedback data)
	{
		_platform.Text = data.platform_pos.ToString("0.000") + " m";
		_drill.Text = data.drill_pos.ToString("0.000") + " m";
		_container.Text = data.container_pos.ToString("0.000") + " m";
		_vacuum.Text = data.vacuum_suction_vel.ToString("0.0") + " rpm";
		_drillRot.Text = data.drill_rot_vel.ToString("0.0") + " rpm";
		_brush.Text = data.brush_rot_vel.ToString("0.0") + " rpm";
	}

	public void SaveScale1()
	{
		_save1.Text = _scale1.Text;
	}

	public void SaveScale2()
	{
		_save2.Text = _scale2.Text;
	}
}
