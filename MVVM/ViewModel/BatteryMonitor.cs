using Godot;
using MQTTnet;
using RoverControlApp.Core;
using RoverControlApp.MVVM.Model;
using System;
using System.Text.Json;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace RoverControlApp.MVVM.ViewModel;

public partial class BatteryMonitor : Panel
{
	[Export] volatile SubBattery[] battery = new SubBattery[3];
	[Export] private Label _vescVoltageLabel = new();
	[Export] private Label _battVoltageLabel = new();
	[Export] private Label _sumCurrentLabel = new();
	[Export] private Label _blackMushroomLabel = new();
	[Export] private Label _hotswapGPIO = new();

	[Export] private Timer _vescUpdateTimer = new Timer();


	private volatile float _currentVoltageAlt = 0;

	public event Action<int, float, Color>? OnBatteryDataChanged; //enabled batteries (closed hotswaps) (0 if it's in alt mode), percentages (volts from alt mode), color to check for warnings
	public event Action<MqttClasses.MushroomStatus>? SetMushroomState;

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

		CallDeferred("ResetVescTimer");

		_currentVoltageAlt = _currentVoltageAlt * 0.9f + 0.1f * (float)altData.VoltsIn;

		UpdateGeneralPowerInfo();

		SendToHUD();
		return Task.CompletedTask;
	}

	private void ResetVescTimer()
	{
		_vescUpdateTimer.SetWaitTime(LocalSettings.Singleton.Battery.ExpectedMessageInterval);
		_vescUpdateTimer.Start();
	}

	private void NoVescDataHandler()
	{
		_currentVoltageAlt = 0;
		_vescVoltageLabel.SetText("VESC Voltage:\nNoData");
		SendToHUD();
	}

	private void UpdateGeneralPowerInfo(MqttClasses.HotswapStatus newHotswapStatus)
	{
		battery[0].ShowHotswapStatusHandler(newHotswapStatus.HasFlag(MqttClasses.HotswapStatus.Hotswap1));
		battery[1].ShowHotswapStatusHandler(newHotswapStatus.HasFlag(MqttClasses.HotswapStatus.Hotswap2));
		battery[2].ShowHotswapStatusHandler(newHotswapStatus.HasFlag(MqttClasses.HotswapStatus.Hotswap3));
		CallDeferred("ShowInQuickData", 
			CalculateBatteryAverageVoltage(),
			CalculateBatterySumCurrent(),
			newHotswapStatus.HasFlag(MqttClasses.HotswapStatus.BlackMushroom),
			(int)(newHotswapStatus & (MqttClasses.HotswapStatus.GPIO1 | MqttClasses.HotswapStatus.GPIO2 |
									  MqttClasses.HotswapStatus.GPIO3 | MqttClasses.HotswapStatus.GPIO4)) >> 4
			);
		SetMushroomState?.Invoke((newHotswapStatus.HasFlag(MqttClasses.HotswapStatus.BlackMushroom)? MqttClasses.MushroomStatus.Molded : MqttClasses.MushroomStatus.Unmolded));
	}

	private void UpdateGeneralPowerInfo()
	{
		CallDeferred("ShowInQuickData");
	}

	public void ClearQuickDataHandler()
	{
		if (CountConnectedBatts() == 0)
		{
			battery[0].ShowHotswapStatusHandler(null);
			battery[1].ShowHotswapStatusHandler(null);
			battery[2].ShowHotswapStatusHandler(null);
			CallDeferred("ClearQuickData");
			SetMushroomState?.Invoke(MqttClasses.MushroomStatus.NotAvailable);
		}
	}	

	void ClearQuickData()
	{
		_battVoltageLabel.SetText("Batt Voltage:\nNoData");
		_sumCurrentLabel.SetText("Sum Current:\nNoData");
		_blackMushroomLabel.SetText("Black Mushroom:\nNoData");
		_hotswapGPIO.SetText("Hotswap GPIO:\nNoData");
	}

	void ShowInQuickData(float battVoltage, float sumCurrent, bool blackMushroom, int GPIO)
	{
		_battVoltageLabel.SetText($"Batt Voltage:\n{battVoltage:F1} V");
		_sumCurrentLabel.SetText($"Sum Current:\n{((sumCurrent > 0) ? "+" : "")}{sumCurrent:F1} A");
		_blackMushroomLabel.SetText($"Black Mushroom:\n{(blackMushroom ? "Pressed" : "Released")}");
		_hotswapGPIO.SetText($"Hotswap GPIO:\n{Convert.ToString(GPIO, 2).PadLeft(4, '0')}");
	}

	void ShowInQuickData()
	{
		_vescVoltageLabel.SetText($"VESC Voltage:\n{_currentVoltageAlt:0.0} V");
	}

	public Task SendToHUD()
	{
		if (LocalSettings.Singleton.Battery.AltMode || CountConnectedBatts() == 0)
		{
			OnBatteryDataChanged.Invoke(0, _currentVoltageAlt, CheckForWarnings());
			return Task.CompletedTask;
		}

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
				if (batt.myData == null || !batt.UpToDate) continue;
				if (batt.IsHotswapClosed && battVoltage > batt.myData.Voltage)
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
			if (batt.myData == null || !batt.UpToDate) continue;
			if (batt.IsHotswapClosed)
				count++;
		}
		return count;
	}

	int CalculateBatteryPercentSum()
	{
		int sum = 0;

		foreach (var batt in battery)
		{
			if (batt.myData == null || !batt.UpToDate) continue;
			if (batt.IsHotswapClosed)
				sum += (int)batt.myData.ChargePercent;
		}

		return sum;
	}

	float CalculateBatteryAverageVoltage()
	{
		int batts = 0;
		float avgVolt = 0;

		foreach (var batt in battery)
		{
			if (batt.myData == null || !batt.UpToDate) continue;
			if (batt.IsHotswapClosed)
			{
				batts++;
				avgVolt += batt.myData.Voltage;
			}
		}

		if (batts == 0) return 0;

		return avgVolt/batts;
	}

	float CalculateBatterySumCurrent()
	{
		float sumCurrent = 0;
		foreach (var batt in battery)
		{
			if (batt.myData == null || !batt.UpToDate) continue;
			if (batt.IsHotswapClosed)
				sumCurrent += batt.myData.Current;
		}
		return sumCurrent;
	}
}
