using Godot;
using RoverControlApp.Core.JSONConverters;
using System;
using System.Text.Json.Serialization;

namespace RoverControlApp.Core.Settings;

[JsonConverter(typeof(CanIdsConverter))]
public partial class CanIds : SettingBase, ICloneable
{
	public const string IdPattern = @"^(?:0[xX])?[0-9a-fA-F]{1,2}$";
	public const string OptionalIdPattern = @"^(?:(?:0[xX])?[0-9a-fA-F]{1,2})?$";

	public CanIds()
	{
		_driveMotors = new DriveMotorCanIds();
		_turnMotors = new TurnMotorCanIds();
		_imuBoards = new ImuBoardCanIds();
		_manipulator = new ManipulatorCanIds();
		_manipulator = new ManipulatorCanIds();
		_sampler = new SamplerCanIds();
	}

	public CanIds(DriveMotorCanIds driveMotors, TurnMotorCanIds turnMotors, ImuBoardCanIds imuBoards, ManipulatorCanIds manipulator, SamplerCanIds sampler)
	{
		_driveMotors = driveMotors;
		_turnMotors = turnMotors;
		_imuBoards = imuBoards;
		_manipulator = manipulator;
		_sampler = sampler;
	}

	public object Clone()
	{
		return new CanIds(
			(DriveMotorCanIds)_driveMotors.Clone(),
			(TurnMotorCanIds)_turnMotors.Clone(),
			(ImuBoardCanIds)_imuBoards.Clone(),
			(ManipulatorCanIds)_manipulator.Clone(),
			(SamplerCanIds)_sampler.Clone());
	}

	[SettingsManagerVisible(immutableSection: true)]
	public DriveMotorCanIds DriveMotors
	{
		get => _driveMotors;
		set
		{
			EmitSignal_SectionChanged(ref _driveMotors, value);
		}
	}

	[SettingsManagerVisible(immutableSection: true)]
	public TurnMotorCanIds TurnMotors
	{
		get => _turnMotors;
		set
		{
			EmitSignal_SectionChanged(ref _turnMotors, value);
		}
	}

	[SettingsManagerVisible(immutableSection: true)]
	public ImuBoardCanIds ImuBoards
	{
		get => _imuBoards;
		set
		{
			EmitSignal_SectionChanged(ref _imuBoards, value);
		}
	}

	[SettingsManagerVisible(immutableSection: true)]
	public ManipulatorCanIds Manipulator
	{
		get => _manipulator;
		set
		{
			EmitSignal_SectionChanged(ref _manipulator, value);
		}
	}

	[SettingsManagerVisible(immutableSection: true)]
	public SamplerCanIds Sampler
	{
		get => _sampler;
		set
		{
			EmitSignal_SectionChanged(ref _sampler, value);
		}
	}

	private DriveMotorCanIds _driveMotors;
	private TurnMotorCanIds _turnMotors;
	private ImuBoardCanIds _imuBoards;
	private ManipulatorCanIds _manipulator;
	private SamplerCanIds _sampler;
}
