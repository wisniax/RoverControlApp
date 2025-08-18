using Godot;

namespace RoverControlApp.MVVM.ViewModel;

public partial class WidgetManagerItemGrip : Label
{
	[ExportGroup(".internal", "_")]
	[Export]
	private WidgetManagerItem _dropDataProxy = null!;

	public override Variant _GetDragData(Vector2 atPosition)
	{
		return _dropDataProxy.GetDragData(atPosition);
	}
}
