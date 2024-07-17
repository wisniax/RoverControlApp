using Godot;
using RoverControlApp.Core;
using System.Runtime.CompilerServices;

namespace RoverControlApp.MVVM.Model.Settings
{
	public abstract partial class SettingBase : RefCounted
	{
		[Signal]
		public delegate void SettingChangedEventHandler(StringName name, Variant oldValue, Variant newValue);

		[Signal]
		public delegate void SectionChangedEventHandler(StringName property, Variant oldValue, Variant newValue);


		protected void EmitSignal_SettingChanged<[MustBeVariant] FieldType>(ref FieldType field, FieldType @value, [CallerMemberName] string propertyName = "") where FieldType : notnull
		{
			Variant oldValue = Variant.From(field);
			field = @value;
			EmitSignal(SignalName.SettingChanged, propertyName, oldValue, Variant.From(@value));

			EventLogger.LogMessageDebug("SettingsBase", EventLogger.LogLevel.Verbose, $"Property \"{propertyName}\" was changed from:\n{oldValue.As<FieldType>()}\n   to:\n{@value}");
		}

		protected void EmitSignal_SectionChanged<[MustBeVariant] FieldType>(ref FieldType field, FieldType @value, [CallerMemberName] string propertyName = "") where FieldType : notnull
		{
			Variant oldValue = Variant.From(field);
			field = @value;
			EmitSignal(SignalName.SectionChanged, propertyName, oldValue, Variant.From(@value));

			EventLogger.LogMessageDebug("SettingsBase", EventLogger.LogLevel.Verbose, $"Section \"{propertyName}\" was changed from:\n{oldValue.As<FieldType>()}\n   to:\n{@value}");
		}
	}
}
