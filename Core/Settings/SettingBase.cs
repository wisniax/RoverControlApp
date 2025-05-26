using Godot;
using System.Runtime.CompilerServices;

namespace RoverControlApp.Core.Settings
{
	public abstract partial class SettingBase : RefCounted
	{
		[Signal]
		public delegate void PropertyChangedEventHandler(StringName name, Variant oldValue, Variant newValue);

		[Signal]
		public delegate void SubcategoryChangedEventHandler(StringName property, Variant oldValue, Variant newValue);


		protected void EmitSignal_SettingChanged<[MustBeVariant] FieldType>(ref FieldType field, FieldType @value, [CallerMemberName] string propertyName = "") where FieldType : notnull
		{
			Variant oldValue = Variant.From(field);
			field = @value;
			
			EventLogger.LogMessage("SettingsBase", EventLogger.LogLevel.Verbose, $"Property \"{propertyName}\" was changed from:\n{oldValue.As<FieldType>()}\n   to:\n{@value}");
			
			EmitSignal(SignalName.PropertyChanged, propertyName, oldValue, Variant.From(@value));
		}

		protected void EmitSignal_SectionChanged<[MustBeVariant] FieldType>(ref FieldType field, FieldType @value, [CallerMemberName] string propertyName = "") where FieldType : notnull
		{
			Variant oldValue = Variant.From(field);
			field = @value;

			EventLogger.LogMessage("SettingsBase", EventLogger.LogLevel.Verbose, $"Section \"{propertyName}\" was changed from:\n{oldValue.As<FieldType>()}\n   to:\n{@value}");

			EmitSignal(SignalName.SubcategoryChanged, propertyName, oldValue, Variant.From(@value));
		}
	}
}
