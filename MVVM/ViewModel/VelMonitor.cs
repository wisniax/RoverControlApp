using Godot;
using RoverControlApp.Core;
using RoverControlApp.Core.Settings;

namespace RoverControlApp.MVVM.ViewModel;
public partial class VelMonitor : Panel
{
	[Export] private Label _flLabel;
	[Export] private Label _frLabel;
	[Export] private Label _brLabel;
	[Export] private Label _blLabel;

	public override void _EnterTree()
	{
		UpdateCanIDLabels();
		LocalSettings.Singleton.Connect(LocalSettings.SignalName.PropagatedPropertyChanged, Callable.From<StringName, StringName, Variant, Variant>(OnSettingsPropertyChanged));
	}

	void OnSettingsPropertyChanged(StringName category, StringName name, Variant oldValue, Variant newValue)
	{
		if (category != nameof(WheelData)) return;
		UpdateCanIDLabels();
	}

	private void UpdateCanIDLabels()
	{
		_flLabel.SetText($"D: {LocalSettings.Singleton.WheelData.FrontLeftDrive}\n" +
						 $"R: {LocalSettings.Singleton.WheelData.FrontLeftTurn}");
		_frLabel.SetText($"D: {LocalSettings.Singleton.WheelData.FrontRightDrive}\n" +
						 $"R: {LocalSettings.Singleton.WheelData.FrontRightTurn}");
		_blLabel.SetText($"D: {LocalSettings.Singleton.WheelData.BackLeftDrive}\n" +
						 $"R: {LocalSettings.Singleton.WheelData.BackLeftTurn}");
		_brLabel.SetText($"D: {LocalSettings.Singleton.WheelData.BackRightDrive}\n" +
						 $"R: {LocalSettings.Singleton.WheelData.BackRightTurn}");
	}
}