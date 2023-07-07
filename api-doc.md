# Rover Control Handbook

<!-- Written by [Jakub Wiśniewski](https://www.linkedin.com/in/jakub-wi%C5%9Bniewski-0a1b21273/) -->

This document covers how to implement Rover Control via this App.

# Table of Contents

- [Introduction](#toc-introduction)
- [API](#toc-api)
  - [MQTT Rover Control](#toc-rover-control)

# <a id="toc-introduction"></a>Introduction




# <a id="toc-api"></a>API

## <a id="toc-rover-control"></a> MQTT Rover Control
This sends a JSON-serialized message across MQTT to control rover, defined as such:
```
public class RoverControl
{
	public float XVelAxis { get; set; } = 0;
	public float ZRotAxis { get; set; } = 0;
	public long Timestamp { get; set; } = DateTimeOffset.Now.ToUnixTimeMilliseconds();
}
```

The example messege looks like this:
`{"XVelAxis":0.9835227,"ZRotAxis":-0.18078424,"Timestamp":1688726220651}`
