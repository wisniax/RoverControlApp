using Godot;
using System;
using RoverControlApp.Core;
using MQTTnet;
using System.Threading.Tasks;
using System.Text.Json;
using RoverControlApp.MVVM.Model;

namespace RoverControlApp.MVVM.ViewModel;

public partial class BatteryMonitor : Panel
{
	[Export] SubBattery batt1 = new SubBattery(1);
	[Export] SubBattery batt2 = new SubBattery(2);
	[Export] SubBattery batt3 = new SubBattery(3);

	private MqttClasses.BatteryInfo data;

	private MqttClasses.BatteryInfo[] battery = new MqttClasses.BatteryInfo[3];
	private float _currentVoltageAlt = 0;

	public event Func<int, int, Color, Task>? OnBatteryPercentageChanged; //enabled batteries (closed hotswaps) (0 if it's in alt mode), percentages (volts from alt mode), color to check for warnings

	public override void _EnterTree()
	{
		MqttNode.Singleton.MessageReceivedAsync += BatteryInfoChanged;
		MqttNode.Singleton.MessageReceivedAsync += AltBatteryInfoChanged;
	}

	public override void _ExitTree()
	{
		MqttNode.Singleton.MessageReceivedAsync -= BatteryInfoChanged;
		MqttNode.Singleton.MessageReceivedAsync -= AltBatteryInfoChanged;
	}

	//public override void _Process(double delta)
	//{
	//	if (LocalSettings.Singleton.Battery.AltMode)
	//	{
	//		altDisp.SetVisible(true);
	//		this.Size = new Vector2(400, Size.Y);
			
	//		GetNode<HBoxContainer>("BatBoxes").Size = new Vector2(400, Size.Y);
	//		GetNode<Label>("Label").Size = new Vector2(400, GetNode<Label>("Label").Size.Y);
	//	}
	//	else
	//	{
	//		altDisp.SetVisible(false);
	//		this.Size = new Vector2(310, Size.Y);
			
	//		GetNode<HBoxContainer>("BatBoxes").Size = new Vector2(310, Size.Y);
	//		GetNode<Label>("Label").Size = new Vector2(310, GetNode<Label>("Label").Size.Y);
	//	}

	//}

	public Task BatteryInfoChanged(string subTopic, MqttApplicationMessage? msg)
	{
		if (string.IsNullOrEmpty(LocalSettings.Singleton.Mqtt.TopicBatteryInfo) || subTopic != LocalSettings.Singleton.Mqtt.TopicBatteryInfo)
			return Task.CompletedTask;
		if (msg is null || msg.PayloadSegment.Count == 0)
		{
			EventLogger.LogMessage("BatteryMonitor", EventLogger.LogLevel.Error, "Empty payload");
			return Task.CompletedTask;
		}

		data = JsonSerializer.Deserialize<MqttClasses.BatteryInfo>(msg.ConvertPayloadToString());

		switch (data.Slot)
		{
			case 1:
				batt1.UpdateBattInfo(data);
				break;
			case 2:
				batt2.UpdateBattInfo(data);
				break;
			case 3:
				batt3.UpdateBattInfo(data);
				break;
			default:
				EventLogger.LogMessage("BatteryMonitor", EventLogger.LogLevel.Error, "Invalid battery slot. Expected 1, 2 or 3");
				return Task.CompletedTask;
		}

		OnBatteryPercentageChanged.Invoke(CalculateAverageBatteryPercent(), CheckForWarnings());
		
		return Task.CompletedTask;
	}
	public Task AltBatteryInfoChanged(string subTopic, MqttApplicationMessage? msg)
	{
		if (!LocalSettings.Singleton.Battery.AltMode)
			return Task.CompletedTask;
		if (string.IsNullOrEmpty(LocalSettings.Singleton.Mqtt.TopicWheelFeedback) || subTopic != LocalSettings.Singleton.Mqtt.TopicWheelFeedback)
			return Task.CompletedTask;
		if (msg is null || msg.PayloadSegment.Count == 0)
		{
			EventLogger.LogMessage("AltBatteryMonitor", EventLogger.LogLevel.Error, "Empty payload");
			return Task.CompletedTask;
		}
		
		var altData = JsonSerializer.Deserialize<MqttClasses.WheelFeedback>(msg.ConvertPayloadToString());
		
		if (!(altData.VescId == 0x50 || altData.VescId == 0x51 || altData.VescId == 0x52 || altData.VescId == 0x53)) return Task.CompletedTask;

		_currentVoltageAlt = _currentVoltageAlt * 0.9f + 0.1f * (float)altData.VoltsIn;
		GD.Print($"{altData.VescId}:{altData.VoltsIn}");
		CallDeferred("ShowAltVoltage");

		OnBatteryPercentageChanged.Invoke((int)(_currentVoltageAlt*10),CheckForWarningsAlt());

		return Task.CompletedTask;
	}

	void ShowAltVoltage()
	{
		//altDisp.GetNode<Label>("Voltage").SetText($"{_currentVoltageAlt:F2} V");
	}

	Color CheckForWarnings()
	{
		bool warning = false;
		foreach (var batt in battery)
		{
			if(batt == null) continue;
			if (batt.Temperature > LocalSettings.Singleton.Battery.WarningTemperature) return Colors.Red;
			if (batt.Voltage < 6 * LocalSettings.Singleton.Battery.CriticalVoltage) return Colors.Red;
			if (batt.Voltage < 6 * LocalSettings.Singleton.Battery.WarningVoltage) warning = true;
		}

		return warning ? Colors.Yellow : Colors.White;
	}

	Color CheckForWarningsAlt()
	{
		if (_currentVoltageAlt < LocalSettings.Singleton.Battery.CriticalVoltage)
		{
			return Colors.Red;
		}
		else
		{
			return Colors.White;
		}
	}

	void OnBatteryControl(int slot, MqttClasses.BatterySet set)
	{
		int temp = 0;
		foreach (var batt in battery)
		{
			if (batt.HotswapStatus == MqttClasses.HotswapStatus.OnMan ||
			    batt.HotswapStatus == MqttClasses.HotswapStatus.OnAuto)
			{
				temp++;
			}
		}

		if(temp < 2 && set == MqttClasses.BatterySet.Off) return;

		MqttClasses.BatteryControl control = new MqttClasses.BatteryControl()
		{
			Slot = slot,
			Set = set
		};
		OnBatteryControlChanged(control);
	}

	private async Task OnBatteryControlChanged(MqttClasses.BatteryControl arg)
	{
		await MqttNode.Singleton.EnqueueMessageAsync(LocalSettings.Singleton.Mqtt.TopicBatteryControl,
			JsonSerializer.Serialize(arg));
	}

	int CountConnectedBatts()
	{
		int count = 0;
		foreach (var batt in battery)
		{
			if (batt != null && batt.Status != MqttClasses.BatteryStatus.Fault)
			{
				count++;
			}
		}

		return count;
	}

	int CalculateAverageBatteryPercent()
	{
		int sum = 0;
		int count = 0;
		foreach (var batt in battery)
		{
			if (batt != null && batt.Status != MqttClasses.BatteryStatus.Fault)
			{
				sum += (int)batt.ChargePercent;
				count++;
			}
		}

		if(LocalSettings.Singleton.Battery.AverageAll) count = 3;

		return count == 0 ? 0 : sum / count;
	}

	public override void _Ready()
	{
	}
}
