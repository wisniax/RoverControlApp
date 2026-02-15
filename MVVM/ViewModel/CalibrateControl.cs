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

	[ExportGroup("Offset")]
	[Export] private LineEdit OffsetBox = new LineEdit();
	[Export] private Button[] OffestButtons = new Button[6];
	[Export] private HScrollBar OffsetScroll = new HScrollBar();

	[ExportGroup("Velocity")]
	[Export] private LineEdit VelocityBox = new LineEdit();
	[Export] private Button[] VelocityButtons = new Button[6];
	[Export] private HScrollBar VelocityScroll = new HScrollBar();

	[ExportGroup("Actions Button")]
	[Export] private Button OffsetButton = new Button();
	[Export] private Button VelocityButton = new Button();
	[Export] private Button ConfirmButton = new Button();
	[Export] private Button CancelButton = new Button();
	[Export] private Button StopButton = new Button();
	[Export] private Button ReturnToOriginButton = new Button();

	[ExportGroup("Cover")]
	[Export] private Panel PanelCover = new Panel();

	private Action?[] _AxisBtnHandlers = Array.Empty<Action?>();
	private Action?[] _OffsetBtnHandlers = Array.Empty<Action?>();
	private Action?[] _VelocityBtnHandlers = Array.Empty<Action?>();

	private int[] _valueDeltas = new int[] { -5, -2, -1, 1, 2, 5 };

	private bool _calibrateEnabled = true;
	private int _wheelValue = -1; // -1 -> none, 0 to 3 - FL, FR, BL, BR
	private byte _vescId = byte.MaxValue;
	private float _offsetValue = 1.0f;
	private float _velocityValue = 1.0f;

	// Managin private Values and LocalSettingsMemory
	private float OffsetValue
	{
		get => _offsetValue;
		set
		{
			_offsetValue = value;
			LocalSettingsMemory.Singleton.CalibrateAxis.OffsetValue = value;
		}
	}

	private float VelocityValue
	{
		get => _velocityValue;
		set
		{
			_velocityValue = value;
			LocalSettingsMemory.Singleton.CalibrateAxis.VelocityValue = value;
		}
	}

	private bool CalibrateEnabled
	{
		get => _calibrateEnabled;
		set
		{
			_calibrateEnabled = value;
			LocalSettingsMemory.Singleton.CalibrateAxis.PanelVisibilty = value;
		}
	}

	private int WheelValue
	{
		get => _wheelValue;
		set
		{
			_wheelValue = value;
			if (TryGetSelectedVescId(out var vescId))
			{
				_vescId = vescId;
				LocalSettingsMemory.Singleton.CalibrateAxis.ChoosenAxis = vescId;
			}
		}
	}


	// Getting the VescID from WheelData in LocalSettings with conversion from string to byte
	private byte GetVescId(int wheelID)
	{
		try
		{
			var raw = wheelID switch
			{
				0 => LocalSettings.Singleton.WheelData.FrontLeftTurn,
				1 => LocalSettings.Singleton.WheelData.FrontRightTurn,
				2 => LocalSettings.Singleton.WheelData.BackLeftTurn,
				3 => LocalSettings.Singleton.WheelData.BackRightTurn,
				_ => string.Empty
			};

			if (string.IsNullOrWhiteSpace(raw))
				return byte.MaxValue;

			return (byte)Convert.ToInt32(raw.Replace("0x", ""), 16);
		}
		catch (Exception e)
		{
			EventLogger.LogMessage(nameof(CalibrateControl), EventLogger.LogLevel.Warning, $"GetVescId parse error for wheel {wheelID}: {e.Message}");
			return byte.MaxValue;
		}
	}

	// Validation for gettings the vescId
	private bool TryGetSelectedVescId(out byte vescId)
	{
		vescId = byte.MaxValue;

		if (_wheelValue < 0 || _wheelValue >= AxisModels.Length)
		{
			EventLogger.LogMessage("CalibrateControl", EventLogger.LogLevel.Warning, "No wheel selected.");
			return false;
		}

		vescId = GetVescId(_wheelValue);
		if (vescId == byte.MaxValue)
		{
			EventLogger.LogMessage("CalibrateControl", EventLogger.LogLevel.Warning, "Invalid VESC id parsed from settings.");
			return false;
		}

		return true;
	}


	public override void _EnterTree()
	{
		// Handlers for Wheel Axis changes
		AxisOptions.ItemSelected += index => ChooseAxis(index);
		HookButtons(AxisButtons, ref _AxisBtnHandlers, HookAction.Enter, ChooseAxis);

		// Handlers for LineEdit and Sliders
		OffsetBox.TextChanged += newValue => ValueChanged<string>(newValue, v => OffsetValue = v, OffsetBox, OffsetScroll);
		OffsetScroll.ValueChanged += newValue => ValueChanged<double>(newValue, v => OffsetValue = v, OffsetBox, OffsetScroll);

		VelocityBox.TextChanged += newValue => ValueChanged<string>(newValue, v => VelocityValue = v, VelocityBox, VelocityScroll);
		VelocityScroll.ValueChanged += newValue => ValueChanged<double>(newValue, v => VelocityValue = v, VelocityBox, VelocityScroll);

		// Handlers for incerments buttons
		HookDeltaButtons(OffestButtons, ref _OffsetBtnHandlers, HookAction.Enter, (deltaIndex) => ValueChanged<double>(_offsetValue + (_valueDeltas[deltaIndex]), v => OffsetValue = v, OffsetBox, OffsetScroll));
		HookDeltaButtons(VelocityButtons, ref _VelocityBtnHandlers, HookAction.Enter, (deltaIndex) => ValueChanged<double>(_velocityValue + (_valueDeltas[deltaIndex]), v => VelocityValue = v, VelocityBox, VelocityScroll));

		// Handlers for functional buttons
		OffsetButton.Pressed += OffsetClicked;
		VelocityButton.ButtonDown += VelocityDown;
		VelocityButton.ButtonUp += VelocityUp;
		ConfirmButton.Pressed += ConfirmClicked;
		CancelButton.Pressed += CancelClicked;
		StopButton.Pressed += StopClicked;
		ReturnToOriginButton.Pressed += ReturnToOriginClicked;

		Connect("visibility_changed", new Callable(this, nameof(OnVisibilityChanged)));
	}

	public override void _Ready()
	{
		for (int i = 0; i < AxisModels.Length; i++)
		{
			AxisModels[i].Modulate = Color.FromHtml("#505050");
		}
		OffsetValue = Convert.ToSingle(OffsetScroll.Value);
		VelocityValue = Convert.ToSingle(VelocityScroll.Value);
		CalibrateEnabled = PanelCover.Visible;

		LocalSettingsMemory.Singleton.Connect(LocalSettingsMemory.SignalName.PropagatedPropertyChanged,
			Callable.From<StringName, StringName, Variant, Variant>(OnSettingsMemoryPropertyChanged)
		);
	}

	public override void _ExitTree()
	{
		// Handlers for Wheel Axis changes
		AxisOptions.ItemSelected -= index => ChooseAxis(index);
		HookButtons(AxisButtons, ref _AxisBtnHandlers, HookAction.Exit, ChooseAxis);

		// Handlers for LineEdit and Sliders
		OffsetBox.TextChanged -= newValue => ValueChanged<string>(newValue, v => OffsetValue = v, OffsetBox, OffsetScroll);
		OffsetScroll.ValueChanged -= newValue => ValueChanged<double>(newValue, v => OffsetValue = v, OffsetBox, OffsetScroll);

		VelocityBox.TextChanged -= newValue => ValueChanged<string>(newValue, v => VelocityValue = v, VelocityBox, VelocityScroll);
		VelocityScroll.ValueChanged -= newValue => ValueChanged<double>(newValue, v => VelocityValue = v, VelocityBox, VelocityScroll);

		// Handlers for incerments buttons
		HookDeltaButtons(OffestButtons, ref _OffsetBtnHandlers, HookAction.Exit, null);
		HookDeltaButtons(VelocityButtons, ref _VelocityBtnHandlers, HookAction.Exit, null);

		// Handlers for functional buttons
		OffsetButton.Pressed -= OffsetClicked;
		VelocityButton.ButtonDown -= VelocityDown;
		VelocityButton.ButtonUp -= VelocityUp;
		ConfirmButton.Pressed -= ConfirmClicked;
		CancelButton.Pressed -= CancelClicked;
		StopButton.Pressed -= StopClicked;
		ReturnToOriginButton.Pressed -= ReturnToOriginClicked;

		Disconnect("visibility_changed", new Callable(this, nameof(OnVisibilityChanged)));
		LocalSettingsMemory.Singleton.Disconnect(LocalSettingsMemory.SignalName.PropagatedPropertyChanged,
			Callable.From<StringName, StringName, Variant, Variant>(OnSettingsMemoryPropertyChanged));
	}


	// Changing Axis via Pad inputs
	void OnSettingsMemoryPropertyChanged(StringName category, StringName name, Variant oldValue, Variant newValue)
	{
		if (category != nameof(LocalSettingsMemory.Singleton.CalibrateAxis)) return;

		if (name == nameof(LocalSettingsMemory.CalibrateAxis.ChoosenWheel))
		{
			ChooseAxis(LocalSettingsMemory.Singleton.CalibrateAxis.ChoosenWheel);
		}
	}

	// Changing ui visibility
	void OnVisibilityChanged()
	{
		CalibrateEnabled = Visible;
	}

	// Some Actions on Control Mode Changed
	public Task ControlModeChangedControl(MqttClasses.ControlMode newMode)
	{
		CalibrateEnabled = (newMode == MqttClasses.ControlMode.EStop ? true : false);
		PanelCover.Visible = CalibrateEnabled ? false : true;

		// Making sure to Cancel if no action provided before changing the ControlMode
		if (
			newMode != MqttClasses.ControlMode.EStop &&
			(CalibrateController.LastAction != CalibrateController.LastActions.Action &&
			CalibrateController.LastAction != CalibrateController.LastActions.None)
		)
		{
			if (_vescId != byte.MaxValue)
			{
				// Stop velocities to avoid conflicts, then send Cancel
				CalibrateController.StopVelocity();
				CalibrateController.SendCancelAsync(_vescId);
			}
		}

		return Task.CompletedTask;
	}

	// Changing the choosen axis, updating the vescId
	void ChooseAxis(long index)
	{

		// Making sure to Cancel if no action provided
		if (
			CalibrateController.LastAction != CalibrateController.LastActions.Action &&
			CalibrateController.LastAction != CalibrateController.LastActions.None
		)
		{
			// Stop velocities to avoid conflicts, then send Cancel
			CalibrateController.StopVelocity();
			CalibrateController.SendCancelAsync(_vescId);
		}

		for (int i = 0; i < AxisModels.Length; i++)
		{
			if (i == index)
			{
				WheelValue = i;
				AxisOptions.Select(i);
				AxisModels[i].Modulate = Color.FromHtml("#00ff00");
			}
			else
			{
				AxisModels[i].Modulate = Color.FromHtml("#505050");
			}
		}
	}

	// Managing control on value via ui elements TextEdit and Slider
	void ValueChanged<T>(T newValue, Action<float> setter, LineEdit line, HScrollBar slider)
	{
		try
		{
			float parsed = Convert.ToSingle(newValue);
			float clamped = Mathf.Clamp(parsed, (float)slider.MinValue, (float)slider.MaxValue);

			setter(clamped);

			line.Text = clamped.ToString();
			slider.Value = clamped;

		}
		catch (Exception)
		{
			EventLogger.LogMessage("ValueChanged", EventLogger.LogLevel.Warning, "Exeption in ValueChanged");
		}
	}


	// Velocity Action on Button Hold, Actions Down and Up
	void VelocityDown()
	{
		if (!TryGetSelectedVescId(out var vescId)) return;
		CalibrateController.StartVelocity(vescId, VelocityValue);
	}

	void VelocityUp()
	{
		CalibrateController.StopVelocity();
	}


	// Others actions Actions 
	void OffsetClicked()
	{
		if (!TryGetSelectedVescId(out var vescId)) return;
		CalibrateController.SendOffsetAsync(vescId, OffsetValue);
	}

	void ConfirmClicked()
	{
		if (!TryGetSelectedVescId(out var vescId)) return;
		CalibrateController.SendConfirmAsync(vescId);
	}

	void CancelClicked()
	{
		if (!TryGetSelectedVescId(out var vescId)) return;
		CalibrateController.SendCancelAsync(vescId);
	}

	void StopClicked()
	{
		if (!TryGetSelectedVescId(out var vescId)) return;
		CalibrateController.SendStopAsync(vescId);
	}

	void ReturnToOriginClicked()
	{
		if (!TryGetSelectedVescId(out var vescId)) return;
		CalibrateController.SendReturnToOriginAsync(vescId);
	}


	// Hook for Axis choose Buttons
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

	// Hook for Increment Buttons
	void HookDeltaButtons(Button[] buttons, ref Action?[] actions, HookAction actionType, Action<int>? callback)
	{
		if (buttons is null)
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
				int idx = i;
				Action handler = () =>
				{
					try
					{
						callback?.Invoke(idx);
					}
					catch (Exception ex)
					{
						EventLogger.LogMessage(nameof(CalibrateControl), EventLogger.LogLevel.Error, $"Delta button handler exception: {ex}");
					}
				};
				actions[i] = handler;
				btn.Pressed += handler;
			}
			else
			{
				var handler = actions[i];
				if (handler is null)
					continue;
				try
				{
					btn.Pressed -= handler;
				}
				catch { }
				actions[i] = null;
			}
		}
	}

}
