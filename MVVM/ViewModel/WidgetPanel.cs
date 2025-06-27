using System;
using System.Linq;

using Godot;

public partial class WidgetPanel : Container
{
	struct WidgetPos
	{
		private LayoutPreset _anchorPoint;

		/// <summary>
		/// Target position for widget.
		/// </summary>
		public Vector2 Position { get; set; }
		/// <summary>
		/// Target size for widget.
		/// </summary>
		public Vector2 Size { get; set; }

		/// <summary>
		/// Anchor point. Should be one of:<br/>
		/// <list type="bullet">
		/// 	<item><term><see cref="Godot.LayoutPreset.TopLeft"/></term></item>
		/// 	<item><term><see cref="Godot.LayoutPreset.TopRight"/></term></item>
		/// 	<item><term><see cref="Godot.LayoutPreset.BottomLeft"/></term></item>
		/// 	<item><term><see cref="Godot.LayoutPreset.BottomRight"/></term></item>
		/// 	<item><term><see cref="Godot.LayoutPreset.Center"/></term></item>
		/// </list>
		/// </summary>
		public LayoutPreset AnchorPoint
		{
			readonly get => _anchorPoint;
			set
			{
				switch (value)
				{
					case LayoutPreset.TopLeft:
					case LayoutPreset.TopRight:
					case LayoutPreset.BottomLeft:
					case LayoutPreset.BottomRight:
					case LayoutPreset.Center:
						_anchorPoint = value;
						break;
				}
			}
		}
	}

	enum EasyAnchor
	{
		Begin = 0,
		End = 1,
		Center = 2
	}

	const float CENTER_ERROR = 30f;

	private bool _resizeStarted = false;
	private Vector2 _mouseMoveStart = Vector2.Zero;
	private Rect2 _resizeInitialRect = new Rect2();

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

	private bool _showVisuals = true;
	private bool _processDrag = true;
	private bool _processResize = true;
	private bool _windowBarEnabled = false;
	private bool _editMode = false;

	[Export]
	public bool ShowVisuals
	{
		get
		{
			_showVisuals = _widgetDragControl.ShowVisuals;
			return _showVisuals;
		}

		set
		{
			_showVisuals = value;
			if (IsInsideTree())
			{
				_widgetDragControl.SetDeferred(PropertyName.ShowVisuals, _showVisuals);
			}
		}
	}

	[Export]
	public bool ProcessDrag
	{
		get
		{
			_showVisuals = _widgetDragControl.ProcessDrag;
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
			_showVisuals = _widgetDragControl.ProcessResize;
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

	public void OnDrag(InputEventMouseMotion eventMouseMotion)
	{
		if (!eventMouseMotion.ButtonMask.HasFlag(MouseButtonMask.Left))
		{
			_resizeStarted = false;
			SetAnchorsPreset(ClosestAnchorPoint(GetBoundingRect()));
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
		if (!eventMouseMotion.ButtonMask.HasFlag(MouseButtonMask.Left))
		{
			_resizeStarted = false;
			SetAnchorsPreset(ClosestAnchorPoint(GetBoundingRect()));
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
			newRect = GetResizedRectCenter(layoutPreset, mouseRelativeToStart);
		else
			newRect = GetResizedRectNormal(layoutPreset, mouseRelativeToStart);


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

	private Rect2 GetResizedRectNormal(LayoutPreset layoutPreset, Vector2 mouseRelative)
	{
		Rect2 newRect = _resizeInitialRect;
		Vector2 deltaToMinimalSize = GetCombinedMinimumSize() - _resizeInitialRect.Size;
		switch (layoutPreset)
		{
			case LayoutPreset.TopRight:
			case LayoutPreset.TopWide:
			case LayoutPreset.TopLeft:
				newRect = newRect.GrowSide(Side.Top, Mathf.Max(deltaToMinimalSize.Y, -mouseRelative.Y));
				break;
			case LayoutPreset.BottomLeft:
			case LayoutPreset.BottomWide:
			case LayoutPreset.BottomRight:
				newRect = newRect.GrowSide(Side.Bottom, Mathf.Max(deltaToMinimalSize.Y, mouseRelative.Y));
				break;
		}

		switch (layoutPreset)
		{
			case LayoutPreset.TopLeft:
			case LayoutPreset.LeftWide:
			case LayoutPreset.BottomLeft:
				newRect = newRect.GrowSide(Side.Left, Mathf.Max(deltaToMinimalSize.X, -mouseRelative.X));
				break;
			case LayoutPreset.TopRight:
			case LayoutPreset.RightWide:
			case LayoutPreset.BottomRight:
				newRect = newRect.GrowSide(Side.Right, Mathf.Max(deltaToMinimalSize.X, mouseRelative.X));
				break;
		}



		return newRect;
	}

	private Rect2 GetResizedRectCenter(LayoutPreset layoutPreset, Vector2 mouseRelative)
	{
		Rect2 newRect = _resizeInitialRect;
		Vector2 deltaToMinimalSize = GetCombinedMinimumSize() - _resizeInitialRect.Size;

		//mouseRelative *= 0.5f;
		deltaToMinimalSize *= 0.5f;

		switch (layoutPreset)
		{
			case LayoutPreset.TopRight:
			case LayoutPreset.TopWide:
			case LayoutPreset.TopLeft:
				newRect = newRect.GrowSide(Side.Top, Mathf.Max(deltaToMinimalSize.Y, -mouseRelative.Y));
				newRect = newRect.GrowSide(Side.Bottom, Mathf.Max(deltaToMinimalSize.Y, -mouseRelative.Y));
				break;
			case LayoutPreset.BottomLeft:
			case LayoutPreset.BottomWide:
			case LayoutPreset.BottomRight:
				newRect = newRect.GrowSide(Side.Bottom, Mathf.Max(deltaToMinimalSize.Y, mouseRelative.Y));
				newRect = newRect.GrowSide(Side.Top, Mathf.Max(deltaToMinimalSize.Y, mouseRelative.Y));
				break;
		}

		switch (layoutPreset)
		{
			case LayoutPreset.TopLeft:
			case LayoutPreset.LeftWide:
			case LayoutPreset.BottomLeft:
				newRect = newRect.GrowSide(Side.Left, Mathf.Max(deltaToMinimalSize.X, -mouseRelative.X));
				newRect = newRect.GrowSide(Side.Right, Mathf.Max(deltaToMinimalSize.X, -mouseRelative.X));

				break;
			case LayoutPreset.TopRight:
			case LayoutPreset.RightWide:
			case LayoutPreset.BottomRight:
				newRect = newRect.GrowSide(Side.Right, Mathf.Max(deltaToMinimalSize.X, mouseRelative.X));
				newRect = newRect.GrowSide(Side.Left, Mathf.Max(deltaToMinimalSize.X, mouseRelative.X));

				break;
		}

		return newRect;
	}

	public Rect2 GetBoundingRect()
	{
		return new Rect2(-_widgetDragControl.GetCombinedMinimumSize() / 2f, (GetParentOrNull<Control>()?.Size ?? GetViewportRect().Size) + _widgetDragControl.GetCombinedMinimumSize());
	}

	private void WindowBarEnabledInternal(bool enabled)
	{
		_windowBar.Visible = enabled;

		QueueSort();
		CallDeferred(MethodName.UpdateEditInfo);
	}

	private void EditModeInternal(bool enabled)
	{
		_widgetDragControl.Visible = enabled;
		_editInfo.Visible = enabled;
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

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		ShowVisuals = _showVisuals;
		ProcessDrag = _processDrag;
		ProcessResize = _processResize;
		WindowBarEnabled = _windowBarEnabled;
		EditMode = _editMode;
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		base._Process(delta);
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

				MoveChild(_editInfo, -1);
				FitChildInRect(_editInfo, new Rect2(childBoundingBox.Position - windowBarOffset, childBoundingBox.Size + windowBarOffset));

				MoveChild(_widgetDragControl, -2);
				FitChildInRect(_widgetDragControl, GetRect() with { Position = Vector2.Zero });

				MoveChild(_windowBar, -3);
				FitChildInRect(_windowBar, new Rect2(childBoundingBox.Position - windowBarOffset, new Vector2(childBoundingBox.Size.X, windowBarOffset.Y)));

				foreach (var child in GetChildren().Cast<Control>().Except([_widgetDragControl, _windowBar, _editInfo]))
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

		foreach (var child in GetChildren().Cast<Control>().Except([_widgetDragControl]))
		{
			minimum.X = Mathf.Max(minimum.X, child.GetCombinedMinimumSize().X + _widgetDragControl.GetCombinedMinimumSize().X);
			minimum.Y = Mathf.Max(minimum.Y, child.GetCombinedMinimumSize().Y + _widgetDragControl.GetCombinedMinimumSize().Y);
		}

		if (_windowBarEnabled)
			minimum.Y += _windowBar.GetCombinedMinimumSize().Y;

		return minimum;
	}

}
