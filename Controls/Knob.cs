using Godot;
using System;
using System.Globalization;

[Tool]
public partial class Knob : Control
{

	[Signal]
	public delegate void ValueChangedEventHandler(float value);

	[ExportGroup("Colors")]
	[Export] public Color ArcColor { get; set; } = Colors.Green;
	[Export] public Color GapColor { get; set; } = new Color(0.8f, 0.4f, 0.0f);
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

				if (Engine.IsEditorHint())
					UpdateConfigurationWarnings();
			}
		}
	}

	[Export]
	public string TitleText
	{
		get => _titleText;
		set
		{
			_titleText = value;
			UpdateTitle();
		}
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

	[ExportGroup("Nodes")]
	[Export] public Label title = new Label();
	[Export] public LineEdit edit = new LineEdit();

	// Private State
	private float _value = 0f;
	private string _titleText = "Knob";
	private bool _dragging = false;


	public override void _GuiInput(InputEvent e)
	{
		if (Engine.IsEditorHint())
			return;

		if (e is InputEventMouseButton mb)
		{
			if (mb.ButtonIndex == MouseButton.Left)
			{
				_dragging = mb.Pressed;
				if (_dragging)
					UpdateValueFromMouse(mb.Position);
			}

			if (mb.Pressed)
			{
				if (mb.ButtonIndex == MouseButton.WheelUp)
					Value += Step;

				if (mb.ButtonIndex == MouseButton.WheelDown)
					Value -= Step;
			}
		}

		if (e is InputEventMouseMotion motion && _dragging)
			UpdateValueFromMouse(motion.Position);
	}


	// Geometry
	private Vector2 GetCenter() => Size / 2f;

	private float GetRadius()
	{
		float minSide = Mathf.Min(Size.X, Size.Y);
		return (minSide / 2f) - (Width / 2f);
	}

	private float DegToRad(float d) => Mathf.DegToRad(d);


	public override void _Ready()
	{
		UpdateTitle();
		UpdateEditText();

		if (edit != null) {
			edit.TextSubmitted += OnEditSubmitted;
			edit.FocusExited += OnEditFocusExit;
		}
	}

	// Title Update
	private void UpdateTitle()
	{
		if (title != null)
			title.Text = _titleText;
	}

	// Changing Value by Text
	private void UpdateEditText()
	{
		if (edit != null)
			edit.Text = Math.Round(_value, 1).ToString(CultureInfo.InvariantCulture);
	}

	private void OnEditSubmitted(string text)
	{
		ApplyEditValue(text);
	}

	private void OnEditFocusExit()
	{
		ApplyEditValue(edit.Text);
	}

	private void ApplyEditValue(string text)
	{
		if (float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out float parsed))
		{
			Value = parsed;
		}
		else
		{
			UpdateEditText();
		}
	}

	// Clamping the Value
	private float ClampValue(float val)
	{
		if (!UseBipolar)
			return Mathf.Clamp(val, Min, Max);

		if (IncludeZeroCenter)
			return Mathf.Clamp(val, -Max, Max);

		if (val > 0)
			return Mathf.Clamp(val, Min, Max);
		else
			return Mathf.Clamp(val, -Max, -Min);
	}

	// Editing the Value by mouse
	private void UpdateValueFromMouse(Vector2 mousePos)
	{
		Vector2 center = Size / 2f;
		Vector2 dir = mousePos - center;

		float rawAngle = Mathf.RadToDeg(Mathf.Atan2(dir.Y, dir.X));

		float start = StartAngle + Rotate;
		float end = EndAngle + Rotate;
		float range = end - start;

		float angle = rawAngle;
		while (angle < start)
			angle += 360f;
		while (angle >= start + 360f)
			angle -= 360f;

		if (angle < start)
		{
			if (UseBipolar)
				Value = dir.X >= 0 ? Max : -Max;
			else
				Value = Min;

			return;
		}

		if (angle > end)
		{
			if (UseBipolar)
				Value = dir.X >= 0 ? Max : -Max;
			else
				Value = Max;

			return;
		}

		float t = Mathf.Clamp((angle - start) / range, 0f, 1f);

		if (!UseBipolar)
		{
			Value = Mathf.Lerp(Min, Max, t);
			return;
		}

		// For Bipolar

		float gapHalf = CenterGapDegrees / 2f;
		float gapT = gapHalf / range;
		float midT = 0.5f;

		if (t > midT - gapT && t < midT + gapT)
			return;

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

	// Drawing the Arcs
	public override void _Draw()
	{
		Vector2 center = GetCenter();
		float radius = GetRadius();

		float start = StartAngle + Rotate;
		float end = EndAngle + Rotate;
		float range = end - start;

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

		float mid = start + range / 2f;
		float gapHalf = CenterGapDegrees / 2f;

		float negEnd = mid - gapHalf;
		float posStart = mid + gapHalf;

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
