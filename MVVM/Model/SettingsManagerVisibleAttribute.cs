using Godot;
using RoverControlApp.MVVM.ViewModel;
using System;
using System.Linq;
using System.Runtime.CompilerServices;

namespace RoverControlApp.MVVM.Model
{

    /// <summary>
    /// Marks setting (from LocalSettings) visible in SettingsManager
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, Inherited = false, AllowMultiple = false)]
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
        /// <br/>
        /// With cellMode: <see cref="TreeItem.TreeCellMode.Range">Range</see><br/>
        /// min;max;step;exp;type - Default: <c>"0;100;1;f"</c><br/>
        /// <i>min</i> - minimal value for range input [number]<br/>
        /// <i>max</i> - maximal value for range input [number]<br/>
        /// <i>step</i> - step for range input [number]<br/>
        /// <i>exp</i> - use exponential scale for range input [f-false|t-true]<br/>
        /// <i>type</i> - explicit type declaracion, needed for casts [i|ui|l|ul|f|d|m]<br/>
        /// </param>
        public SettingsManagerVisibleAttribute(
            TreeItem.TreeCellMode cellMode = TreeItem.TreeCellMode.Custom,
            string formatData = null,
            [CallerMemberName] string propertyName = null)
        {
            CellMode = cellMode;

            switch (cellMode)
            {
                case TreeItem.TreeCellMode.String:
                    if (!string.IsNullOrEmpty(formatData))
                    {
                        if (RegEx.CreateFromString(formatData).IsValid())
                            break;
                        MainViewModel.EventLogger
                            .LogMessage(
                                $"SettingsManagerVisibleAttribute: ERROR Invalid RegEx pattern on property/field \"{propertyName}\"! (using default instead)");
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
                        MainViewModel.EventLogger
                            .LogMessage(
                                $"SettingsManagerVisibleAttribute: ERROR Invalid format for range on property/field \"{propertyName}\"! (using default instead)");
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
        }

        public TreeItem.TreeCellMode CellMode { get; private set; }

        public string FormatData { get; private set; }
    }
}
