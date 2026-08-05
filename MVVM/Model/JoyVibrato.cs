using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Godot;

using RoverControlApp.Core;

namespace RoverControlApp.MVVM.Model;

public class JoyVibrato : IDisposable
{
	private readonly Dictionary<MqttClasses.ControlModeFlags, VibrationSequence[]> Presets = new()
	{
		{
			MqttClasses.ControlModeFlags.EStop,
			new VibrationSequence[]
			{
				new VibrationSequence(0.1f, 1.0f, 0.0f),
				new VibrationSequence(0.2f, 0.0f, 0.0f),
				new VibrationSequence(0.7f, 1.0f, 1.0f)
			}
		},
		{
			MqttClasses.ControlModeFlags.Drive,
			new VibrationSequence[]
			{
				new VibrationSequence(0.1f, 1.0f, 0.0f),
				new VibrationSequence(0.2f, 0.0f, 0.0f),
				new VibrationSequence(0.2f, 0.0f, 1.0f)
			}
		},
		{
			MqttClasses.ControlModeFlags.RoboticArm,
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
			MqttClasses.ControlModeFlags.DeepSampler,
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
			MqttClasses.ControlModeFlags.SurfaceSampler,
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
	};

	private Task? _taskVibrato;
	private CancellationTokenSource _ctSource;
	private CancellationToken _ctToken;
	private bool _disposedValue = false;

	private bool _isMasterVibrato;

	public JoyVibrato(bool master)
	{
		_ctSource = new CancellationTokenSource();
		_ctToken = _ctSource.Token;
		_isMasterVibrato = master;
	}

	public async Task ControlModeChangedSubscriber(MqttClasses.ControlModeFlags newMode)
	{
		if (_taskVibrato?.IsCompleted == false)
		{
			_ctSource.Cancel();
			try { await _taskVibrato; }
			catch (Exception) { /*its just canceled*/ }
			_ctSource = new();
			_ctToken = _ctSource.Token;
		}

		if (newMode.HasFlag(MqttClasses.ControlModeFlags.EStop) &&
			LocalSettings.Singleton.General.NoInputSecondsToEstop > 0 &&
			!LocalSettings.Singleton.Joystick.VibrateOnAutoEstop &&
			PressedKeys.Singleton.TimeToAutoEStopMsec <= 0)
		{
			// no vibrato on Auto E-Stop
			return;
		}
		else
		{
			_taskVibrato = Task.Run(async () => await Vibrate(newMode), _ctToken);
		}
	}

	private async Task Vibrate(MqttClasses.ControlModeFlags controlMode)
	{
		await Task.Delay(300, _ctToken);

		_ctToken.ThrowIfCancellationRequested();

		VibrationSequence[] sequence = Presets[controlMode];
		long offset;

		int joyId = _isMasterVibrato ? 0 : 1;

		foreach (var vibration in sequence)
		{
			if (_ctToken.IsCancellationRequested)
			{
				Input.StopJoyVibration(joyId);
				_ctToken.ThrowIfCancellationRequested();
			}

			offset = DateTimeOffset.Now.ToUnixTimeMilliseconds();

			Input.StartJoyVibration(joyId, vibration.WeakMotor, vibration.StrongMotor, vibration.Duration);

			await Task.Delay(Math.Max(0, Convert.ToInt32(Convert.ToInt64(vibration.Duration * 1000f) - (DateTimeOffset.Now.ToUnixTimeMilliseconds() - offset))));
		}
	}
	public async Task VibrateMaster()
	{
		await Task.Delay(300, _ctToken);

		_ctToken.ThrowIfCancellationRequested();

		VibrationSequence[] sequence =
		[
			new VibrationSequence(0.5f, 0.0f, 0.0f),
			new VibrationSequence(0.5f, 0.0f, 1.0f),
		];
		long offset;

		int joyId = 0;

		foreach (var vibration in sequence)
		{
			if (_ctToken.IsCancellationRequested)
			{
				Input.StopJoyVibration(joyId);
				_ctToken.ThrowIfCancellationRequested();
			}

			offset = DateTimeOffset.Now.ToUnixTimeMilliseconds();

			Input.StartJoyVibration(joyId, vibration.WeakMotor, vibration.StrongMotor, vibration.Duration);

			await Task.Delay(Math.Max(0, Convert.ToInt32(Convert.ToInt64(vibration.Duration * 1000f) - (DateTimeOffset.Now.ToUnixTimeMilliseconds() - offset))));
		}
	}

	public async Task VibrateSlave()
	{
		await Task.Delay(300, _ctToken);

		_ctToken.ThrowIfCancellationRequested();

		VibrationSequence[] sequence =
		[
			new VibrationSequence(0.5f, 0.0f, 0.0f),
			new VibrationSequence(0.5f, 0.0f, 1.0f),
			new VibrationSequence(0.2f, 0.0f, 0.0f),
			new VibrationSequence(0.5f, 0.0f, 1.0f),
		];
		long offset;

		int joyId = 1;

		foreach (var vibration in sequence)
		{
			if (_ctToken.IsCancellationRequested)
			{
				Input.StopJoyVibration(joyId);
				_ctToken.ThrowIfCancellationRequested();
			}

			offset = DateTimeOffset.Now.ToUnixTimeMilliseconds();

			Input.StartJoyVibration(joyId, vibration.WeakMotor, vibration.StrongMotor, vibration.Duration);

			await Task.Delay(Math.Max(0, Convert.ToInt32(Convert.ToInt64(vibration.Duration * 1000f) - (DateTimeOffset.Now.ToUnixTimeMilliseconds() - offset))));
		}
	}

	protected virtual void Dispose(bool disposing)
	{
		if (_disposedValue) return;

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
