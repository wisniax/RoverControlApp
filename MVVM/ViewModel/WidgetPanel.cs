using System;
using System.Collections.Generic;
using System.Linq;

using Godot;

namespace RoverControlApp.MVVM.ViewModel;

public partial class WidgetPanel : Container
{
	#region Classes
	public struct WidgetPos
	{
		/// <summary>
		/// Target position for widget.
		/// </summary>
		public Vector2 Position { get; set; }
		/// <summary>
		/// Target size for widget.
		/// </summary>
		public Vector2 Size { get; set; }

		/// <summary>
		/// Anchor point.
		/// </summary>
		public LayoutPreset AnchorPoint { get; set; }
	}

	public enum EasyAnchor
	{
		Begin = 0,
		End = 1,
		Center = 2
	}

	#endregion Classes
	#region Fields

	private const float CENTER_ERROR = 30f;

	private bool _resizeStarted = false;
	private Vector2 _mouseMoveStart = Vector2.Zero;
	private Rect2 _resizeInitialRect = new Rect2();

	private bool _showVisuals = true;
	private bool _processDrag = true;
	private bool _processResize = true;
	private bool _windowBarEnabled = false;
	private bool _editMode = false;

	private string _windowBarTitle = "";

	private LayoutPreset _lastAppliedLayout = LayoutPreset.TopLeft;

	private List<Control> _untouchableBySortChildren = [];

	[ExportGroup(".internal", "_")]
	[Export]
	private WidgetDragControl _widgetDragControl = null!;

	[Export]
	private PanelContainer _windowBar = null!;

	[Export]
	private Panel _editInfo = null!;

	[Export]
	private Label _leftDistanceLabel = null!;

	[Export]
	private Label _topDistanceLabel = null!;

	[Export]
	private Label _rightDistanceLabel = null!;

	[Export]
	private Label _bottomDistanceLabel = null!;

	[Export]
	private Label _anchorPointLabel = null!;

	[Export]
	private Label _windowBarTitleLabel = null!;

	[Export]
	private Panel _windowBorder = null!;

	#endregion Fields
	#region Properties

	[Export]
	public bool ShowVisuals
	{
		get
		{
			return _showVisuals;
		}
		set
		{
			_showVisuals = value;
			if (IsInsideTree())
			{
				CallDeferred(MethodName.EditModeInternal, _editMode);
			}
		}
	}

	[Export]
	public bool ProcessDrag
	{
		get
		{
			if (IsInsideTree())
				_processDrag = _widgetDragControl.ProcessDrag;
			return _processDrag;
		}
		set
		{
			_processDrag = value;
			if (IsInsideTree())
			{
				_widgetDragControl.SetDeferred(PropertyName.ProcessDrag, _processDrag);
			}
		}
	}

	[Export]
	public bool ProcessResize
	{
		get
		{
			if (IsInsideTree())
				_processResize = _widgetDragControl.ProcessResize;
			return _processResize;
		}
		set
		{
			_processResize = value;
			if (IsInsideTree())
			{
				_widgetDragControl.SetDeferred(PropertyName.ProcessResize, _processResize);
			}
		}
	}

	[Export]
	public bool WindowBarEnabled
	{
		get => _windowBarEnabled;
		set
		{
			_windowBarEnabled = value;
			if (IsInsideTree())
			{
				CallDeferred(MethodName.WindowBarEnabledInternal, _windowBarEnabled);
			}
		}
	}

	[Export]
	public bool EditMode
	{
		get => _editMode;
		set
		{
			_editMode = value;
			if (IsInsideTree())
			{
				CallDeferred(MethodName.EditModeInternal, _editMode);
			}
		}
	}

	[Export]
	public string WindowBarTitle
	{
		get => _windowBarTitle;
		set
		{
			_windowBarTitle = value;
			if (IsInsideTree())
			{
				CallDeferred(MethodName.WindowBarTitleInternal, _windowBarTitle);
			}
		}
	}

	#endregion Properties
	#region Methods.Helpers

	private Vector4 DistanceToSides(Rect2 rect)
	{
		float leftDistance = Mathf.Abs(rect.Position.X - GetRect().Position.X);
		float rightDistance = Mathf.Abs(rect.End.X - GetRect().End.X);

		float topDistance = Mathf.Abs(rect.Position.Y - GetRect().Position.Y);
		float bottomDistance = Mathf.Abs(rect.End.Y - GetRect().End.Y);

		return new(leftDistance, topDistance, rightDistance, bottomDistance);
	}

	private LayoutPreset ClosestAnchorPoint(Rect2 rect)
	{
		var distances = DistanceToSides(rect);

		EasyAnchor xPos, yPos;

		//xPos
		if (Mathf.IsEqualApprox(distances.X, distances.Z, CENTER_ERROR))
			xPos = EasyAnchor.Center;
		else
			xPos = distances.X < distances.Z ? EasyAnchor.Begin : EasyAnchor.End;

		//yPos
		if (Mathf.IsEqualApprox(distances.Y, distances.W, CENTER_ERROR))
			yPos = EasyAnchor.Center;
		else
			yPos = distances.Y < distances.W ? EasyAnchor.Begin : EasyAnchor.End;

		return (xPos, yPos) switch
		{
			// xPos = Begin (0)
			(EasyAnchor.Begin, EasyAnchor.Begin) => LayoutPreset.TopLeft,
			(EasyAnchor.Begin, EasyAnchor.End) => LayoutPreset.BottomLeft,
			(EasyAnchor.Begin, EasyAnchor.Center) => LayoutPreset.CenterLeft,

			// xPos = End (1)
			(EasyAnchor.End, EasyAnchor.Begin) => LayoutPreset.TopRight,
			(EasyAnchor.End, EasyAnchor.End) => LayoutPreset.BottomRight,
			(EasyAnchor.End, EasyAnchor.Center) => LayoutPreset.CenterRight,

			// xPos = Center (2)
			(EasyAnchor.Center, EasyAnchor.Begin) => LayoutPreset.CenterTop,
			(EasyAnchor.Center, EasyAnchor.End) => LayoutPreset.CenterBottom,
			(EasyAnchor.Center, EasyAnchor.Center) => LayoutPreset.Center,

			//Some possible case by Linter.
			_ => throw new NotImplementedException()
		};

	}

	private void WindowBarEnabledInternal(bool enabled)
	{
		_windowBar.Visible = enabled;
		_windowBorder.Visible = enabled;

		QueueSort();
		CallDeferred(MethodName.UpdateEditInfo);
	}

	private void EditModeInternal(bool enabled)
	{
		_widgetDragControl.Visible = enabled;
		_widgetDragControl.ShowVisuals = enabled && _showVisuals;
		_editInfo.Visible = enabled && _showVisuals;
		_windowBar.MouseDefaultCursorShape = enabled ? CursorShape.Drag : CursorShape.Arrow;

		UpdateEditInfo();
	}

	private void UpdateEditInfo()
	{
		if (!EditMode)
			return;
		var distances = DistanceToSides(GetBoundingRect());

		_leftDistanceLabel.Text = distances.X.ToString();
		_topDistanceLabel.Text = distances.Y.ToString();
		_rightDistanceLabel.Text = distances.Z.ToString();
		_bottomDistanceLabel.Text = distances.W.ToString();
		_anchorPointLabel.Text = $"Sticks to {ClosestAnchorPoint(GetBoundingRect())}\nOffset ({distances.X - distances.Z},{distances.Y - distances.W})\nSize ({Size.X},{Size.Y})";
	}

	private void WindowBarTitleInternal(string name)
	{
		_windowBarTitleLabel.Text = name;
	}

	private void OnButtonExit()
	{
		Visible = false;
	}

	public Rect2 GetBoundingRect()
	{
		return new Rect2(-_widgetDragControl.GetCombinedMinimumSize() / 2f, (GetParentOrNull<Control>()?.Size ?? GetViewportRect().Size) + _widgetDragControl.GetCombinedMinimumSize());
	}

	public WidgetPos SaveWidget()
	{
		return new WidgetPos() { Position = Position, Size = Size, AnchorPoint = _lastAppliedLayout };
	}

	public void LoadWidget(WidgetPos data)
	{
		Position = data.Position;
		Size = data.Size;
		_lastAppliedLayout = ClosestAnchorPoint(GetBoundingRect());
		SetAnchorsPreset(_lastAppliedLayout);
	}

	#endregion Methods.Helpers
	#region Methods.DragNSize

	public void OnDragWindowBar(InputEvent @event)
	{
		if (@event is InputEventMouseMotion inputEventMouseMotion)
			OnDrag(inputEventMouseMotion);
	}

	public void OnDrag(InputEventMouseMotion eventMouseMotion)
	{
		if (!EditMode)
			return;
		if (!eventMouseMotion.ButtonMask.HasFlag(MouseButtonMask.Left))
		{
			_resizeStarted = false;
			_lastAppliedLayout = ClosestAnchorPoint(GetBoundingRect());
			SetAnchorsPreset(_lastAppliedLayout);
			return;
		}

		if (!_resizeStarted)
		{
			_resizeStarted = true;
			_mouseMoveStart = eventMouseMotion.GlobalPosition;
			_resizeInitialRect.Size = Size;
			_resizeInitialRect.Position = Position;
		}

		Vector2 mouseRelativeToStart = eventMouseMotion.GlobalPosition - _mouseMoveStart;

		Rect2 parentRect = GetBoundingRect();

		if (eventMouseMotion.GetModifiersMask().HasFlag(KeyModifierMask.MaskAlt))
		{
			Position += eventMouseMotion.Relative * 0.25f;
			_mouseMoveStart = eventMouseMotion.GlobalPosition;
			_resizeInitialRect.Position = Position;
		}
		else
			Position = _resizeInitialRect.Position + mouseRelativeToStart;

		//clip at Left
		if (Position.X < parentRect.Position.X)
		{
			Position = Position with { X = parentRect.Position.X };
		}

		//clip at Top
		if (Position.Y < parentRect.Position.Y)
		{
			Position = Position with { Y = parentRect.Position.Y };
		}

		//clip at Right
		if (GetRect().End.X > parentRect.End.X)
		{
			Position = Position with { X = parentRect.End.X - Size.X };
		}

		//clip at Bottom
		if (GetRect().End.Y > parentRect.End.Y)
		{
			Position = Position with { Y = parentRect.End.Y - Size.Y };
		}

		UpdateEditInfo();
	}

	public void OnResizeZone(InputEventMouseMotion eventMouseMotion, LayoutPreset layoutPreset)
	{
		if (!EditMode)
			return;
		if (!eventMouseMotion.ButtonMask.HasFlag(MouseButtonMask.Left))
		{
			_resizeStarted = false;
			_lastAppliedLayout = ClosestAnchorPoint(GetBoundingRect());
			SetAnchorsPreset(_lastAppliedLayout);
			return;
		}

		if (!_resizeStarted)
		{
			_resizeStarted = true;
			_mouseMoveStart = eventMouseMotion.GlobalPosition;
			_resizeInitialRect.Size = Size;
			_resizeInitialRect.Position = Position;
		}

		Vector2 mouseRelativeToStart = eventMouseMotion.GlobalPosition - _mouseMoveStart;

		Rect2 newRect;

		Rect2 parentRect = GetBoundingRect();


		if (eventMouseMotion.GetModifiersMask().HasFlag(KeyModifierMask.MaskCtrl))
			newRect = WidgetStatic.GetResizedRectCenter(
				_resizeInitialRect,
				GetCombinedMinimumSize(),
				layoutPreset,
				mouseRelativeToStart
			);
		else
			newRect = WidgetStatic.GetResizedRectNormal(
				_resizeInitialRect,
				GetCombinedMinimumSize(),
				layoutPreset,
				mouseRelativeToStart
			);


		//clip at Left
		if (newRect.Position.X < parentRect.Position.X)
		{
			newRect.Size = newRect.Size with { X = newRect.Size.X + newRect.Position.X - parentRect.Position.X };
			newRect.Position = newRect.Position with { X = parentRect.Position.X };
		}

		//clip at Top
		if (newRect.Position.Y < parentRect.Position.Y)
		{
			newRect.Size = newRect.Size with { Y = newRect.Size.Y + newRect.Position.Y - parentRect.Position.Y };
			newRect.Position = newRect.Position with { Y = parentRect.Position.Y };
		}

		//clip at Right
		if (newRect.End.X > parentRect.End.X)
		{
			newRect.Size = newRect.Size with { X = parentRect.End.X - newRect.Position.X };
		}

		//clip at Bottom
		if (newRect.End.Y > parentRect.End.Y)
		{
			newRect.Size = newRect.Size with { Y = parentRect.End.Y - newRect.Position.Y };
		}

		Position = newRect.Position;
		Size = newRect.Size;

		UpdateEditInfo();
	}

	#endregion Methods.DragNSize
	#region Godot

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		ShowVisuals = _showVisuals;
		ProcessDrag = _processDrag;
		ProcessResize = _processResize;
		WindowBarEnabled = _windowBarEnabled;
		EditMode = _editMode;
		WindowBarTitle = _windowBarTitle;
		_untouchableBySortChildren = [_widgetDragControl, _windowBar, _editInfo];
	}

	public override void _Notification(int what)
	{
		switch ((long)what)
		{
			case NotificationResized:
				return;
			case NotificationChildOrderChanged:
				UpdateMinimumSize();
				break;
			case NotificationSortChildren:
				if (!IsNodeReady())
					return;
				Vector2 windowBarOffset = Vector2.Zero;

				if (_windowBarEnabled)
					windowBarOffset.Y = _windowBar.GetCombinedMinimumSize().Y;

				Rect2 childBoundingBox = new(
					(_widgetDragControl.GetCombinedMinimumSize() / 2f) + windowBarOffset,
					Size - _widgetDragControl.GetCombinedMinimumSize() - windowBarOffset
				);

				MoveChild(_windowBorder, 0);

				MoveChild(_editInfo, -1);
				FitChildInRect(_editInfo, new Rect2(childBoundingBox.Position - windowBarOffset, childBoundingBox.Size + windowBarOffset));

				MoveChild(_widgetDragControl, -2);
				FitChildInRect(_widgetDragControl, GetRect() with { Position = Vector2.Zero });

				MoveChild(_windowBar, -3);
				FitChildInRect(_windowBar, new Rect2(childBoundingBox.Position - windowBarOffset, new Vector2(childBoundingBox.Size.X, windowBarOffset.Y)));


				foreach (var child in GetChildren().Cast<Control>().Except(_untouchableBySortChildren))
				{
					FitChildInRect(child, childBoundingBox);
				}

				break;
		}

		base._Notification(what);
	}

	public override Vector2 _GetMinimumSize()
	{
		Vector2 minimum = _widgetDragControl.GetCombinedMinimumSize();

		foreach (var child in GetChildren().Cast<Control>().Except(_untouchableBySortChildren))
		{
			minimum = minimum.Max(child.GetCombinedMinimumSize() + _widgetDragControl.GetCombinedMinimumSize());
		}

		if (_windowBarEnabled)
		{
			minimum.X = Mathf.Max(minimum.X, _windowBar.GetCombinedMinimumSize().X);
			minimum.Y += _windowBar.GetCombinedMinimumSize().Y;
		}

		UpdateEditInfo();
		return minimum;
	}

	#endregion Godot

}
