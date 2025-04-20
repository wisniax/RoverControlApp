using Godot;
using System;

public partial class WidgetPanel : Control
{
	struct WidgetPos
	{
		private LayoutPreset _anchorPoint;

		/// <summary>
		/// Target position for widget.
		/// </summary>
		public Vector2 Position {get; set;}
		/// <summary>
		/// Target size for widget.
		/// </summary>
		public Vector2 Size {get; set;}
		
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
				switch(value)
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

	private LayoutPreset ClosestAnchorPoint(Rect2 rect)
	{
		float leftDistance = rect.Position.X;
		float rightDistance = Size.X - (rect.Position.X + rect.Size.X);

		float topDistance = rect.Position.Y;
		float bottomDistance = Size.Y - (rect.Position.Y + rect.Size.Y);

		EasyAnchor xPos, yPos;

		//xPos
		if(Mathf.IsEqualApprox(leftDistance, rightDistance, CENTER_ERROR))
			xPos = EasyAnchor.Center;
		else
			xPos = leftDistance < rightDistance ? EasyAnchor.Begin : EasyAnchor.End;

		//yPos
		if(Mathf.IsEqualApprox(topDistance, bottomDistance, CENTER_ERROR))
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

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		base._Ready();
		//TODO remove dummy code
		var kiddo = GetChild<Control>(0);
		var kotwa = ClosestAnchorPoint(kiddo.GetRect());
		kiddo.SetAnchorsPreset(kotwa);
		GD.Print($"Closest Anchor point is: {kotwa}");
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		base._Process(delta);
	}

    public override void _Notification(int what)
    {
		switch((long)what)
		{
			case NotificationResized:
			return;
		}

		base._Notification(what);
    }

	
}
