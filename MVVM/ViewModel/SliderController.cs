using Godot;
using RoverControlApp.MVVM.Model;

namespace RoverControlApp.MVVM.ViewModel;

public partial class SliderController : Godot.Range
{
	[Signal]
	public delegate void ValueChangedOutputEventHandler(int val);

	[Export]
	public Color ForegroundColor
	{
		get
		{
			return _foregroundColor;
		}
		set
		{
			_foregroundColor = value;
			if (_sliderGradient is null) return;
			_sliderGradient.ActiveColor = value;
		}
	}

	[Export]
	public Color BackgroundColor
	{
		get
		{
			return _backgroundColor;
		}
		set
		{
			_backgroundColor = value;
			if (_sliderGradient is null) return;
			_sliderGradient.ActiveColor = value;
		}
	}

	Color _foregroundColor;
	Color _backgroundColor;

	StyleBoxTexture _localStyle;
	bool skipChange = false;
	bool skipUpdate = false;
	SliderGradient _sliderGradient;





	public void InputValue(float val)
	{
		if (skipUpdate)
			return;

		skipChange = true;
		Value = (double)val;
		skipChange = false;

		if (_sliderGradient is null)
			return;

		UpdateGradient();
	}

	public void InputMaxValue(float val)
	{
		MaxValue = (double)val;

		if (_sliderGradient is null)
			return;

		UpdateGradient();
	}

	public void InputMinValue(float val)
	{
		MinValue = (double)val;

		if (_sliderGradient is null)
			return;

		UpdateGradient();
	}

	void _on_value_changed(float val)
	{
		if (skipChange)
			return;

		skipUpdate = true;
		EmitSignal(SignalName.ValueChangedOutput, val);
		skipUpdate = false;

		UpdateGradient();
	}

	void UpdateGradient()
	{
		float range = (float)(MaxValue - MinValue);
		float rangeVal = (float)Value - (float)MinValue;
		_sliderGradient.SliderValue = (rangeVal / range);
	}

	public override void _Ready()
	{
		_sliderGradient = new(ForegroundColor, BackgroundColor);
		_localStyle = new() { TextureMarginBottom = 2, TextureMarginTop = 2, Texture = _sliderGradient.Texture, };
		AddThemeStyleboxOverride("slider", _localStyle);

		//StyleOut
		AddThemeStyleboxOverride("grabber_area", new StyleBoxEmpty());
		AddThemeStyleboxOverride("grabber_area_highlight", new StyleBoxEmpty());
		AddThemeIconOverride("grabber", new ImageTexture());
		AddThemeIconOverride("grabber_highlight", new ImageTexture());
		AddThemeIconOverride("grabber_disabled", new ImageTexture());

		UpdateGradient();

		Connect(SignalName.ValueChanged, new Callable(this, "_on_value_changed"));
	}
}
