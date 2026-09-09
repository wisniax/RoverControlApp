using System.Threading.Tasks;

using Godot;

using static RoverControlApp.Core.MqttClasses;

namespace RoverControlApp.MVVM.Model;

public partial class JoyLedFeedback : Node
{
	// RGB table (0–255). These colors can also be changed in the Inspector.
	[Export] public Color EStopColor { get; set; } = Color.Color8(255,   0,   0);
	[Export] public Color DriveColor { get; set; } = Color.Color8(  0, 255,   0);
	[Export] public Color ManipulatorColor { get; set; } = Color.Color8(255, 255,   0);
	[Export] public Color SamplerColor { get; set; } = Color.Color8(255,   0, 255);
	[Export] public Color AutonomyColor { get; set; } = Color.Color8(  0,   0, 255);
	[Export(PropertyHint.Range, "0.05,5,0.05")]
	public double BlinkIntervalSeconds { get; set; } = 0.5;
	[Export(PropertyHint.Range, "0.05,5,0.05")]
	public double AutonomyModeDurationSeconds { get; set; } = 0.8;
	[Export(PropertyHint.Range, "0.05,5,0.05")]
	public double AutonomyColorDurationSeconds { get; set; } = 0.2;

	private const ControlModeFlags AutonomyMask = ControlModeFlags.DriveAutonomy |
		ControlModeFlags.RoboticArmAutonomy | ControlModeFlags.DeepSamplerAutonomy |
		ControlModeFlags.SurfaceSamplerAutonomy;

	private readonly Timer _blinkTimer = new();
	private readonly Timer _autonomyTimer = new() { OneShot = true };
	private bool _lightOn = true;
	private bool _showAutonomyColor;

	public override void _Ready()
	{
		AddChild(_blinkTimer);
		_blinkTimer.Timeout += OnBlinkTimeout;
		AddChild(_autonomyTimer);
		_autonomyTimer.Timeout += OnAutonomyTimeout;
		PressedKeys.Singleton.OnControlModeChanged += OnModeChanged;
		Input.JoyConnectionChanged += OnJoyConnectionChanged;
		RefreshLights();
	}

	public override void _ExitTree()
	{
		PressedKeys.Singleton.OnControlModeChanged -= OnModeChanged;
		Input.JoyConnectionChanged -= OnJoyConnectionChanged;
		_blinkTimer.Stop();
		_blinkTimer.Timeout -= OnBlinkTimeout;
		_autonomyTimer.Stop();
		_autonomyTimer.Timeout -= OnAutonomyTimeout;
		foreach (int device in Input.GetConnectedJoypads())
			if (device is 0 or 1 && Input.HasJoyLight(device))
				Input.SetJoyLight(device, Colors.Black);
	}

	private Task OnModeChanged(ControlModeFlags mode)
	{
		// Start each new selection with its mode color for the full duration.
		_autonomyTimer.Stop();
		_showAutonomyColor = false;
		RefreshLights();
		return Task.CompletedTask;
	}

	private void OnJoyConnectionChanged(long device, bool connected) => RefreshLights();

	private void OnBlinkTimeout()
	{
		_lightOn = !_lightOn;
		RefreshLights();
	}

	private void OnAutonomyTimeout()
	{
		_showAutonomyColor = !_showAutonomyColor;
		RefreshLights();
	}

	private void RefreshLights()
	{
		var input = PressedKeys.Singleton;
		// The effective rover E-Stop (including startup) overrides both seats.
		bool roverEstop = input.GetCurrentControlMode().HasFlag(ControlModeFlags.EStop);
		bool blinking = false;
		bool autonomy = false;
		foreach (int device in Input.GetConnectedJoypads())
		{
			if (device is not (0 or 1) || !Input.HasJoyLight(device)) continue;

			var mode = device == 0 ? input.MasterControlMode : input.SlaveControlMode;
			bool estop = roverEstop || mode.HasFlag(ControlModeFlags.EStop);
			blinking |= estop;
			autonomy |= !estop && (mode & AutonomyMask) != 0;
			// Avoid pure black, which shows as white on the reported controller.
			// Encode the dim color as sRGB so Godot's linear conversion preserves it.
			var dimColor = EStopColor.Darkened(0.995f).LinearToSrgb();
			Input.SetJoyLight(device, estop ? (_lightOn ? EStopColor : dimColor) : GetModeColor(mode));
		}

		if (blinking)
		{
			if (_blinkTimer.IsStopped()) _blinkTimer.Start(BlinkIntervalSeconds);
		}
		else
		{
			_blinkTimer.Stop();
			_lightOn = true;
		}

		if (autonomy)
		{
			if (_autonomyTimer.IsStopped())
				_autonomyTimer.Start(_showAutonomyColor ? AutonomyColorDurationSeconds : AutonomyModeDurationSeconds);
		}
		else
		{
			_autonomyTimer.Stop();
			_showAutonomyColor = false;
		}
	}

	private Color GetModeColor(ControlModeFlags mode)
	{
		if (_showAutonomyColor && (mode & AutonomyMask) != 0) return AutonomyColor;
		if (mode.HasFlag(ControlModeFlags.Drive)) return DriveColor;
		if (mode.HasFlag(ControlModeFlags.RoboticArm)) return ManipulatorColor;
		if ((mode & (ControlModeFlags.DeepSampler | ControlModeFlags.SurfaceSampler)) != 0)
			return SamplerColor;
		// Autonomy normally replaces the corresponding manual mode flag.
		if (mode.HasFlag(ControlModeFlags.DriveAutonomy)) return DriveColor;
		if (mode.HasFlag(ControlModeFlags.RoboticArmAutonomy)) return ManipulatorColor;
		if ((mode & (ControlModeFlags.DeepSamplerAutonomy | ControlModeFlags.SurfaceSamplerAutonomy)) != 0)
			return SamplerColor;
		return Colors.Black;
	}
}
