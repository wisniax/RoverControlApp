using Godot;
using RoverControlApp.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Reflection;

namespace RoverControlApp.MVVM.ViewModel;

[System.AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, Inherited = true, AllowMultiple = false)]
public sealed class SettingsManagerVisibleAttribute : Attribute
{ }


public partial class SettingsManager : Tree
{
	Type GetMemberType(MemberInfo member)
	{
		switch (member.MemberType)
		{
			case MemberTypes.Field:
				return ((FieldInfo)member).FieldType;
			case MemberTypes.Property:
				return ((PropertyInfo)member).PropertyType;
			default:
				throw new InvalidProgramException();
		}
	}

	void ConstructScene()
	{
		var searchList = new List<MemberInfo>();
		searchList.AddRange(typeof(LocalSettings).GetFields());
		searchList.AddRange(typeof(LocalSettings).GetProperties());

		foreach (MemberInfo member in searchList)
		{
			if (member.GetCustomAttribute<SettingsManagerVisibleAttribute>() is null)
				continue;

			Type memberType = GetMemberType(member);

			//for primitive property types, add new tab named LocalSettings and put them here
			if (memberType.IsPrimitive || memberType == typeof(string))
			{
				var treePItem = GetRoot().CreateChild();
				treePItem.SetText(0, member.Name);
				treePItem.SetCellMode(1, TreeItem.TreeCellMode.Check);
				continue;
			}

			//for custom property type sniff around
			var treeItem = GetRoot().CreateChild();
			treeItem.SetText(0, member.Name);
			ConstructTab(member, treeItem);
		}
	}


	void ConstructTab(MemberInfo basedOn, TreeItem parentTab)
	{
		Type basedOnType = GetMemberType(basedOn);

		var searchList = new List<MemberInfo>();
		searchList.AddRange(basedOnType.GetProperties());
		searchList.AddRange(basedOnType.GetFields());

		foreach (var member in searchList)
		{
			if (member.GetCustomAttribute<SettingsManagerVisibleAttribute>() is null)
				continue;

			Type memberType = GetMemberType(member);

			if (memberType.IsPrimitive || memberType == typeof(string))
			{
				var treePItem = parentTab.CreateChild();
				treePItem.SetText(0, member.Name);
				continue;
			}


			var treeItem = parentTab.CreateChild();
			treeItem.SetText(0, member.Name.ToUpper());
			ConstructTab(member, treeItem);
		}
	}

	public override void _Ready()
	{
		HideRoot = false;
		Columns = 2;
		var item = CreateItem();
		item.DisableFolding = false;
		item.SetText(0, "SettingsManager");
		ConstructScene();
	}

}
