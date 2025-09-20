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
	[Export] volatile SubBattery[] battery = new SubBattery[3];
	[Export] private Label _vescVoltageLabel = new();
	[Export] private Label _battVoltageLabel = new();
	[Export] private Label _sumCurrentLabel = new();
	[Export] private Label _blackMushroomLabel = new();
	[Export] private Label _hotswapGPIO = new();


	private volatile float _currentVoltageAlt = 0;

	public event Action<int, int, Color>? OnBatteryDataChanged; //enabled batteries (closed hotswaps) (0 if it's in alt mode), percentages (volts from alt mode), color to check for warnings

	public int ConnectedBatts
	{
		get => CountConnectedBatts();
	}

	public override void _EnterTree()
	{
		MqttNode.Singleton.MessageReceivedAsync += BatteryInfoChanged;
		MqttNode.Singleton.MessageReceivedAsync += AltBatteryInfoChanged;
		foreach (var batt in battery)
		{
			batt.NewBatteryInfo += SendToHUD;
			batt.OnBatteryControl += OnBatteryControl;
		}
	}

	public override void _ExitTree()
	{
		MqttNode.Singleton.MessageReceivedAsync -= BatteryInfoChanged;
		MqttNode.Singleton.MessageReceivedAsync -= AltBatteryInfoChanged;
		foreach (var batt in battery)
		{
			batt.NewBatteryInfo -= SendToHUD;
			batt.OnBatteryControl -= OnBatteryControl;
		}
	}

	public override void _Ready()
	{
		for (int i = 0; i < 3; i++)
		{
			battery[i].SetSlotNumber(i+1);
		}
	}

	public async Task BatteryInfoChanged(string subTopic, MqttApplicationMessage? msg)
	{
		if (string.IsNullOrEmpty(LocalSettings.Singleton.Mqtt.TopicBatteryInfo) || subTopic != LocalSettings.Singleton.Mqtt.TopicBatteryInfo)
			return;
		if (msg is null || msg.PayloadSegment.Count == 0)
		{
			EventLogger.LogMessage("BatteryMonitor", EventLogger.LogLevel.Error, "Empty payload");
			return;
		}
		
		MqttClasses.BatteryInfo data;
		
		data = JsonSerializer.Deserialize<MqttClasses.BatteryInfo>(msg.ConvertPayloadToString());

		await (battery[data.Slot - 1].UpdateBattInfoHandler(msg.ConvertPayloadToString()));

		UpdateGeneralPowerInfo(data.HotswapStatus);

		return;
	}


	public Task AltBatteryInfoChanged(string subTopic, MqttApplicationMessage? msg)
	{
		if (string.IsNullOrEmpty(LocalSettings.Singleton.Mqtt.TopicWheelFeedback) || subTopic != LocalSettings.Singleton.Mqtt.TopicWheelFeedback)
			return Task.CompletedTask;
		if (msg is null || msg.PayloadSegment.Count == 0)
		{
			EventLogger.LogMessage("AltBatteryMonitor", EventLogger.LogLevel.Error, "Empty payload");
			return Task.CompletedTask;
		}
		
		var altData = JsonSerializer.Deserialize<MqttClasses.WheelFeedback>(msg.ConvertPayloadToString());
		
		if (!(altData.VescId == Convert.ToInt32(LocalSettings.Singleton.WheelData.FrontLeftDrive.Replace("0x", ""), 16) || 
			  altData.VescId == Convert.ToInt32(LocalSettings.Singleton.WheelData.FrontRightDrive.Replace("0x", ""), 16) || 
			  altData.VescId == Convert.ToInt32(LocalSettings.Singleton.WheelData.BackRightDrive.Replace("0x", ""), 16) || 
			  altData.VescId == Convert.ToInt32(LocalSettings.Singleton.WheelData.BackLeftDrive.Replace("0x", ""), 16)))
			  return Task.CompletedTask;

		_currentVoltageAlt = _currentVoltageAlt * 0.9f + 0.1f * (float)altData.VoltsIn;

		UpdateGeneralPowerInfo();

		if (!LocalSettings.Singleton.Battery.AltMode && CountConnectedBatts() != 0)
		{
			CallDeferred("ShowAltVoltage", false);
			return Task.CompletedTask;
		}
		CallDeferred("ShowAltVoltage", true);

		OnBatteryDataChanged.Invoke(0,(int)(_currentVoltageAlt*10),CheckForWarnings());

		return Task.CompletedTask;
	}

	private void UpdateGeneralPowerInfo(MqttClasses.HotswapStatus newHotswapStatus)
	{
		battery[0].ShowHotswapStatusHandler(newHotswapStatus.HasFlag(MqttClasses.HotswapStatus.Hotswap1));
		battery[1].ShowHotswapStatusHandler(newHotswapStatus.HasFlag(MqttClasses.HotswapStatus.Hotswap2));
		battery[2].ShowHotswapStatusHandler(newHotswapStatus.HasFlag(MqttClasses.HotswapStatus.Hotswap3));
	}

	private void UpdateGeneralPowerInfo()
	{

	}

	public Task SendToHUD()
	{
		if (LocalSettings.Singleton.Battery.AltMode) return Task.CompletedTask;
		if (LocalSettings.Singleton.Battery.AverageAll)
			OnBatteryDataChanged.Invoke(0, CalculateBatteryAverageVoltage(), CheckForWarnings());
		else
			OnBatteryDataChanged.Invoke(CountConnectedBatts(), CalculateBatteryPercentSum(), CheckForWarnings());

		return Task.CompletedTask;
	}

	Color CheckForWarnings()
	{
		float battVoltage = 0;
		if (LocalSettings.Singleton.Battery.AltMode || CountConnectedBatts() == 0)
		{
			battVoltage = _currentVoltageAlt;
		}
		else
		{
			battVoltage = 999;
			foreach (var batt in battery)
			{
				if (batt.myData == null) continue;
				if ((((int)batt.myData.HotswapStatus & (1 << batt.myData.Slot - 1)) != 0) &&
				   battVoltage > batt.myData.Voltage && batt.UpToDate)
				{
					battVoltage = batt.myData.Voltage;
				}

				if (batt.myData.Temperature > LocalSettings.Singleton.Battery.WarningTemperature) return Colors.Red;
			}
		}

		if (battVoltage < LocalSettings.Singleton.Battery.CriticalVoltage) return Colors.Red;
		if (battVoltage < LocalSettings.Singleton.Battery.WarningVoltage) return Colors.Yellow;
		

		return Colors.White;
	}

	Task OnBatteryControl(int slot, MqttClasses.BatterySet set)
	{
		if(CountConnectedBatts() < 2 && set == MqttClasses.BatterySet.Off) return Task.CompletedTask;

		MqttClasses.BatteryControl control = new MqttClasses.BatteryControl()
		{
			Slot = slot,
			Set = set
		};
		OnBatteryControlChanged(control);
		return Task.CompletedTask;
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
			if (batt.myData == null) continue;
			if ((((int)batt.myData.HotswapStatus & (1 << batt.myData.Slot - 1)) != 0) && batt.UpToDate)
				count++;
		}
		return count;
	}

	int CalculateBatteryPercentSum()
	{
		int sum = 0;

		foreach (var batt in battery)
		{
			if (batt.myData == null) continue;
			if ((((int)batt.myData.HotswapStatus & (1 << batt.myData.Slot - 1)) != 0) && batt.UpToDate)
				sum += (int)batt.myData.ChargePercent;
		}

		return sum;
	}

	int CalculateBatteryAverageVoltage()
	{
		int batts = 0;
		int avgVolt = 0;

		foreach (var batt in battery)
		{
			if (batt.myData == null) continue;
			if ((((int)batt.myData.HotswapStatus & (1 << batt.myData.Slot - 1)) != 0) && batt.myData.Voltage != 0)
			{
				batts++;
				avgVolt += (int)(10 * batt.myData.Voltage);
			}
		}

		if (batts == 0) return 0;

		return avgVolt/batts;
	}
}
