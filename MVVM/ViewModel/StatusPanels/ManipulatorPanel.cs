using Godot;
using MQTTnet;
using MQTTnet.Client;
using RoverControlApp.Core;
using RoverControlApp.MVVM.Model;
using System.Text.Json;
using System.Threading.Tasks;

public partial class ManipulatorPanel : StatusPanel
{
	[Export] private Label _statusLabel = null!;
	[Export] private Label _messageLabel = null!;

	public override void _Ready()
	{
		MqttNode.Singleton.MessageReceivedAsync += ManipulatorKinematicsFeedbackReceived;
	}
	public override void _ExitTree()
	{
		MqttNode.Singleton.MessageReceivedAsync -= ManipulatorKinematicsFeedbackReceived;
	}

	private async Task ManipulatorKinematicsFeedbackReceived(string subTopic, MqttApplicationMessage? msg)
	{
		if (string.IsNullOrEmpty(LocalSettings.Singleton.Mqtt.TopicServoStatus) || subTopic != LocalSettings.Singleton.Mqtt.TopicServoStatus)
			return;
		if (msg is null || msg.PayloadSegment.Count == 0)
			return;

		string payload = System.Text.Encoding.UTF8.GetString(msg.PayloadSegment.ToArray());
		var servoStatus = JsonSerializer.Deserialize<MqttClasses.ServoStatus>(payload);

		if (servoStatus is null)
			return;

		Color color;

		switch (servoStatus.Code)
		{
			// WHITE
			case MqttClasses.StatusCode.NoWarning:
				color = Color.Color8(255, 255, 255, 255);
				break;
			// YELLOW
			case MqttClasses.StatusCode.DecelerateForApproachingSingularity:
			case MqttClasses.StatusCode.DecelerateForLeavingSingularity:
			case MqttClasses.StatusCode.DecelerateForCollision:
				color = Color.Color8(255, 255, 0, 255);
				break;
			// ORANGE
			case MqttClasses.StatusCode.JointBound:
				color = Color.Color8(255, 165, 0, 255);
				break;
			// RED
			case MqttClasses.StatusCode.HaltForSingularity:
			case MqttClasses.StatusCode.HaltForCollision:
				color = Color.Color8(255, 0, 0, 255);
				break;
			// BLUE
			case MqttClasses.StatusCode.Invalid:
			default:
				color = Color.Color8(0, 0, 255, 255);
				break;
		}
		CallDeferred("UpdateStatus", $"{servoStatus.Code}", $"{servoStatus.Message}", color);
		await Task.CompletedTask;
	}

	void UpdateStatus(string status, string message, Color color)
	{
		_statusLabel.Text = status;
		_messageLabel.Text = message;
		this.Modulate = color;
	}
}
