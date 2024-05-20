using Godot;
using Godot.Bridge;
using RoverControlApp.Core;
using RoverControlApp.MVVM.Model;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;

namespace RoverControlApp.MVVM.ViewModel;

public partial class SettingsManagerTree : Tree
{
	private const int COLUMN_NAME = 0;
	private const int COLUMN_VALUE = 1;

	[Signal]
	public delegate void UpdateStatusBarEventHandler(string text);

	private void ConstructItemEdit(TreeItem parentTab, SettingsManagerVisibleAttribute attribute, string internalName, object value)
	{
		var columnHost = parentTab.CreateChild();
		columnHost.SetText(COLUMN_NAME, attribute.CustomName);
		columnHost.SetMetadata(COLUMN_NAME, internalName);
		columnHost.SetTooltipText(COLUMN_NAME, attribute.CustomTooltip);
		columnHost.SetSelectable(COLUMN_NAME, false);

		columnHost.SetEditable(COLUMN_VALUE, attribute.AllowEdit);
		columnHost.SetSelectable(COLUMN_VALUE, attribute.AllowEdit);
		if (!attribute.AllowEdit)
		{
			columnHost.SetCustomColor(COLUMN_VALUE, Colors.DimGray);
		}

		switch (attribute.CellMode)
		{
			case TreeItem.TreeCellMode.String:
				columnHost.SetCellMode(COLUMN_VALUE, TreeItem.TreeCellMode.String);
				columnHost.SetText(COLUMN_VALUE, (string)value);
				//columnHost.SetMetadata(COLUMN_VALUE, attribute.FormatData);
				if (!string.IsNullOrEmpty(attribute.FormatData))
					columnHost.SetTooltipText(COLUMN_VALUE, "RegEx pattern: " + attribute.FormatData);

				break;
			case TreeItem.TreeCellMode.Check:
				columnHost.SetCellMode(COLUMN_VALUE, TreeItem.TreeCellMode.Check);
				columnHost.SetChecked(COLUMN_VALUE, (bool)value);
				columnHost.SetText(COLUMN_VALUE, "(Checkbox)");
				break;
			case TreeItem.TreeCellMode.Range:
				var splitedFormat = attribute.FormatData.Split(';');
				columnHost.SetCellMode(COLUMN_VALUE, TreeItem.TreeCellMode.Range);
				columnHost.SetRangeConfig(
					COLUMN_VALUE,
					double.Parse(splitedFormat[0], CultureInfo.InvariantCulture),
					double.Parse(splitedFormat[1], CultureInfo.InvariantCulture),
					double.Parse(splitedFormat[2], CultureInfo.InvariantCulture),
					splitedFormat[3] == "t" || splitedFormat[3] == "T");
				columnHost.SetRange(COLUMN_VALUE, Convert.ToDouble(value));
				columnHost.SetMetadata(COLUMN_VALUE, attribute.FormatData);
				break;
			default:
				throw new NotImplementedException();
		}
	}

	// -------- COLUMN_NAME                                                COLUMN_VALUE
	// Text     Setting custom name                                        -
	// Metadata index of TargetObjectMirror stored in targetMembersClones. index of tab, sotred in tabCollapseStatus. 

	private void ConstructTab(TreeItem parentTab, object basedOn, TargetObjectMirror? parnetObjectMirror = null)
	{
		Type basedOnType = basedOn.GetType();

		foreach (var member in basedOnType.GetProperties())
		{
			if (member.GetCustomAttribute<SettingsManagerVisibleAttribute>() is not { } memberAttribute)
				continue;

			//fetch object
			object? memberObject = basedOnType.GetProperty(member.Name)!.GetValue(basedOn);

			if (memberObject is null)
			{
				EventLogger.LogMessage("SettingsManagerTree", EventLogger.LogLevel.Warning, $"SettingsManagerTree: WARNING Could not fetch member (parent:\"{basedOnType.Name}\")\"{member.Name}\" value");
				continue;
			}

			//if not class create editable item for it
			//See SettingsManagerVisibleAttribute..ctor summary
			if (memberAttribute.CellMode != TreeItem.TreeCellMode.Custom)
			{
				ConstructItemEdit(parentTab, memberAttribute, member.Name, memberObject);
				continue;
			}

			//if memberObject is class, store reference to it. 
			//it's a better surprise tool that will help us much more later
			TargetObjectMirror targetMemberClone = new(parnetObjectMirror ?? basedOn, member.GetValue(basedOn)!, member.Name, memberAttribute);
			targetMembersClones.Add(targetMemberClone);

			//creation of new tab
			var treeItem = parentTab.CreateChild();
			treeItem.SetMetadata(COLUMN_NAME, targetMembersClones.IndexOf(targetMemberClone));
			treeItem.SetMetadata(COLUMN_VALUE, tabIndexTracker++);
			treeItem.SetText(COLUMN_NAME, memberAttribute.CustomName);
			treeItem.SetTooltipText(COLUMN_NAME, memberAttribute.CustomTooltip);
			treeItem.SetCellMode(COLUMN_VALUE, TreeItem.TreeCellMode.Custom);
			treeItem.SetSelectable(COLUMN_NAME, false);
			treeItem.SetSelectable(COLUMN_VALUE, false);

			if(tabCollapseStatus.Count < tabIndexTracker)
			{
				tabCollapseStatus.Add(true);
			}

			treeItem.Collapsed = tabCollapseStatus[tabIndexTracker - 1];
			ConstructTab(treeItem, memberObject, targetMemberClone);
		}
	}

	private void ConstructScene(object basedOn)
	{
		if (basedOn is null)
		{
			TryUpdateStatusBar($"[color=khaki]Nothing to show[/color]");
			return;
		}

		if(IsConnected(SignalName.ItemCollapsed, Callable.From<TreeItem>(OnItemCollapsed)))
			Disconnect(SignalName.ItemCollapsed, Callable.From<TreeItem>(OnItemCollapsed));

		tabIndexTracker = 0;
		targetMembersClones.Clear();
		Clear();

		HideRoot = true;
		Columns = 2;

		var rootItem = CreateItem();
		rootItem.SetCellMode(COLUMN_VALUE, TreeItem.TreeCellMode.Custom);
		rootItem.DisableFolding = true;
		rootItem.SetText(COLUMN_NAME, "I am gROOT");

		ConstructTab(rootItem, basedOn);

		Connect(SignalName.ItemCollapsed, Callable.From<TreeItem>(OnItemCollapsed));
		TryUpdateStatusBar($"[color=lightgreen]Ready[/color]");
	}

	public override void _Ready()
	{
		Connect(Tree.SignalName.ItemEdited, new Callable(this, MethodName.ItemEditedSelfSubscriber));
	}

	private void TryUpdateStatusBar(string withText)
	{
		EmitSignal(SignalName.UpdateStatusBar, withText);
	}

	private void OnItemCollapsed(TreeItem treeItem)
	{
		tabCollapseStatus[treeItem.GetMetadata(COLUMN_VALUE).AsInt32()] = treeItem.Collapsed;
	}

	private void ItemEditedSelfSubscriber()
	{
		var itemEdited = GetSelected();
		var itemParent = itemEdited.GetParent();

		itemEdited.SetCustomBgColor(COLUMN_VALUE, Colors.Yellow, true);

		var targetMemberClone = targetMembersClones[itemParent.GetMetadata(COLUMN_NAME).AsInt32()];

		string editedVarName = itemEdited.GetMetadata(COLUMN_NAME).AsString();

		var oldValue = targetMemberClone.GetCloneValue(editedVarName);
		object newValue;

		switch (itemEdited.GetCellMode(COLUMN_VALUE))
		{
			case TreeItem.TreeCellMode.String:
				newValue = itemEdited.GetText(COLUMN_VALUE);

				if (newValue.Equals(oldValue))
				{
					TryUpdateStatusBar($"[color=Greenyellow]\"{editedVarName}\" edited but no changes made![/color]");
					break;
				}

				if (!targetMemberClone.VadilateValue(editedVarName, newValue))
				{
					EventLogger.LogMessage("SettingsManagerTree", EventLogger.LogLevel.Info, $"RegEx match failed for property \"{editedVarName}\"");
					itemEdited.SetText(COLUMN_VALUE, (string?)oldValue ?? "null");
					TryUpdateStatusBar($"[color=orange]\"{newValue}\" RegEx match failed![/color]");
					break;
				}
				targetMemberClone.SetCloneValue(editedVarName, newValue);
				TryUpdateStatusBar($"[color=Greenyellow]\"{editedVarName}\" edited! \n({(string?)oldValue ?? "null"} -> {(string)newValue})[/color]");
				break;
			case TreeItem.TreeCellMode.Check:
				newValue = itemEdited.IsChecked(COLUMN_VALUE);

				if (newValue.Equals(oldValue))
				{
					TryUpdateStatusBar($"[color=Greenyellow]\"{editedVarName}\" edited but no changes made![/color]");
					break;
				}

				if (!targetMemberClone.VadilateValue(editedVarName, newValue))
				{
					EventLogger.LogMessage("SettingsManagerTree", EventLogger.LogLevel.Error, $"Cannot update property \"{editedVarName}\"");
					itemEdited.SetChecked(COLUMN_VALUE, (bool?)oldValue ?? false);
					TryUpdateStatusBar($"[color=red]\"{editedVarName}\" Can not update for unknown reason! [/color]");
					break;
				}
				targetMemberClone.SetCloneValue(editedVarName, newValue);
				TryUpdateStatusBar($"[color=Greenyellow]\"{editedVarName}\" edited! \n({(bool?)oldValue ?? false} -> {(bool)newValue})[/color]");
				break;
			case TreeItem.TreeCellMode.Range:
				var typeLiteral = itemEdited.GetMetadata(COLUMN_VALUE).AsString().Split(';')[^1];
				//type must be same to satisfy validator
				switch (typeLiteral)
				{
					case "i":
						newValue = Convert.ToInt32(itemEdited.GetRange(COLUMN_VALUE));
						break;
					case "ui":
						newValue = Convert.ToUInt32(itemEdited.GetRange(COLUMN_VALUE));
						break;
					case "l":
						newValue = Convert.ToInt64(itemEdited.GetRange(COLUMN_VALUE));
						break;
					case "ul":
						newValue = Convert.ToUInt64(itemEdited.GetRange(COLUMN_VALUE));
						break;
					case "f":
						newValue = Convert.ToSingle(itemEdited.GetRange(COLUMN_VALUE));
						break;
					case "d":
						newValue = Convert.ToDouble(itemEdited.GetRange(COLUMN_VALUE));
						break;
					case "m":
						newValue = Convert.ToDecimal(itemEdited.GetRange(COLUMN_VALUE));
						break;
					default:
						throw new InvalidOperationException();
				}

				if (newValue.Equals(oldValue))
				{
					TryUpdateStatusBar($"[color=Greenyellow]\"{editedVarName}\" edited but no changes made![/color]");
					break;
				}

				if (!targetMemberClone.VadilateValue(editedVarName, newValue))
				{
					EventLogger.LogMessage("SettingsManagerTree", EventLogger.LogLevel.CriticalError, $"Range type is incorrectly set for property \"{editedVarName}\" (Class: {targetMemberClone.HoldingType.FullName})");
					itemEdited.SetEditable(COLUMN_VALUE, false);
					itemEdited.SetRange(COLUMN_VALUE, Convert.ToDouble(oldValue));
					itemEdited.SetText(COLUMN_NAME, $"{editedVarName} # ERROR formatData.type is incorrectly set! #");
					TryUpdateStatusBar($"[color=red]RUNTIME ERROR: \"{editedVarName}\" formatData.type is incorrectly set!\nEditing is disabled.[/color]");
				}

				targetMemberClone.SetCloneValue(editedVarName, newValue);
				TryUpdateStatusBar($"[color=Greenyellow]\"{itemEdited.GetText(COLUMN_NAME)}\" edited! \n({Convert.ToDecimal(oldValue).ToString(CultureInfo.InvariantCulture)} -> {Convert.ToDecimal(newValue).ToString(CultureInfo.InvariantCulture)})[/color]");
				break;
		}

		if (targetMemberClone.GetChangedProperties().Contains(editedVarName))
		{
			itemEdited.SetCustomBgColor(COLUMN_VALUE, new Color(Colors.Yellow), true);
		}
		else
		{
			itemEdited.SetCustomBgColor(COLUMN_VALUE, new Color(0, 0, 0, 0), true);
		}
	}

	public void Reconstruct() { ConstructScene(Target); }

	public void RevertSettings()
	{
		foreach (var member in targetMembersClones)
			member.Revert();
		Reconstruct();
	}

	public void ApplySettings()
	{
		foreach (var member in targetMembersClones.AsEnumerable().Reverse())
			member.Apply();
		Reconstruct();
	}

	private int tabIndexTracker = 0;

	public object Target { get; set; }

	//NOTE applying should be done from end to begin. (to make children propagate changes to parents)
	private readonly List<TargetObjectMirror> targetMembersClones = new();
	private readonly List<bool> tabCollapseStatus = new();
}