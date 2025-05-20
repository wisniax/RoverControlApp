using Godot;
using System;
using System.Linq;

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

	const float CENTER_ERROR = 20f;

	[Export]
	private WidgetDragControl widgetDragControl = null!;


	private bool _resizeStarted = false;
	private Vector2 _mouseMoveStart = Vector2.Zero;
	private Rect2 _resizeInitialRect = new Rect2();

	private LayoutPreset ClosestAnchorPoint(Rect2 rect)
	{
		float leftDistance = Mathf.Abs(rect.Position.X - GetRect().Position.X);
		float rightDistance = Mathf.Abs(rect.End.X - GetRect().End.X);

		float topDistance = Mathf.Abs(rect.Position.Y - GetRect().Position.Y);
		float bottomDistance = Mathf.Abs(rect.End.Y - GetRect().End.Y);

		EasyAnchor xPos, yPos;

		//xPos
		if (Mathf.IsEqualApprox(leftDistance, rightDistance, CENTER_ERROR))
			xPos = EasyAnchor.Center;
		else
			xPos = leftDistance < rightDistance ? EasyAnchor.Begin : EasyAnchor.End;

		//yPos
		if (Mathf.IsEqualApprox(topDistance, bottomDistance, CENTER_ERROR))
			yPos = EasyAnchor.Center;
		else
			yPos = topDistance < bottomDistance ? EasyAnchor.Begin : EasyAnchor.End;

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
	}

	public void OnResizeZone(InputEventMouseMotion eventMouseMotion, LayoutPreset layoutPreset)
	{
		if (!eventMouseMotion.ButtonMask.HasFlag(MouseButtonMask.Left))
		{
			_resizeStarted = false;
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

				GD.Print("deltaToMinimalSize:", deltaToMinimalSize.Y);
				GD.Print("mouseRelative:", -mouseRelative.Y);
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
		return new Rect2(-widgetDragControl.GetCombinedMinimumSize() / 2f, (GetParentOrNull<Control>()?.Size ?? GetViewportRect().Size) + widgetDragControl.GetCombinedMinimumSize());
	}

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		base._Ready();

		var kotwa = ClosestAnchorPoint(GetBoundingRect());
		SetAnchorsPreset(kotwa);

		GD.Print($"Closest Anchor point is: {kotwa}");
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
				MoveChild(widgetDragControl, -1);
				FitChildInRect(widgetDragControl, GetRect() with { Position = Vector2.Zero });

				foreach (var child in GetChildren().SkipLast(1).Cast<Control>())
					FitChildInRect(child, new Rect2(widgetDragControl.GetCombinedMinimumSize() / 2f, Size - widgetDragControl.GetCombinedMinimumSize()));

				break;
		}

		base._Notification(what);
	}

	public override Vector2 _GetMinimumSize()
	{
		Vector2 minimum = widgetDragControl.GetCombinedMinimumSize();

		foreach (var child in GetChildren().Cast<Control>().Except([widgetDragControl]))
		{
			minimum.X = Mathf.Max(minimum.X, child.GetCombinedMinimumSize().X + widgetDragControl.GetCombinedMinimumSize().X);
			minimum.Y = Mathf.Max(minimum.Y, child.GetCombinedMinimumSize().Y + widgetDragControl.GetCombinedMinimumSize().Y);
		}

		return minimum;
	}

}
