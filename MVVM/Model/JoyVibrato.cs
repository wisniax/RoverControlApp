using Godot;
using RoverControlApp.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static RoverControlApp.Core.MqttClasses;

namespace RoverControlApp.MVVM.Model;

public class JoyVibrato : IDisposable
{
	private readonly Dictionary<MqttClasses.ControlMode, VibrationSequence[]> PRESET = new()
	{
		{
			MqttClasses.ControlMode.EStop,
			new VibrationSequence[]
			{
				new VibrationSequence(0.1f, 1.0f, 0.0f),
				new VibrationSequence(0.2f, 0.0f, 0.0f),
				new VibrationSequence(0.7f, 1.0f, 1.0f)
			}
		},
		{
			MqttClasses.ControlMode.Rover,
			new VibrationSequence[]
			{
				new VibrationSequence(0.1f, 1.0f, 0.0f),
				new VibrationSequence(0.2f, 0.0f, 0.0f),
				new VibrationSequence(0.2f, 0.0f, 1.0f)
			}
		},
		{
			MqttClasses.ControlMode.Manipulator,
			new VibrationSequence[]
			{
				new VibrationSequence(0.1f, 1.0f, 0.0f),
				new VibrationSequence(0.2f, 0.0f, 0.0f),
				new VibrationSequence(0.2f, 0.0f, 1.0f),
				new VibrationSequence(0.2f, 0.0f, 0.0f),
				new VibrationSequence(0.2f, 0.0f, 1.0f)
			}
		},
		{
			MqttClasses.ControlMode.Autonomy,
			new VibrationSequence[]
			{
				new VibrationSequence(0.1f, 1.0f, 0.0f),
				new VibrationSequence(0.2f, 0.0f, 0.0f),
				new VibrationSequence(0.2f, 0.0f, 1.0f),
				new VibrationSequence(0.2f, 0.0f, 0.0f),
				new VibrationSequence(0.2f, 0.0f, 1.0f),
				new VibrationSequence(0.2f, 0.0f, 0.0f),
				new VibrationSequence(0.2f, 0.0f, 1.0f)
			}
		}
	};

	public struct VibrationSequence
	{
		public float Duration { get; set; }
		public float StrongMotor { get; set; }
		public float WeakMotor { get; set; }

		public VibrationSequence(float duration, float strongMotor, float weakMotor)
		{
			Duration = duration;
			StrongMotor = strongMotor;
			WeakMotor = weakMotor;
		}
	}

	public JoyVibrato()
	{
		ctSource = new CancellationTokenSource();
		ctToken = ctSource.Token;
	}

	public async Task ControlModeChangedSubscriber(MqttClasses.ControlMode newMode)
	{
		if (taskVibrato?.IsCompleted == false)
		{
			ctSource.Cancel();
			try { await taskVibrato; } 
			catch (Exception) { /*its just canceled*/ }
			ctSource = new();
			ctToken = ctSource.Token;
		}

		if(LocalSettings.Singleton.Joystick.VibrateOnModeChange)
			taskVibrato = Task.Run(async () => await Vibrate(newMode), ctToken);
	}

	private Task taskVibrato;
	private CancellationTokenSource ctSource;
	private CancellationToken ctToken;
	

	private async Task Vibrate(MqttClasses.ControlMode controlMode)
	{
		ctToken.ThrowIfCancellationRequested();

		VibrationSequence[] sequence = PRESET[controlMode];
		long offset;

		foreach(var vibration in sequence)
		{
			if(ctToken.IsCancellationRequested)
			{
				foreach(var joyId in Input.GetConnectedJoypads()) 
					Input.StopJoyVibration(joyId);
				ctToken.ThrowIfCancellationRequested();
			}

			offset = DateTimeOffset.Now.ToUnixTimeMilliseconds();

			foreach (var joyId in Input.GetConnectedJoypads())
				Input.StartJoyVibration(joyId, vibration.WeakMotor, vibration.StrongMotor, vibration.Duration);

			await Task.Delay(Math.Max(0,Convert.ToInt32(Convert.ToInt64(vibration.Duration * 1000f) - (DateTimeOffset.Now.ToUnixTimeMilliseconds() - offset))));
		}
	}

	public void Dispose()
	{
		if (taskVibrato?.IsCompleted == false)
		{
			ctSource.Cancel();
		}
		GC.SuppressFinalize(this);
	}
}
