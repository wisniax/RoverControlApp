using MQTTnet.Protocol;

namespace RoverControlApp.Core.Settings;

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
            ( mqttSettings.TopicBatteryInfo, MqttQualityOfServiceLevel.ExactlyOnce ),
			( mqttSettings.TopicSamplerFeedback, MqttQualityOfServiceLevel.ExactlyOnce )
		];

}
