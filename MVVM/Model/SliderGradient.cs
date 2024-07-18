using Godot;

namespace RoverControlApp.MVVM.Model;

public class SliderGradient
{
	GradientTexture1D _gradientTexture;

	public Color ActiveColor
	{
		get => _gradientTexture.Gradient.GetColor(1);
		set
		{
			_gradientTexture.Gradient.SetColor(1, value);
			_gradientTexture.Gradient.SetColor(2, value);
		}
	}
	public Color InActiveColor
	{
		get => _gradientTexture.Gradient.GetColor(0);
		set
		{
			_gradientTexture.Gradient.SetColor(0, value);
			_gradientTexture.Gradient.SetColor(3, value);
		}
	}

	public float SliderValue
	{
		get
		{
			if (Mathf.IsEqualApprox(_gradientTexture.Gradient.GetOffset(1), _gradientTexture.Gradient.GetOffset(3)))
				return 0.5f;
			else if (_gradientTexture.Gradient.GetOffset(1) < 0.5f)
				return _gradientTexture.Gradient.GetOffset(1);
			else
				return _gradientTexture.Gradient.GetOffset(3);
		}
		set
		{
			_gradientTexture.Gradient.SetOffset(1, Mathf.Min(0.49999999f, Mathf.Max(0.00000001f, value)));
			_gradientTexture.Gradient.SetOffset(3, Mathf.Max(0.50000001f, Mathf.Min(0.99999999f, value)));
		}
	}

	public SliderGradient(Color active, Color inActive, float startValue = 0.0f)
	{
		_gradientTexture = new()
		{
			Width = 512,
			Gradient = new()
			{
				InterpolationMode = Gradient.InterpolationModeEnum.Constant,
				Offsets = new[] { 0.0f, 0.49999999f, 0.5f, 0.50000001f },
				Colors = new[] { inActive, active, active, inActive }
			}
		};

		ActiveColor = active;
		InActiveColor = inActive;
	}

	public Texture2D Texture => _gradientTexture;

}
