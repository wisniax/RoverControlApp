using Godot;
using MQTTnet;

namespace RoverControlApp.MVVM.Model;

public partial class MqttNodeMessage : RefCounted
{
	public MqttNodeMessage(MqttApplicationMessage message)
	{
		Message = message;
	}

	public MqttApplicationMessage Message { get; }
}
