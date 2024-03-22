using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoverControlApp.MVVM.Model.Settings
{
	public abstract partial class SettingBase : GodotObject
	{
		[Signal]
		public delegate void SettingChangedEventHandler(StringName name, Variant oldValue, Variant newValue);

		[Signal]
		public delegate void SectionChangedEventHandler(StringName property, Variant oldValue, Variant newValue);
	}
}
