# Rover Control Handbook

<!-- Written by [Jakub Wiśniewski](https://www.linkedin.com/in/jakub-wi%C5%9Bniewski-0a1b21273/) -->

This document covers how to implement Rover Control via this App.

# Table of Contents

- [Introduction](#toc-introduction)
- [API](#toc-api)
  - [MQTT Rover Status](#toc-rover-status)
  - [MQTT Rover Control](#toc-rover-control)

# <a id="toc-introduction"></a>Introduction



# <a id="toc-api"></a>API

## <a id="toc-rover-status"></a> MQTT Rover Status
This sends a JSON-serialized message across MQTT with status, defined as such:
```
public class RoverStatus
{
	public CommunicationState CommunicationState { get; set; } = CommunicationState.Closed;
	public bool PadConnected { get; set; } = false;
	public ControlMode ControlMode { get; set; } = ControlMode.EStop;
}
```
With enums defined as such:
```
public enum CommunicationState
{
	Created = 0,
	Opening = 1,
	Opened = 2,
	Closing = 3,
	Closed = 4,
	Faulted = 5
}
public enum ControlMode
{
	EStop = 0,
	Rover = 1,
	Manipulator = 2,
	Autonomy = 3
}
 ```

Topic definition (from settings): `(MqttTopic)/(MqttTopicRoverStatus)`.
> Default path: `RappTORS/RoverStatus`

The example messege looks like this:
`{"CommunicationState":5,"PadConnected":false,"ControlMode":0}`




## <a id="toc-rover-control"></a> MQTT Rover Control
This sends a JSON-serialized message across MQTT to control rover, defined as such:
```
public class RoverControl
{
	public double XVelAxis { get; set; } = 0;
	public double ZRotAxis { get; set; } = 0;
	public long Timestamp { get; set; } = DateTimeOffset.Now.ToUnixTimeMilliseconds();
}
```
Topic definition (from settings): `(MqttTopic)/(MqttTopicRoverControl)`
> Default path: `RappTORS/RoverControl`

The example messege looks like this:
`{"XVelAxis":-0.6493909358978271,"ZRotAxis":-0.7604547739028931,"Timestamp":1688729381666}`

