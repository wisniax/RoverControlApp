using Godot;
using RoverControlApp.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Globalization;
using System.Linq;
using System.Reflection;

namespace RoverControlApp.MVVM.ViewModel;

public partial class SettingsManagerTree : Tree
{
	private static readonly int COLUMN_NAME = 0;
	private static readonly int COLUMN_VALUE = 1;


	private List<object> _middleObjects;
	private object _target;

	[Signal]
	public delegate void ReconstructNeededEventHandler();

	void ConstructItemEdit(TreeItem parentTab, SettingsManagerVisibleAttribute attribute, string internalName, object value)
	{
		var columnHost = parentTab.CreateChild();
		columnHost.SetText(COLUMN_NAME, attribute.CustomName);
		columnHost.SetMetadata(COLUMN_NAME, internalName);
		columnHost.SetTooltipText(COLUMN_NAME, attribute.CustomTooltip);

		switch (attribute.CellMode)
		{
			case TreeItem.TreeCellMode.String:
				columnHost.SetCellMode(COLUMN_VALUE, TreeItem.TreeCellMode.String);
				columnHost.SetEditable(COLUMN_VALUE, true);

				columnHost.SetText(COLUMN_VALUE, (string)value);
				columnHost.SetMetadata(COLUMN_VALUE, attribute.FormatData);
				if (!string.IsNullOrEmpty(attribute.FormatData))
					columnHost.SetTooltipText(COLUMN_VALUE, "RegEx pattern: " + attribute.FormatData);

				break;
			case TreeItem.TreeCellMode.Check:
				columnHost.SetCellMode(COLUMN_VALUE, TreeItem.TreeCellMode.Check);
				columnHost.SetChecked(COLUMN_VALUE, (bool)value);
				columnHost.SetEditable(COLUMN_VALUE, true);

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
				columnHost.SetEditable(COLUMN_VALUE, true);

				columnHost.SetRange(COLUMN_VALUE, Convert.ToDouble(value));
				columnHost.SetMetadata(COLUMN_VALUE, attribute.FormatData);
				break;
			default:
				throw new NotImplementedException();
		}
	}

	private void ConstructScene(object basedOn)
	{
		_middleObjects = new();
		Clear();

		if (basedOn is null)
		{
			TryUpdateStatusBar($"[color=khaki]Nothing to show[/color]");
			return;
		}

		HideRoot = true;
		Columns = 2;

		var rootItem = CreateItem();
		rootItem.DisableFolding = true;
		rootItem.SetText(COLUMN_NAME, "I am gROOT");

		ConstructTab(rootItem, basedOn);
		TryUpdateStatusBar($"[color=lightgreen]Ready[/color]");
	}

	private void ConstructTab(TreeItem parentTab, object basedOn)
	{
		Type basedOnType = basedOn.GetType();

		var searchList = new List<MemberInfo>();
		searchList.AddRange(basedOnType.GetProperties());
		searchList.AddRange(basedOnType.GetFields());

		foreach (var member in searchList)
		{
			if (member.GetCustomAttribute<SettingsManagerVisibleAttribute>()
				is not SettingsManagerVisibleAttribute memberAttribute)
				continue;

			//fetch object
			object memberObject;
			if (member.MemberType == MemberTypes.Property)
				memberObject = basedOnType.GetProperty(member.Name).GetValue(basedOn);
			else
				memberObject = basedOnType.GetField(member.Name).GetValue(basedOn);

			//if not class create editable item for it
			//See SettingsManagerVisibleAttribute..ctor summary
			if (memberAttribute.CellMode != TreeItem.TreeCellMode.Custom)
			{
				ConstructItemEdit(parentTab, memberAttribute, member.Name, memberObject);
				continue;
			}

			//if memberObject is class, store reference to it. 
			//tt's a surprise tool that will help us later
			if (!_middleObjects.Contains(memberObject))
				_middleObjects.Add(memberObject);

			//creation of new tab
			var treeItem = parentTab.CreateChild();
			treeItem.SetMetadata(COLUMN_NAME, _middleObjects.IndexOf(memberObject));
			treeItem.SetText(COLUMN_NAME, memberAttribute.CustomName);
			treeItem.SetTooltipText(COLUMN_NAME, memberAttribute.CustomTooltip);
			ConstructTab(treeItem, memberObject);
		}
	}

	private void ItemEditedSelfSubscriber()
	{
		var itemEdited = GetSelected();
		var itemParent = itemEdited.GetParent();

		string editedVarName = itemEdited.GetMetadata(COLUMN_NAME).AsString();
		object editedObj = _middleObjects[itemParent.GetMetadata(COLUMN_NAME).AsInt32()];
		Type editedType = editedObj.GetType();

		Action<object, object> ValueSetter;
		Func<object, object> ValueGetter;

		var editedVarTypeInfo = editedType.GetMember(editedVarName)[0];
		if (editedVarTypeInfo.MemberType == MemberTypes.Property)
		{
			ValueSetter = editedType.GetProperty(editedVarName).SetValue;
			ValueGetter = editedType.GetProperty(editedVarName).GetValue;
		}
		else
		{
			ValueSetter = editedType.GetField(editedVarName).SetValue;
			ValueGetter = editedType.GetField(editedVarName).GetValue;
		}

		switch (itemEdited.GetCellMode(COLUMN_VALUE))
		{
			case TreeItem.TreeCellMode.String:
				if (!string.IsNullOrEmpty(itemEdited.GetMetadata(COLUMN_VALUE).AsString()))
				{
					var tested = itemEdited.GetText(COLUMN_VALUE);
					var tester = RegEx.CreateFromString(itemEdited.GetMetadata(COLUMN_VALUE).ToString());
					var test = tester.Search(tested);
					if (test is null || test.GetStart() != 0 || test.GetEnd() != tested.Length)
					{
						MainViewModel.EventLogger
							.LogMessage($"SettingsManager: INFO RegEx match failed for property/field \"{itemEdited.GetMetadata(COLUMN_NAME).AsString()}\"");
						itemEdited.SetText(COLUMN_VALUE, (string)ValueGetter(editedObj));
						TryUpdateStatusBar($"[color=orange]\"{itemEdited.GetText(COLUMN_NAME)}\" RegEx match failed![/color]");
						break;
					}
				}
				TryUpdateStatusBar($"[color=Greenyellow]\"{itemEdited.GetText(COLUMN_NAME)}\" edited! \n({ValueGetter(editedObj)} -> {itemEdited.GetText(COLUMN_VALUE)})[/color]");
				ValueSetter(editedObj, itemEdited.GetText(COLUMN_VALUE));

				break;
			case TreeItem.TreeCellMode.Check:
				TryUpdateStatusBar($"[color=Greenyellow]\"{itemEdited.GetText(COLUMN_NAME)}\" edited! \n({(bool)ValueGetter(editedObj)} -> {itemEdited.IsChecked(COLUMN_VALUE)})[/color]");
				ValueSetter(editedObj, itemEdited.IsChecked(COLUMN_VALUE));
				break;
			case TreeItem.TreeCellMode.Range:
				try //to think programmer isn't stupid
				{

					var typeLiteral = itemEdited.GetMetadata(COLUMN_VALUE).AsString().Split(';')[^1];
					switch (typeLiteral)
					{
						case "i":
							TryUpdateStatusBar($"[color=Greenyellow]\"{itemEdited.GetText(COLUMN_NAME)}\" edited! \n({Convert.ToInt32(ValueGetter(editedObj))} -> {Convert.ToInt32(itemEdited.GetRange(COLUMN_VALUE))})[/color]");
							ValueSetter(editedObj, Convert.ToInt32(itemEdited.GetRange(COLUMN_VALUE)));
							break;
						case "ui":
							TryUpdateStatusBar($"[color=Greenyellow]\"{itemEdited.GetText(COLUMN_NAME)}\" edited! \n({Convert.ToUInt32(ValueGetter(editedObj))} -> {Convert.ToUInt32(itemEdited.GetRange(COLUMN_VALUE))})[/color]");
							ValueSetter(editedObj, Convert.ToUInt32(itemEdited.GetRange(COLUMN_VALUE)));
							break;
						case "l":
							TryUpdateStatusBar($"[color=Greenyellow]\"{itemEdited.GetText(COLUMN_NAME)}\" edited! \n({Convert.ToInt64(ValueGetter(editedObj))} -> {Convert.ToInt64(itemEdited.GetRange(COLUMN_VALUE))})[/color]");
							ValueSetter(editedObj, Convert.ToInt64(itemEdited.GetRange(COLUMN_VALUE)));
							break;
						case "ul":
							TryUpdateStatusBar($"[color=Greenyellow]\"{itemEdited.GetText(COLUMN_NAME)}\" edited! \n({Convert.ToUInt64(ValueGetter(editedObj))} -> {Convert.ToUInt64(itemEdited.GetRange(COLUMN_VALUE))})[/color]");
							ValueSetter(editedObj, Convert.ToUInt64(itemEdited.GetRange(COLUMN_VALUE)));
							break;
						case "f":
							TryUpdateStatusBar($"[color=Greenyellow]\"{itemEdited.GetText(COLUMN_NAME)}\" edited! \n({Convert.ToSingle(ValueGetter(editedObj))} -> {Convert.ToSingle(itemEdited.GetRange(COLUMN_VALUE))})[/color]");
							ValueSetter(editedObj, Convert.ToSingle(itemEdited.GetRange(COLUMN_VALUE)));
							break;
						case "d":
							TryUpdateStatusBar($"[color=Greenyellow]\"{itemEdited.GetText(COLUMN_NAME)}\" edited! \n({Convert.ToDouble(ValueGetter(editedObj))} -> {Convert.ToDouble(itemEdited.GetRange(COLUMN_VALUE))})[/color]");
							ValueSetter(editedObj, itemEdited.GetRange(COLUMN_VALUE)); //cast from double to double is stupid ok?
							break;
						case "m":
							TryUpdateStatusBar($"[color=Greenyellow]\"{itemEdited.GetText(COLUMN_NAME)}\" edited! \n({Convert.ToDecimal(ValueGetter(editedObj))} -> {Convert.ToDecimal(itemEdited.GetRange(COLUMN_VALUE))})[/color]");
							ValueSetter(editedObj, Convert.ToDecimal(itemEdited.GetRange(COLUMN_VALUE)));
							break;
					}
				}
				catch (Exception _) //yourself...
				{
					MainViewModel.EventLogger
						.LogMessage(
							$"SettingsManager: ERROR Range type is incorrectly set for property/field \"{itemEdited.GetMetadata(COLUMN_NAME).AsString()}\" (Root class: {Target})");
					itemEdited.SetEditable(COLUMN_VALUE, false);
					itemEdited.SetRange(COLUMN_VALUE, Convert.ToDouble(ValueGetter(editedObj)));
					itemEdited.SetText(COLUMN_NAME, $"{itemEdited.GetMetadata(COLUMN_NAME).AsString()} # ERROR formatData.type is incorrectly set! #");
					TryUpdateStatusBar($"[color=red]RUNTIME ERROR: \"{itemEdited.GetMetadata(COLUMN_NAME).AsString()}\" formatData.type is incorrectly set!\nEditing is disabled.[/color]");
				}

				break;
		}
	}

	private void TryUpdateStatusBar(string withText)
	{
		if (StatusBar is null)
			return;
		StatusBar.Text = withText;
	}

	public override void _Ready()
	{
		ConstructScene(Target);
		Connect(SignalName.ReconstructNeeded, new Callable(this, MethodName.Reconstruct));
		Connect(SignalName.ItemEdited, new Callable(this, MethodName.ItemEditedSelfSubscriber));
	}

	public void Reconstruct() { ConstructScene(Target); }

	public RichTextLabel? StatusBar { get; set; }

	public object Target
	{
		get => _target;
		set
		{
			_target = value;
			EmitSignal(SignalName.ReconstructNeeded, null);
		}
	}
}
