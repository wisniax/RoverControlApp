using Godot;
using MQTTnet;

namespace RoverControlApp.MVVM.Model;

public partial class MqttNodeMessage(MqttApplicationMessage message) : RefCounted
{
	public MqttApplicationMessage Message { get; } = message;
}
