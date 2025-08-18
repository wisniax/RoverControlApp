using Godot;

namespace RoverControlApp.MVVM.ViewModel;

public partial class WidgetManager : PanelContainer
{
	private const int scrollSpeed = 30;
	private const double scrollDelayLimit = 0.3;

	private double scroll_delay = 0;
	private bool scroll_up = false;
	private bool scroll_down = false;

	[ExportGroup(".internal", "_")]
	[Export]
	private ScrollContainer _scrollContainer = null!;

	public void ScrollUp_Begin() => scroll_up = true;
	public void ScrollUp_End() => scroll_up = false;
	public void ScrollDown_Begin() => scroll_down = true;
	public void ScrollDown_End() => scroll_down = false;

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		if (scroll_up | scroll_down)
		{
			scroll_delay += delta;
		}
		else
		{
			scroll_delay = 0;
		}

		if (scroll_delay > scrollDelayLimit)
		{
			scroll_delay -= scrollDelayLimit;
			if (scroll_up)
			{
				_scrollContainer.ScrollVertical -= scrollSpeed;
			}
			else if (scroll_down)
			{
				_scrollContainer.ScrollVertical += scrollSpeed;
			}
		}
	}
}
