using Godot;

using static Godot.Control;

namespace RoverControlApp.MVVM.ViewModel;

public static class WidgetStatic
{
	public static Rect2 GetResizedRectNormal(Rect2 currentRect, Vector2 minSize, LayoutPreset growDirection, Vector2 delta)
	{
		Vector2 deltaToMinimalSize = minSize - currentRect.Size;
		switch (growDirection)
		{
			case LayoutPreset.TopRight:
			case LayoutPreset.TopWide:
			case LayoutPreset.TopLeft:
				currentRect = currentRect.GrowSide(Side.Top, Mathf.Max(deltaToMinimalSize.Y, -delta.Y));
				break;
			case LayoutPreset.BottomLeft:
			case LayoutPreset.BottomWide:
			case LayoutPreset.BottomRight:
				currentRect = currentRect.GrowSide(Side.Bottom, Mathf.Max(deltaToMinimalSize.Y, delta.Y));
				break;
		}

		switch (growDirection)
		{
			case LayoutPreset.TopLeft:
			case LayoutPreset.LeftWide:
			case LayoutPreset.BottomLeft:
				currentRect = currentRect.GrowSide(Side.Left, Mathf.Max(deltaToMinimalSize.X, -delta.X));
				break;
			case LayoutPreset.TopRight:
			case LayoutPreset.RightWide:
			case LayoutPreset.BottomRight:
				currentRect = currentRect.GrowSide(Side.Right, Mathf.Max(deltaToMinimalSize.X, delta.X));
				break;
		}



		return currentRect;
	}

	public static Rect2 GetResizedRectCenter(Rect2 currentRect, Vector2 minSize, LayoutPreset growDirection, Vector2 delta)
	{
		Vector2 deltaToMinimalSize = minSize - currentRect.Size;

		//mouseRelative *= 0.5f;
		deltaToMinimalSize *= 0.5f;

		switch (growDirection)
		{
			case LayoutPreset.TopRight:
			case LayoutPreset.TopWide:
			case LayoutPreset.TopLeft:
				currentRect = currentRect.GrowSide(Side.Top, Mathf.Max(deltaToMinimalSize.Y, -delta.Y));
				currentRect = currentRect.GrowSide(Side.Bottom, Mathf.Max(deltaToMinimalSize.Y, -delta.Y));
				break;
			case LayoutPreset.BottomLeft:
			case LayoutPreset.BottomWide:
			case LayoutPreset.BottomRight:
				currentRect = currentRect.GrowSide(Side.Bottom, Mathf.Max(deltaToMinimalSize.Y, delta.Y));
				currentRect = currentRect.GrowSide(Side.Top, Mathf.Max(deltaToMinimalSize.Y, delta.Y));
				break;
		}

		switch (growDirection)
		{
			case LayoutPreset.TopLeft:
			case LayoutPreset.LeftWide:
			case LayoutPreset.BottomLeft:
				currentRect = currentRect.GrowSide(Side.Left, Mathf.Max(deltaToMinimalSize.X, -delta.X));
				currentRect = currentRect.GrowSide(Side.Right, Mathf.Max(deltaToMinimalSize.X, -delta.X));

				break;
			case LayoutPreset.TopRight:
			case LayoutPreset.RightWide:
			case LayoutPreset.BottomRight:
				currentRect = currentRect.GrowSide(Side.Right, Mathf.Max(deltaToMinimalSize.X, delta.X));
				currentRect = currentRect.GrowSide(Side.Left, Mathf.Max(deltaToMinimalSize.X, delta.X));

				break;
		}

		return currentRect;
	}
}
