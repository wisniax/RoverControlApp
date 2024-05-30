using MQTTnet.Protocol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoverControlApp.MVVM.Model.Settings 
{
	public static class MqttSubscribedExtension
	{
		public static (string, MqttQualityOfServiceLevel)[] GetAllTopicsToSubscribe(this Mqtt mqttSettings)
			=>
			[
				( mqttSettings.TopicRoverStatus, MqttQualityOfServiceLevel.ExactlyOnce ),
				( mqttSettings.TopicMissionStatus, MqttQualityOfServiceLevel.ExactlyOnce ),
				( mqttSettings.TopicKmlListOfActiveObj, MqttQualityOfServiceLevel.ExactlyOnce ),
				( mqttSettings.TopicRoverFeedback, MqttQualityOfServiceLevel.ExactlyOnce ),

				( mqttSettings.TopicWheelFeedback, MqttQualityOfServiceLevel.AtMostOnce ),
				( mqttSettings.TopicZedImuData, MqttQualityOfServiceLevel.AtMostOnce ),
				( mqttSettings.TopicEStopStatus, MqttQualityOfServiceLevel.AtMostOnce ),
			];
		
	}
}
