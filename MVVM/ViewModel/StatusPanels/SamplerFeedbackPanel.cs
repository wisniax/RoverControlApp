using Godot;
using MQTTnet;
using RoverControlApp.Core;
using RoverControlApp.MVVM.Model;
using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;


public partial class SamplerFeedbackPanel : Control
{

	[Export]
	Label _scale1 = null!;

	[Export]
	Label _scale2 = null!;

	[Export]
	Label _save1 = null!;

	[Export]
	Label _save2 = null!;

	[Export]
	Button _button1 = null!;

	[Export]
	Button _button2 = null!;

	public override void _EnterTree()
	{
		MqttNode.Singleton.MessageReceivedAsync += OnWeightChanged;
	}

	public override void _ExitTree()
	{
		MqttNode.Singleton.MessageReceivedAsync -= OnWeightChanged;
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

            if (data != null)
            {
                CallDeferred(nameof(UpdateSamplerFeedbackInfo), data.weight1, data.weight2);
            }
			
			return Task.CompletedTask;
		}
		catch (Exception e)
		{
			EventLogger.LogMessage("SamplerFeedbackPanel", EventLogger.LogLevel.Error, $"{e.Message}");
			return Task.CompletedTask;
		}

	}

	private void UpdateSamplerFeedbackInfo(double w1, double w2)
    {
        _scale1.Text = w1.ToString("0.000");
        _scale2.Text = w2.ToString("0.000");
	}

	private void saveScale1() 
	{
		_save1.Text = _scale1.Text;
	}

	private void saveScale2() 
	{
		_save2.Text = _scale2.Text;
	}
}
