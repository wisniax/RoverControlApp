using Godot;
using RoverControlApp.MVVM.Model;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;

namespace RoverControlApp.MVVM.ViewModel;

public partial class SettingsManager : Tree
{
	private static readonly int COLUMN_NAME = 0;
	private static readonly int COLUMN_VALUE = 1;


	private List<object> _middleObjects;
	private object _target;

	[Signal]
	public delegate void ReconstructNeededEventHandler();

	void ConstructColumn(TreeItem columnHost, SettingsManagerVisibleAttribute attribute, object value)
	{
		switch (attribute.CellMode)
		{
			case TreeItem.TreeCellMode.String:
				columnHost.SetCellMode(COLUMN_VALUE, TreeItem.TreeCellMode.String);
				columnHost.SetEditable(COLUMN_VALUE, true);

				columnHost.SetText(COLUMN_VALUE, (string)value);
				columnHost.SetMetadata(COLUMN_VALUE, attribute.FormatData);
				break;
			case TreeItem.TreeCellMode.Check:
				columnHost.SetCellMode(COLUMN_VALUE, TreeItem.TreeCellMode.Check);
				columnHost.SetChecked(COLUMN_VALUE, (bool)value);
				columnHost.SetEditable(COLUMN_VALUE	, true);

				columnHost.SetText(COLUMN_VALUE, "(Checkbox)");
				break;
			case TreeItem.TreeCellMode.Range:
				var splitedFormat = attribute.FormatData.Split(';');
				columnHost.SetCellMode(COLUMN_VALUE		, TreeItem.TreeCellMode.Range);
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

		HideRoot = true;
		Columns = 2;
		var rootItem = CreateItem();
		rootItem.DisableFolding = false;
		rootItem.SetText(COLUMN_NAME, "SettingsManager");
		ConstructTab(rootItem, basedOn);
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

			//object form member
			object memberObject;
			if (member.MemberType == MemberTypes.Property)
				memberObject = basedOnType.GetProperty(member.Name).GetValue(basedOn);
			else
				memberObject = basedOnType.GetField(member.Name).GetValue(basedOn);

			//adding line to tab
			if (memberAttribute.CellMode != TreeItem.TreeCellMode.Custom)
			{
				var treePItem = parentTab.CreateChild();
				treePItem.SetText(COLUMN_NAME, member.Name);
				treePItem.SetMetadata(COLUMN_NAME, member.Name);
				ConstructColumn(treePItem, memberAttribute, memberObject);
				continue;
			}

			//_middleObject update
			if (!_middleObjects.Contains(memberObject))
				_middleObjects.Add(memberObject);

			//creation of new tab
			var treeItem = parentTab.CreateChild();
			treeItem.SetMetadata(COLUMN_NAME, _middleObjects.IndexOf(memberObject));
			treeItem.SetText(COLUMN_NAME, member.Name);
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

		switch (itemEdited.GetCellMode(1))
		{
			case TreeItem.TreeCellMode.String:
				if (!string.IsNullOrEmpty(itemEdited.GetMetadata(COLUMN_VALUE).AsString()))
				{
					var tested = itemEdited.GetText(1);
					var tester = RegEx.CreateFromString(itemEdited.GetMetadata(COLUMN_VALUE).ToString());
					var test = tester.Search(tested);
					if (test is null || test.GetStart() != 0 || test.GetEnd() != tested.Length)
					{
						MainViewModel.EventLogger
							.LogMessage($"SettingsManager: INFO RegEx match failed for property/field \"{itemEdited.GetMetadata(COLUMN_NAME).AsString()}\"");
						itemEdited.SetText(COLUMN_VALUE, (string)ValueGetter(editedObj));
						break;
					}
				}

				ValueSetter(editedObj, itemEdited.GetText(COLUMN_VALUE));
				break;
			case TreeItem.TreeCellMode.Check:
				ValueSetter(editedObj, itemEdited.IsChecked(COLUMN_VALUE));
				break;
			case TreeItem.TreeCellMode.Range:

				//cast hell
				try
				{
					var typeLiteral = itemEdited.GetMetadata(1).AsString().Split(';')[^1];
					switch (typeLiteral)
					{
						case "i":
							ValueSetter(editedObj, (int)itemEdited.GetRange(COLUMN_VALUE));
							break;
						case "ui":
							ValueSetter(editedObj, (uint)itemEdited.GetRange(COLUMN_VALUE));
							break;
						case "l":
							ValueSetter(editedObj, (long)itemEdited.GetRange(COLUMN_VALUE));
							break;
						case "ul":
							ValueSetter(editedObj, (ulong)itemEdited.GetRange(COLUMN_VALUE));
							break;
						case "f":
							ValueSetter(editedObj, (float)itemEdited.GetRange(COLUMN_VALUE));
							break;
						case "d":
							ValueSetter(editedObj, itemEdited.GetRange(COLUMN_VALUE));
							break;
						case "m":
							ValueSetter(editedObj, (decimal)itemEdited.GetRange(COLUMN_VALUE));
							break;
					}
				}
				catch (Exception _)
				{
					MainViewModel.EventLogger
						.LogMessage(
							$"SettingsManager: ERROR Range type is incorrectly set for property/field \"{itemEdited.GetMetadata(COLUMN_NAME).AsString()}\" (Root class: {Target})");
					itemEdited.SetEditable(COLUMN_VALUE, false);
					itemEdited.SetRange(COLUMN_VALUE, (double)ValueGetter(editedObj));
					itemEdited.SetText(COLUMN_NAME, $"{itemEdited.GetMetadata(COLUMN_NAME).AsString()} # ERROR formatData.type is incorrectly set! #");
				}

				break;
		}
	}

	private void ReconstructNeededSubscriber() { ConstructScene(Target); }

	public override void _Ready()
	{
		//TODO make dependant on Target again
		MainViewModel.EventLogger = new();
		MainViewModel.Settings = new();
		Target = MainViewModel.Settings;

		ConstructScene(Target);
		Connect(SignalName.ReconstructNeeded, new Callable(this, MethodName.ReconstructNeededSubscriber));
		Connect(SignalName.ItemEdited, new Callable(this, MethodName.ItemEditedSelfSubscriber));

		GetParent().GetNode("SaveSettings").Connect(Button.SignalName.Pressed, new Callable(this, "SaveSettings"));
		GetParent().GetNode("ForceDefaultSettings").Connect(Button.SignalName.Pressed, new Callable(this, "ForceDefaultSettings"));
		GetParent().GetNode("LoadSettings").Connect(Button.SignalName.Pressed, new Callable(this, "LoadSettings"));
	}

	public void SaveSettings() { MainViewModel.Settings.SaveSettings(); }
	public void ForceDefaultSettings() { MainViewModel.Settings.ForceDefaultSettings(); ConstructScene(Target); }
	public void LoadSettings() { MainViewModel.Settings.LoadSettings(); ConstructScene(Target); }

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
