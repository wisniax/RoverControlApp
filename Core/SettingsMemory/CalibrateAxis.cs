using System;

namespace RoverControlApp.Core.SettingsMemory;

public partial class CalibrateAxis : SettingMemoryBase, ICloneable
{

	public CalibrateAxis()
	{
		_panelVisibilty = false;
		_offsetValue = 0.0f;
		_velocityValue = 0.0f;
		_choosenAxis = byte.MaxValue;
		_choosenWheel = -1;
	}

	public CalibrateAxis(bool panelVisibilty, float offsetValue, float velocityValue, byte choosenAxis, int choosenWheel)
	{
		_panelVisibilty = panelVisibilty;
		_offsetValue = offsetValue;
		_velocityValue = velocityValue;
		_choosenAxis = choosenAxis;
		_choosenWheel = choosenWheel;
	}

	public object Clone()
	{
		return new CalibrateAxis()
		{
			PanelVisibilty = _panelVisibilty,
			OffsetValue = _offsetValue,
			VelocityValue = _velocityValue,
			ChoosenAxis = _choosenAxis,
			ChoosenWheel = _choosenWheel
		};
	}

	public bool PanelVisibilty
	{
		get => _panelVisibilty;
		set => EmitSignal_SettingMemoryChanged(ref _panelVisibilty, value);
	}

	public float OffsetValue
	{
		get => _offsetValue;
		set => EmitSignal_SettingMemoryChanged(ref _offsetValue, value);
	}

	public float VelocityValue
	{
		get => _velocityValue;
		set => EmitSignal_SettingMemoryChanged(ref _velocityValue, value);
	}

	public byte ChoosenAxis
	{
		get => _choosenAxis;
		set => EmitSignal_SettingMemoryChanged(ref _choosenAxis, value);
	}

	public int ChoosenWheel
	{
		get => _choosenWheel;
		set => EmitSignal_SettingMemoryChanged(ref _choosenWheel, value);
	}

	bool _panelVisibilty;
	float _offsetValue;
	float _velocityValue;
	byte _choosenAxis;
	int _choosenWheel;
}

