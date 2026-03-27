using Godot;
using RoverControlApp.Core;
using System;
using System.Globalization;

[Tool]
public partial class Knob : Control
{
	[Signal] public delegate void ValueChangedEventHandler(float value);

	public enum SimulationMode {Off, Auto, Single, Manual }

	[ExportGroup("Colors")]
	[Export] public Color ArcColor { get; set; } = Colors.Green;
	[Export] public Color GapColor { get; set; } = new Color(0.8f, 0.4f, 0.0f);
	[Export] public Color SimulationColor { get; set; } = new Color(1f, 0.6f, 0f);
	[Export] public Color BackgroundArcColor { get; set; } = new Color(0.2f, 0.2f, 0.2f);

	[ExportGroup("Values")]
	[Export] public float Min { get; set; } = 10f;
	[Export] public float Max { get; set; } = 100f;
	[Export(PropertyHint.Range, "0.001,100,0.001")] public float Step { get; set; } = 1f;

	[Export]
	public float Value
	{
		get => _value;
		set
		{
			float clamped = ClampValue(value);
			if (!Mathf.IsEqualApprox(_value, clamped))
			{
				_value = clamped;
				EmitSignal(SignalName.ValueChanged, clamped);
				QueueRedraw();
				UpdateEditText();
				if (Engine.IsEditorHint()) UpdateConfigurationWarnings();
			}
		}
	}

	[Export]
	public string TitleText
	{
		get => _titleText;
		set { _titleText = value; UpdateTitle(); }
	}

	[ExportGroup("Bipolar")]
	[Export] public bool UseBipolar { get; set; } = false;
	[Export] public bool IncludeZeroCenter { get; set; } = true;
	[Export(PropertyHint.Range, "0,180,0.1")] public float CenterGapDegrees { get; set; } = 10f;

	[ExportGroup("Angles")]
	[Export] public float StartAngle { get; set; } = 120f;
	[Export] public float EndAngle { get; set; } = 360f;
	[Export] public float Rotate { get; set; } = 30f;

	[ExportGroup("Draw")]
	[Export] public int Segments { get; set; } = 100;
	[Export] public int Width { get; set; } = 10;
	[Export] public bool AntiAliasing { get; set; } = true;

	[ExportGroup("Simulation")]
	[Export] public int SimulationWidth { get; set; } = 16;

	[Export] public float SimValue {
		get => _simulationValue; set {
			_simulationValue = value;
			QueueRedraw();
		}
	}

	[Export] public SimulationMode SimMode { get; set; } = SimulationMode.Off;
	[Export(PropertyHint.Range, "0.05,10,0.01")] public float SimulationDuration { get; set; } = 1.0f;

	[ExportGroup("Nodes")]
	[Export] public Label title = new Label();
	[Export] public LineEdit edit = new LineEdit();

	// Private State
	private float _value = 0f;
	private float _simulationValue = 0f;
	private string _titleText = "Knob";
	private bool _dragging = false;

	// Simulation internals
	private float _autoPhase = 0f;
	private bool _singleActive = false;
	private bool _singleReturning = false;
	private float _singleProgress = 0f;
	private float _singleTarget = 0f;
	private float _singleStartValue = 0f;

	public bool IsSimRunning => IsProcessing();

	public override void _Ready()
	{
		UpdateTitle();
		UpdateEditText();

		if (edit != null) {
			edit.TextSubmitted += OnEditSubmitted;
			edit.FocusExited += OnEditFocusExit;
		}

		SetProcess(true);
	}

	public override void _Process(double delta)
	{
		float d = (float)delta;

		if (SimMode == SimulationMode.Manual) {
			QueueRedraw();
			return;
		}

		if (SimMode == SimulationMode.Auto) {
			if (SimulationDuration <= 0.0001f) SimulationDuration = 0.0001f;
			_autoPhase += d * (Mathf.Pi * 2f) / SimulationDuration;
			float t = (Mathf.Sin(_autoPhase) * 0.5f) + 0.5f;
			SimValue = MapTToValue(ApplyEasing(t));
			QueueRedraw();
			return;
		}

		if (SimMode == SimulationMode.Single || _singleActive) {

			if (!_singleActive)
				return;

			if (SimulationDuration <= 0.0001f) SimulationDuration = 0.0001f;
			float step = d / SimulationDuration;

			if (!_singleReturning)
			{
				_singleProgress += step;
				if (_singleProgress >= 1f) {
					_singleProgress = 1f;
					_singleReturning = true;
				}
			}
			else
			{
				_singleProgress -= step;
				if (_singleProgress <= 0f)
				{
					_singleProgress = 0f;
					_singleActive = false;
					_singleReturning = false;
					SimValue = GetBaseForSingle();
					SimMode = SimulationMode.Off;
					QueueRedraw();
					return;
				}
			}

			float eased = ApplyEasing(Mathf.Clamp(_singleProgress, 0f, 1f));
			SimValue = Mathf.Lerp(_singleStartValue, _singleTarget, eased);
			QueueRedraw();
		}
	}


	public void StartSimulation(float value, SimulationMode? simMode)
	{
		float clampedTarget = ClampValue(value);

		if (SimMode != SimulationMode.Single) {
			_singleActive = false;
			_singleReturning = false;
			_singleProgress = 0f;
		}

		if (simMode is SimulationMode sm)
			SimMode = sm;

		if (SimMode != SimulationMode.Single)
			SimValue = clampedTarget;

		SetProcess(true);
		QueueRedraw();
	}

	public void StartSimSingle(float target) {
		_singleStartValue = GetBaseForSingle();
		SimValue = _singleStartValue;

		_singleTarget = ClampValue(target);
		_singleProgress = 0f;
		_singleReturning = false;
		_singleActive = true;

		SimMode = SimulationMode.Single;

		SetProcess(true);
		QueueRedraw();
	}

	public void StartSim(float value) {
		StartSimulation(value, SimulationMode.Manual);
	}

	public void UpdateSim(float value)
	{
		if (!IsProcessing()) return;
		SimValue = ClampValue(value);
	}

	public void StopSim()
	{
		SimMode = SimulationMode.Off;
		_singleActive = false;
		_singleReturning = false;
		_singleProgress = 0f;
		SimValue = GetBaseForSingle();
		QueueRedraw();
		SetProcess(false);
	}


	private float ApplyEasing(float t)
	{
		t = Mathf.Clamp(t, 0f, 1f);
		return t < 0.5f ? 2f * t * t : -1f + (4f - 2f * t) * t;
	}

	private float MapTToValue(float t)
	{
		if (!UseBipolar)
			return Mathf.Lerp(Min, Max, t);

		float mid = 0.5f;
		if (t < mid)
			return -Mathf.Lerp(Min, Max, (mid - t) / mid);
		else
			return Mathf.Lerp(Min, Max, (t - mid) / mid);
	}


	private float GetBaseForSingle() => !UseBipolar ? Min : 0f;
	private Vector2 GetCenter() => Size / 2f;
	private float GetRadius() => (Mathf.Min(Size.X, Size.Y) / 2f) - (Width / 2f);
	private float DegToRad(float d) => Mathf.DegToRad(d);


	public override void _GuiInput(InputEvent e)
	{
		if (Engine.IsEditorHint())
			return;

		if (e is InputEventMouseButton mb)
		{
			if (mb.ButtonIndex == MouseButton.Left)
			{
				_dragging = mb.Pressed;
				if (_dragging) UpdateValueFromMouse(mb.Position);
			}

			if (mb.Pressed)
			{
				if (mb.ButtonIndex == MouseButton.WheelUp) Value += Step;
				if (mb.ButtonIndex == MouseButton.WheelDown) Value -= Step;
			}
		}

		if (e is InputEventMouseMotion motion && _dragging)
			UpdateValueFromMouse(motion.Position);
	}

	private void UpdateValueFromMouse(Vector2 mousePos)
	{
		Vector2 center = Size / 2f;
		Vector2 dir = mousePos - center;

		float rawAngle = Mathf.RadToDeg(Mathf.Atan2(dir.Y, dir.X));

		float start = StartAngle + Rotate;
		float end = EndAngle + Rotate;
		float range = end - start;

		float angle = rawAngle;
		while (angle < start) angle += 360f;
		while (angle >= start + 360f) angle -= 360f;

		if (angle < start)
		{
			if (UseBipolar) Value = dir.X >= 0 ? Max : -Max;
			else Value = Min;
			return;
		}

		if (angle > end)
		{
			if (UseBipolar) Value = dir.X >= 0 ? Max : -Max;
			else Value = Max;
			return;
		}

		float t = Mathf.Clamp((angle - start) / range, 0f, 1f);

		if (!UseBipolar)
		{
			Value = Mathf.Lerp(Min, Max, t);
			return;
		}

		float gapHalf = CenterGapDegrees / 2f;
		float gapT = gapHalf / range;
		float midT = 0.5f;

		if (t > midT - gapT && t < midT + gapT) return;

		if (t >= midT + gapT)
		{
			float localT = (t - (midT + gapT)) / (0.5f - gapT);
			Value = Mathf.Lerp(Min, Max, localT);
		}
		else
		{
			float localT = ((midT - gapT) - t) / (0.5f - gapT);
			Value = -Mathf.Lerp(Min, Max, localT);
		}
	}


	private void UpdateTitle()
	{
		if (title != null) title.Text = _titleText;
	}
	private void UpdateEditText()
	{
		if (edit != null) edit.Text = Math.Round(_value, 1).ToString(CultureInfo.InvariantCulture);
	}
	public void UpdateRange(float min, float max)
	{
		if (max <= min)
			EventLogger.LogMessage(nameof(Knob), EventLogger.LogLevel.Info, "Min is greater or equals to Max, nothings change");

		Min = min;
		Max = max;
		Value = Mathf.Clamp(Value, Min, Max);
		QueueRedraw();
	}

	private void OnEditSubmitted(string text) => ApplyEditValue(text);
	private void OnEditFocusExit() => ApplyEditValue(edit.Text);

	private void ApplyEditValue(string text)
	{
		if (float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out float parsed))
			Value = parsed;
		else
			UpdateEditText();
	}
	private float ClampValue(float val)
	{
		if (!UseBipolar) return Mathf.Clamp(val, Min, Max);
		if (IncludeZeroCenter) return Mathf.Clamp(val, -Max, Max);
		if (val > 0) return Mathf.Clamp(val, Min, Max);
		else return Mathf.Clamp(val, -Max, -Min);
	}


	public override void _Draw()
	{
		Vector2 center = GetCenter();
		float radius = GetRadius();

		float start = StartAngle + Rotate;
		float end = EndAngle + Rotate;
		float range = end - start;

		float mid = start + range / 2f;
		float gapHalf = CenterGapDegrees / 2f;

		float negEnd = mid - gapHalf;
		float posStart = mid + gapHalf;

		// Simulation arc
		if (!UseBipolar)
		{
			float tSim = (_simulationValue - Min) / (Max - Min);
			float currentSim = Mathf.Lerp(start, end, tSim);

			DrawArc(center, radius,
				DegToRad(start),
				DegToRad(currentSim),
				Segments,
				SimulationColor,
				SimulationWidth,
				AntiAliasing);
		}
		else
		{
			if (_simulationValue > 0)
			{
				float t = (_simulationValue - Min) / (Max - Min);
				float angle = Mathf.Lerp(posStart, end, t);

				DrawArc(center, radius,
					DegToRad(mid),
					DegToRad(angle),
					Segments,
					SimulationColor,
					SimulationWidth,
					AntiAliasing);
			}
			else if (_simulationValue < 0)
			{
				float t = (Mathf.Abs(_simulationValue) - Min) / (Max - Min);
				float angle = Mathf.Lerp(negEnd, start, t);

				DrawArc(center, radius,
					DegToRad(angle),
					DegToRad(mid),
					Segments,
					SimulationColor,
					SimulationWidth,
					AntiAliasing);
			}
		}

		// Background Arc
		DrawArc(center, radius,
			DegToRad(start),
			DegToRad(end),
			Segments,
			BackgroundArcColor,
			Width,
			AntiAliasing);

		if (!UseBipolar)
		{
			float t = (Value - Min) / (Max - Min);
			float current = Mathf.Lerp(start, end, t);

			DrawArc(center, radius,
				DegToRad(start),
				DegToRad(current),
				Segments,
				ArcColor,
				Width,
				AntiAliasing);

			return;
		}

		DrawArc(center, radius,
			DegToRad(negEnd),
			DegToRad(posStart),
			Segments,
			GapColor,
			Width,
			AntiAliasing);

		if (Value > 0)
		{
			float t = (Value - Min) / (Max - Min);
			float angle = Mathf.Lerp(posStart, end, t);

			DrawArc(center, radius,
				DegToRad(posStart),
				DegToRad(angle),
				Segments,
				ArcColor,
				Width,
				AntiAliasing);
		}
		else if (Value < 0)
		{
			float t = (Mathf.Abs(Value) - Min) / (Max - Min);
			float angle = Mathf.Lerp(negEnd, start, t);

			DrawArc(center, radius,
				DegToRad(negEnd),
				DegToRad(angle),
				Segments,
				ArcColor,
				Width,
				AntiAliasing);
		}
	}

}
