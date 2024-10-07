using Godot;
using RoverControlApp.Core;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace RoverControlApp.MVVM.Model;

public class JoyVibrato : IDisposable
{
	private readonly Dictionary<MqttClasses.ControlMode, VibrationSequence[]> Presets = new()
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
			MqttClasses.ControlMode.Sampler,
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
				new VibrationSequence(0.2f, 0.0f, 1.0f),
				new VibrationSequence(0.2f, 0.0f, 0.0f),
				new VibrationSequence(0.2f, 0.0f, 1.0f)
			}
		}
	};

	private Task? _taskVibrato;
	private CancellationTokenSource _ctSource;
	private CancellationToken _ctToken;
	private bool _disposedValue = false;

	public JoyVibrato()
	{
		_ctSource = new CancellationTokenSource();
		_ctToken = _ctSource.Token;
	}

	public async Task ControlModeChangedSubscriber(MqttClasses.ControlMode newMode)
	{
		if (_taskVibrato?.IsCompleted == false)
		{
			_ctSource.Cancel();
			try { await _taskVibrato; } 
			catch (Exception) { /*its just canceled*/ }
			_ctSource = new();
			_ctToken = _ctSource.Token;
		}

		if(LocalSettings.Singleton.Joystick.VibrateOnModeChange)
			_taskVibrato = Task.Run(async () => await Vibrate(newMode), _ctToken);
	}

	private async Task Vibrate(MqttClasses.ControlMode controlMode)
	{
		_ctToken.ThrowIfCancellationRequested();

		VibrationSequence[] sequence = Presets[controlMode];
		long offset;

		foreach(var vibration in sequence)
		{
			if(_ctToken.IsCancellationRequested)
			{
				foreach(var joyId in Input.GetConnectedJoypads()) 
					Input.StopJoyVibration(joyId);
				_ctToken.ThrowIfCancellationRequested();
			}

			offset = DateTimeOffset.Now.ToUnixTimeMilliseconds();

			foreach (var joyId in Input.GetConnectedJoypads())
				Input.StartJoyVibration(joyId, vibration.WeakMotor, vibration.StrongMotor, vibration.Duration);

			await Task.Delay(Math.Max(0,Convert.ToInt32(Convert.ToInt64(vibration.Duration * 1000f) - (DateTimeOffset.Now.ToUnixTimeMilliseconds() - offset))));
		}
	}

	protected virtual void Dispose(bool disposing)
	{
		if (_disposedValue)	return;

		if (disposing && (_taskVibrato?.IsCompleted == false))
			_ctSource.Cancel();

		_disposedValue = true;
	}

	public void Dispose()
	{
		Dispose(disposing: true);
		GC.SuppressFinalize(this);
	}

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

}
