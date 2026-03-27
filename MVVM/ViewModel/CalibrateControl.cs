using Godot;
using RoverControlApp.Core;
using RoverControlApp.MVVM.Model;
using System;
using System.Threading.Tasks;

namespace RoverControlApp.MVVM.ViewModel;
public partial class CalibrateControl : Panel
{
	private enum HookAction { Enter, Exit }

	[ExportGroup("Axis")]
	[Export] private Sprite2D[] AxisModels = new Sprite2D[4];
	[Export] private Button[] AxisButtons = new Button[4];
	[Export] private OptionButton AxisOptions = new OptionButton();

	[ExportGroup("Knobs")]
	[Export] public Knob OffsetKnob = new Knob();
	[Export] public Knob VelocityKnob = new Knob();

	[ExportGroup("Actions Button")]
	[Export] private Button OffsetButton = new Button();
	[Export] private Button VelocityButton = new Button();
	[Export] private Button ConfirmButton = new Button();
	[Export] private Button CancelButton = new Button();
	[Export] private Button StopButton = new Button();
	[Export] private Button ReturnToOriginButton = new Button();

	[ExportGroup("Addons")]
	[Export] private Panel PanelCover = new Panel();
	[Export] private PanelContainer LedLamp = new PanelContainer();

	private Action?[] _AxisBtnHandlers = Array.Empty<Action?>();

	#region Godot.Override

	public override void _EnterTree()
	{
		// Handlers for Wheel Axis changes
		AxisOptions.ItemSelected += index => ChooseAxis(index);
		HookButtons(AxisButtons, ref _AxisBtnHandlers, HookAction.Enter, ChooseAxis);

		// Handlers for LineEdit and Sliders
		OffsetKnob.ValueChanged += CalibrateController.SetOffsetValue;
		VelocityKnob.ValueChanged += CalibrateController.SetVelocityValue;

		// Handlers for functional buttons
		OffsetButton.Pressed += OffsetClicked;
		VelocityButton.ButtonDown += VelocityDown;
		VelocityButton.ButtonUp += VelocityUp;
		ConfirmButton.Pressed += ConfirmClicked;
		CancelButton.Pressed += CancelClicked;
		StopButton.Pressed += StopClicked;
		ReturnToOriginButton.Pressed += ReturnToOriginClicked;

		CalibrateController.VelocityChanged += ManageVelocityChange;
		CalibrateController.OffsetSend += ManageOffsetSend;
		CalibrateController.CalibrateAxisValuesUpdated += OnCalibrateAxisValuesChanged;
		CalibrateController.OnPadSteeringChange += OnPadConnectionChanged;

		Connect("visibility_changed", new Callable(this, nameof(OnVisibilityChanged)));
	}

	public override void _Ready()
	{
		UpdateChooseAxis();

		CalibrateController.SetCalibateAxisValues(
			new MqttClasses.CalibrateAxisValues()
			{
				OffsetValue = Convert.ToSingle(OffsetKnob.Value),
				VelocityValue = Convert.ToSingle(VelocityKnob.Value),
				CalibrateEnabled = PanelCover.Visible
			}
		);

		VelocityKnob.UpdateRange(
				LocalSettings.Singleton.Calibration.CalibrationMotor.MinSpeed,
				LocalSettings.Singleton.Calibration.CalibrationMotor.MaxSpeed
		);

		LocalSettings.Singleton.Connect(LocalSettings.SignalName.PropagatedSubcategoryChanged,
			Callable.From<StringName, StringName, Variant, Variant>(OnSettingsPropertyChanged));
	}

	public override void _ExitTree()
	{
		// Handlers for Wheel Axis changes
		AxisOptions.ItemSelected -= index => ChooseAxis(index);
		HookButtons(AxisButtons, ref _AxisBtnHandlers, HookAction.Exit, ChooseAxis);

		// Handlers for LineEdit and Sliders
		OffsetKnob.ValueChanged -= CalibrateController.SetOffsetValue;
		VelocityKnob.ValueChanged -= CalibrateController.SetVelocityValue;

		// Handlers for functional buttons
		OffsetButton.Pressed -= OffsetClicked;
		VelocityButton.ButtonDown -= VelocityDown;
		VelocityButton.ButtonUp -= VelocityUp;
		ConfirmButton.Pressed -= ConfirmClicked;
		CancelButton.Pressed -= CancelClicked;
		StopButton.Pressed -= StopClicked;
		ReturnToOriginButton.Pressed -= ReturnToOriginClicked;

		CalibrateController.VelocityChanged -= ManageVelocityChange;
		CalibrateController.OffsetSend -= ManageOffsetSend;
		CalibrateController.CalibrateAxisValuesUpdated -= OnCalibrateAxisValuesChanged;
		CalibrateController.OnPadSteeringChange -= OnPadConnectionChanged;

		Disconnect("visibility_changed", new Callable(this, nameof(OnVisibilityChanged)));
		
		LocalSettings.Singleton.Disconnect(LocalSettings.SignalName.PropagatedSubcategoryChanged,
			Callable.From<StringName, StringName, Variant, Variant>(OnSettingsPropertyChanged));
	}

	#endregion Godot.Override


	#region Methods.On

	public void OnPadConnectionChanged(bool isPadsConnected) {
		Color connected = new Color(0.3f, 1f, 0.5f, 1f);
		Color disconnected = new Color(0.4f, 0.4f, 0.4f, 1f);
		if(LedLamp.HasThemeStylebox("panel")) {
			LedLamp.GetThemeStylebox("panel")
				.Set(
					StyleBoxFlat.PropertyName.BgColor,
					isPadsConnected ? connected : disconnected
				);
		}
	}

	void OnCalibrateAxisValuesChanged() {
		UpdateChooseAxis();
	}

	void OnSettingsPropertyChanged(StringName category, StringName name, Variant oldValue, Variant newValue)
	{
		if (category != nameof(LocalSettings.Singleton.Calibration)) return;

		if (name == nameof(LocalSettings.Singleton.Calibration.CalibrationMotor)) {
			VelocityKnob.UpdateRange(
				LocalSettings.Singleton.Calibration.CalibrationMotor.MinSpeed,
				LocalSettings.Singleton.Calibration.CalibrationMotor.MaxSpeed
			);
		}
	}

	void OnVisibilityChanged() {
		CalibrateController.SetCalibrateEnabled(Visible);
	}

	public Task ControlModeChangedControl(MqttClasses.ControlMode newMode)
	{
		bool newVisibility = (newMode == MqttClasses.ControlMode.EStop ? true : false);
		CalibrateController.SetCalibrateEnabled(newVisibility);
		PanelCover.Visible = !newVisibility;
		return Task.CompletedTask;
	}

	#endregion Methods.On


	#region Wheel.Choosing

	void ChooseAxis(long index)
	{
		CalibrateController.SetChoosenWheel(
			(MqttClasses.CalibrateAxisWheel)index
		);
		UpdateChooseAxis();
	}

	void UpdateChooseAxis()
	{
		
		for (int i = 0; i < AxisModels.Length; i++) {
			if (i == (int)CalibrateController.Singleton.CalibrateAxisValues.ChoosenWheel)
			{
				AxisOptions.Select(i);
				AxisModels[i].Modulate = Color.FromHtml("#00ff00");
			}
			else
			{
				AxisModels[i].Modulate = Color.FromHtml("#505050");
			}
		}
	}

	#endregion Wheel.Choosing


	#region Knob.Changes

	void ManageVelocityChange(float newValue) {
		if(!VelocityKnob.IsSimRunning) {
			VelocityKnob.StartSim(newValue);
		}
		else {
			if(newValue != 0f) {
				VelocityKnob.UpdateSim(newValue);
			} else {
				VelocityKnob.StopSim();
			}
		}
	}

	void ManageOffsetSend(float newValue) {
		OffsetKnob.StartSimSingle(newValue);
	}

	#endregion Knob.Changes


	#region Buttons.Actions

	void VelocityDown() => CalibrateController.StartVelocity();
	void VelocityUp() => CalibrateController.StopVelocitySafe();
	void OffsetClicked() => CalibrateController.SendOffsetAsync();
	void ConfirmClicked() => CalibrateController.SendConfirmAsync();
	void CancelClicked() => CalibrateController.SendCancelAsync();
	void StopClicked() => CalibrateController.SendStopAsync();
	void ReturnToOriginClicked() => CalibrateController.SendReturnToOriginAsync();

	#endregion Buttons.Actions

	void HookButtons(Button[] buttons, ref Action?[] actions, HookAction actionType, Action<long> callback)
	{

		if (buttons is null || callback is null)
			return;

		if (actions.Length != buttons.Length)
			actions = new Action?[buttons.Length];

		for (int i = 0; i < buttons.Length; i++)
		{
			var btn = buttons[i];
			if (btn is null)
				continue;

			if (actionType == HookAction.Enter)
			{
				long idx = i;
				Action handler = () => callback(idx);
				actions[i] = handler;
				btn.Pressed += handler;
			}
			else
			{
				var handler = actions[i];
				if (handler is null)
					continue;
				btn.Pressed -= handler;
				actions[i] = null;
			}
		}
	}

}
