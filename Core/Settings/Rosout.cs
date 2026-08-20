using Godot;
using RoverControlApp.Core.JSONConverters;
using System;
using System.Text.Json.Serialization;

namespace RoverControlApp.Core.Settings;

[JsonConverter(typeof(RosoutConverter))]
public partial class Rosout : SettingBase, ICloneable
{
	public Rosout()
	{
		_message = true;
		_function = true;
		_file = true;
		_line = true;
	}
	
	public Rosout(bool mes, bool fun, bool file, bool line)
	{
		_message = mes;
		_function = fun;
		_file = file;
		_line = line;
	}
	
	public object Clone()
	{
		return new Rosout()
		{
			Message = _message,
			Function = _function,
			File = _file,
			Line = _line
		};
	}
	
	[SettingsManagerVisible(cellMode: TreeItem.TreeCellMode.Check)]
	public bool Message
	{
		get => _message;
		set => EmitSignal_SettingChanged(ref _message, value);
	}
	
	[SettingsManagerVisible(cellMode: TreeItem.TreeCellMode.Check)]
	public bool Function
	{
		get => _function;
		set => EmitSignal_SettingChanged(ref _function, value);
	}
	
	[SettingsManagerVisible(cellMode: TreeItem.TreeCellMode.Check)]
	public bool File
	{
		get => _file;
		set => EmitSignal_SettingChanged(ref _file, value);
	}
	
	[SettingsManagerVisible(cellMode: TreeItem.TreeCellMode.Check)]
	public bool Line
	{
		get => _line;
		set => EmitSignal_SettingChanged(ref _line, value);
	}
	
	bool _message;
	bool _function;
	bool _file;
	bool _line;
}
