using System.Collections.Generic;

using Godot;

namespace RoverControlApp.MVVM.ViewModel;

[Tool]
public partial class MaxSizeContainer : Container
{
	#region Fields

	private Vector2 _childMaxSize = Vector2.Inf;
	private Vector2 _minimalMargin = Vector2.Zero;

	private Vector2 _minumalSizeOfChild = Vector2.Zero;

	#endregion Fields

	#region Properties

	[Export]
	public Vector2 ChildMaxSize
	{
		get => _childMaxSize;
		set
		{
			_childMaxSize = value;
			if (IsInsideTree())
				CallDeferred(MethodName.OnMaxSizeUpdated);
		}
	}

	[Export]
	public Vector2 MinimalMargin
	{
		get => _minimalMargin;
		set
		{
			_minimalMargin = value;
			if (IsInsideTree())
				CallDeferred(MethodName.OnMaxSizeUpdated);
		}
	}

	#endregion Properties

	#region Godot

	public override void _Notification(int what)
	{
		switch ((long)what)
		{
			case NotificationChildOrderChanged:
				UpdateConfigurationWarnings();
				UpdateMinimumSize();
				break;
			case NotificationSortChildren when GetChildCount() == 1:
				var confinement = new Rect2(
					Vector2.Zero,
					_childMaxSize.Min(Size - MinimalMargin)
				);

				confinement.Size = confinement.Size.Max(GetCombinedMinimumSize());

				FitChildInRect(GetChild<Control>(0), confinement);

				break;
		}
	}

	public override int[] _GetAllowedSizeFlagsHorizontal() =>
		[(int)SizeFlags.ShrinkBegin];

	public override int[] _GetAllowedSizeFlagsVertical() =>
		[(int)SizeFlags.ExpandFill];

	public override string[] _GetConfigurationWarnings()
	{
		List<string> warns = [];

		var firstKiddo = GetChildOrNull<Control>(0);

		switch (GetChildCount())
		{
			case 0:
				break;
			case 1 when firstKiddo is null:
				warns.Add("Not a 'Control' node!");
				break;
			case 1:
				break;
			default:
				warns.Add("One child is expected!");
				break;

		}

		return [.. warns];
	}

    public override Vector2 _GetMinimumSize()
    {
		_minumalSizeOfChild = GetChildOrNull<Control>(0)?.GetCombinedMinimumSize() ?? Vector2.Zero;
        return _minumalSizeOfChild;
    }

	#endregion Godot

	#region Methods

	private void OnMaxSizeUpdated()
	{
		QueueSort();
	}

	#endregion Methods
}
