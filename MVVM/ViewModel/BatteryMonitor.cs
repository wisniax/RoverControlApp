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
	//[Export] SubBattery batt1 = new (1);
	//[Export] SubBattery batt2 = new (2);
	//[Export] SubBattery batt3 = new (3);

	//[Export] SubBattery batt1;
	//[Export] SubBattery batt2;
	//[Export] SubBattery batt3;

	[Export] SubBattery[] battery = new SubBattery[3];

	private float _currentVoltageAlt = 0;

	public event Func<int, int, Color, Task>? OnBatteryDataChanged; //enabled batteries (closed hotswaps) (0 if it's in alt mode), percentages (volts from alt mode), color to check for warnings

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

	public override void _Ready()
	{
		for (int i = 0; i < 3; i++)
		{
			battery[i].SetSlotNumber(i+1);
		}
	}

	public override void _Process(double delta)
	{
		
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
		
		MqttClasses.BatteryInfo data;
		
		data = JsonSerializer.Deserialize<MqttClasses.BatteryInfo>(msg.ConvertPayloadToString());

		try
		{
			battery[data.Slot - 1].CallDeferred("UpdateBattInfo", msg.ConvertPayloadToString());
		}
		catch (Exception e)
		{
			EventLogger.LogMessage("BatteryMonitor", EventLogger.LogLevel.Error, "Invalid battery slot. Expected 1, 2 or 3");
			throw;
		}

		OnBatteryDataChanged.Invoke(CountConnectedBatts(), CalculateBatteryPercentSum(), CheckForWarnings());
		
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

		OnBatteryDataChanged.Invoke(0,(int)(_currentVoltageAlt*10),CheckForWarningsAlt());

		return Task.CompletedTask;
	}

	Color CheckForWarnings()
	{
		float battVoltage = 0;
		if (LocalSettings.Singleton.Battery.AltMode)
		{
			battVoltage = _currentVoltageAlt;
		}
		else
		{
			battVoltage = battery[0].myData.Voltage;
			foreach (var batt in battery)
			{
				if((batt.myData.HotswapStatus == MqttClasses.HotswapStatus.OnAuto || batt.myData.HotswapStatus == MqttClasses.HotswapStatus.OnMan) &&
				   battVoltage > batt.myData.Voltage)
				{
					battVoltage = batt.myData.Voltage;
				}

				if (batt.myData.Temperature > LocalSettings.Singleton.Battery.WarningTemperature) return Colors.Red;
			}
		}

		if (battVoltage < 6 * LocalSettings.Singleton.Battery.CriticalVoltage) return Colors.Red;
		if (battVoltage < 6 * LocalSettings.Singleton.Battery.WarningVoltage) return Colors.Yellow;
		

		return Colors.White;
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
		if(CountConnectedBatts() < 2 && set == MqttClasses.BatterySet.Off) return;

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

		//if (batt1.myData.HotswapStatus == MqttClasses.HotswapStatus.OnAuto ||
		//    batt1.myData.HotswapStatus == MqttClasses.HotswapStatus.OnMan)
		//	count++;
		//if (batt2.myData.HotswapStatus == MqttClasses.HotswapStatus.OnAuto ||
		//    batt2.myData.HotswapStatus == MqttClasses.HotswapStatus.OnMan)
		//	count++;
		//if (batt3.myData.HotswapStatus == MqttClasses.HotswapStatus.OnAuto ||
		//    batt3.myData.HotswapStatus == MqttClasses.HotswapStatus.OnMan)
		//	count++;

		foreach (var batt in battery)
		{
			if (batt.myData.HotswapStatus == MqttClasses.HotswapStatus.OnAuto ||
				batt.myData.HotswapStatus == MqttClasses.HotswapStatus.OnMan)
				count++;
		}

		return count;
	}

	int CalculateBatteryPercentSum()
	{
		int sum = 0;

		//if (batt1.myData.HotswapStatus == MqttClasses.HotswapStatus.OnAuto ||
		//    batt1.myData.HotswapStatus == MqttClasses.HotswapStatus.OnMan)
		//	sum += (int)batt1.myData.ChargePercent;
		//if (batt2.myData.HotswapStatus == MqttClasses.HotswapStatus.OnAuto ||
		//    batt2.myData.HotswapStatus == MqttClasses.HotswapStatus.OnMan)
		//	sum += (int)batt2.myData.ChargePercent;
		//if (batt3.myData.HotswapStatus == MqttClasses.HotswapStatus.OnAuto ||
		//    batt3.myData.HotswapStatus == MqttClasses.HotswapStatus.OnMan)
		//	sum += (int)batt3.myData.ChargePercent;

		foreach (var batt in battery)
		{
			if (batt.myData.HotswapStatus == MqttClasses.HotswapStatus.OnAuto ||
				batt.myData.HotswapStatus == MqttClasses.HotswapStatus.OnMan)
				sum += (int)batt.myData.ChargePercent;
		}

		return sum;
	}
}
