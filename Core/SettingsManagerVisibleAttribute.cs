using Godot;
using RoverControlApp.MVVM.ViewModel;
using System;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using static System.Net.Mime.MediaTypeNames;

namespace RoverControlApp.Core
{

	/// <summary>
	/// Marks setting (from LocalSettings) visible in SettingsManager
	/// </summary>
	[AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
	public sealed class SettingsManagerVisibleAttribute : Attribute
	{
		/// <param name="cellMode">
		/// Sets cell mode. Valid options are:<br/>
		/// <see cref="TreeItem.TreeCellMode.Custom">Custom</see>,
		/// <see cref="TreeItem.TreeCellMode.String">String</see>,
		/// <see cref="TreeItem.TreeCellMode.Check">Check</see>,
		/// <see cref="TreeItem.TreeCellMode.Range">Range</see>.<br/>
		/// With <see cref="TreeItem.TreeCellMode.String">String</see> and <see cref="TreeItem.TreeCellMode.Range">Range</see> see formatData param usage.<br/>
		/// <see cref="TreeItem.TreeCellMode.Custom">Custom</see> is only for marking classes and structs. It don't alters cell type.
		/// </param>
		/// <param name="formatData">
		/// With cellMode: <see cref="TreeItem.TreeCellMode.String">String</see><br/>
		/// RegEx pattern to match - Default: <c>null</c> (RegEx disabled) <br/> 
		/// <b>New value must have whole match with pattern.<br/>
		/// If match is partial or none, new value is ignored.</b><br/>
		/// With cellMode: <see cref="TreeItem.TreeCellMode.Range">Range</see><br/>
		/// min;max;step;exp;type - Default: <c>"0;100;1;f"</c><br/>
		/// <i>min</i> - minimal value for range input [number]<br/>
		/// <i>max</i> - maximal value for range input [number]<br/>
		/// <i>step</i> - step for range input [number]<br/>
		/// <i>exp</i> - use exponential scale for range input [f-false|t-true]<br/>
		/// <i>type</i> - type literal, needed for casts [i|ui|l|ul|f|d|m]<br/>
		/// </param>
		/// <param name="customTooltip">
		/// Adds custom tooltip when hovering mouse over the item.<br/>
		/// If set to null, customName is used as tooltip.
		/// </param>
		/// <param name="customName">
		/// Overrides property/field name on UI.<br/>
		/// If no there is no spaces, name is converted from "PascalCase" to "Human Case"
		/// </param>
		/// <param name="immutableSection">
		/// Notifies SettingsManager that whole object should be created instead of overwriting chosed properties.
		/// </param>
		/// <param name="allowEdit">
		/// Notifies SettingsManager should this property be allowed to edit.
		/// </param>
		public SettingsManagerVisibleAttribute(
			TreeItem.TreeCellMode cellMode = TreeItem.TreeCellMode.Custom,
			string? formatData = null,
			string? customTooltip = null,
			bool immutableSection = false,
			bool allowEdit = true,
			[CallerMemberName] string? customName = null
			)
		{
			if (customName is null)
				throw new ArgumentException("customName is somehow empty!");

			CellMode = cellMode;
			AllowEdit = allowEdit;

			switch (cellMode)
			{
				case TreeItem.TreeCellMode.String:
					if (!string.IsNullOrEmpty(formatData))
					{
						if (RegEx.CreateFromString(formatData).IsValid())
							break;
						EventLogger.LogMessage("SettingsManagerVisibleAttribute", EventLogger.LogLevel.Error, $"Invalid RegEx pattern on property/field \"{customName}\"! (using default instead)");
					}

					formatData = string.Empty;
					break;

				case TreeItem.TreeCellMode.Check:
					formatData = string.Empty;
					break;
				case TreeItem.TreeCellMode.Range:

					if (!string.IsNullOrEmpty(formatData))
					{
						var tester = RegEx.CreateFromString(@"(?i)^(?:[0-9]+(?:\.|,)?[0-9]*;){3}(?:f|t);(?:i|ui|l|ul|f|d|m)$");
						if (tester.Search(formatData) is not null)
							break;
						EventLogger.LogMessage("SettingsManagerVisibleAttribute", EventLogger.LogLevel.Error, $"Invalid format for range on property/field \"{customName}\"! (using default instead)");
					}

					formatData = "0;100;1;f;d";
					break;
				case TreeItem.TreeCellMode.Custom:
					formatData = string.Empty;
					break;
				default:
					throw new NotImplementedException();
			}

			FormatData = formatData;

			if (!customName.Contains(' '))
			{
				var formatter = RegEx.CreateFromString(@"([A-Z]+[a-z0-9]*)");
				var @out = formatter.SearchAll(customName.ToPascalCase());
				StringBuilder stringBuilder = new();
				for (int i = 0; i < @out.Count; i++)
				{
					stringBuilder.Append(@out[i].GetString());
					stringBuilder.Append(' ');
				}
				//remove last space
				stringBuilder.Remove(stringBuilder.Length - 1, 1);
				CustomName = stringBuilder.ToString();
			}
			else
				CustomName = customName;
			CustomTooltip = customTooltip ?? customName;


			ImmutableSection = immutableSection;
		}

		public bool ValidateValue(object value)
		{
			switch (CellMode)
			{
				case TreeItem.TreeCellMode.Check:
					return value is bool;

				case TreeItem.TreeCellMode.String:
					if (value is not string)
						return false;
					if (string.IsNullOrEmpty(FormatData))
						return true;

					string validatedStr = Convert.ToString(value)!;

					var tester = RegEx.CreateFromString(FormatData);
					var test = tester.Search(validatedStr);
					if (test is null || test.GetStart() != 0 || test.GetEnd() != validatedStr.Length)
						return false;
					return true;

				case TreeItem.TreeCellMode.Range:

					decimal mValue = 0m;

					var splittedFormatData = FormatData.Split(';');

					try
					{
						mValue = Convert.ToDecimal(value);
					}
					catch
					{
						return false;
					}


					if (mValue < decimal.Parse(splittedFormatData[0], CultureInfo.InvariantCulture) || mValue > decimal.Parse(splittedFormatData[1], CultureInfo.InvariantCulture) || (mValue % decimal.Parse(splittedFormatData[2], CultureInfo.InvariantCulture)) != 0)
						return false;

					var typeLiteral = splittedFormatData[^1];
					switch (typeLiteral)
					{
						case "i":
							return value is int;
						case "ui":
							return value is uint;
						case "l":
							return value is long;
						case "ul":
							return value is ulong;
						case "f":
							return value is float;
						case "d":
							return value is double;
						case "m":
							return value is decimal;
						default:
							throw new InvalidOperationException();
					}

				case TreeItem.TreeCellMode.Custom:
					return true;
				default:
					throw new InvalidOperationException();
			}
		}

		public TreeItem.TreeCellMode CellMode { get; private set; }
		public string CustomName { get; private set; }
		public string CustomTooltip { get; private set; }

		public string FormatData { get; private set; }
		public bool ImmutableSection { get; init; }
		public bool AllowEdit { get; init; }
		public bool RestartNeeded { get; init; }
	}
}
