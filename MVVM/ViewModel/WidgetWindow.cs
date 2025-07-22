using Godot;

namespace RoverControlApp.MVVM.ViewModel;

public partial class WidgetWindow : Window
{
	enum OptionList
	{
		Borderless = 2,
		Transparent = 3,
		AlwaysOnTop_None = 5,
		AlwaysOnTop_RCA = 6,
		AlwaysOnTop_All = 7,

	}

	public enum AOT_Mode
	{
		None = OptionList.AlwaysOnTop_None,
		RCA = OptionList.AlwaysOnTop_RCA,
		All = OptionList.AlwaysOnTop_All,

	}

	private AOT_Mode _alwaysOnTopMode = AOT_Mode.RCA;

	[ExportGroup(".internal", "_")]
	[Export]
	private Panel _noChildBg = null!;

	[Export]
	private PopupMenu _options = null!;

	private int BorderlessIdx => _options.GetItemIndex((int)OptionList.Borderless);
	private int TransparentIdx => _options.GetItemIndex((int)OptionList.Transparent);
	private int AlwaysOnTop_NoneIdx => _options.GetItemIndex((int)OptionList.AlwaysOnTop_None);
	private int AlwaysOnTop_RCAIdx => _options.GetItemIndex((int)OptionList.AlwaysOnTop_RCA);
	private int AlwaysOnTop_AllIdx => _options.GetItemIndex((int)OptionList.AlwaysOnTop_All);

	[Export]
	public AOT_Mode AlwaysOnTopMode
	{
		get => _alwaysOnTopMode;
		set
		{
			_alwaysOnTopMode = value;
			if (IsInsideTree())
			{
				CallDeferred(MethodName.ChangeAOT, (int)_alwaysOnTopMode);
			}
		}
	}

	public override void _Ready()
	{
		_noChildBg.Visible = GetChildCount() <= 2;
		_options.SetItemChecked(BorderlessIdx, Borderless);
		_options.SetItemChecked(TransparentIdx, Transparent);
		AlwaysOnTopMode = _alwaysOnTopMode;
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (@event is InputEventMouseButton mouseButton && mouseButton.IsActionPressed("widget_win_opt", allowEcho: false, exactMatch: true))
		{
			_options.Popup(new Rect2I((Vector2I)mouseButton.GlobalPosition + Position, Vector2I.Zero));
		}
	}

	public override void _Notification(int what)
	{
		if (what == NotificationChildOrderChanged && _noChildBg is not null)
		{
			_noChildBg.Visible = GetChildCount() <= 2;
		}
	}

	private void OnOptionsChange(int id_pressed)
	{
		switch (id_pressed)
		{
			case (int)OptionList.Borderless:
				Borderless = !_options.IsItemChecked(BorderlessIdx);
				_options.SetItemChecked(BorderlessIdx, Borderless);
				break;
			case (int)OptionList.Transparent:
				Transparent = !_options.IsItemChecked(TransparentIdx);
				_options.SetItemChecked(TransparentIdx, Transparent);
				break;
			case (int)OptionList.AlwaysOnTop_All:
			case (int)OptionList.AlwaysOnTop_RCA:
			case (int)OptionList.AlwaysOnTop_None:
				ChangeAOT((OptionList)id_pressed);
				break;
		}
	}

	private void ChangeAOT(OptionList desired)
	{
		_options.SetItemChecked(AlwaysOnTop_AllIdx, false);
		_options.SetItemChecked(AlwaysOnTop_RCAIdx, false);
		_options.SetItemChecked(AlwaysOnTop_NoneIdx, false);
		Transient = false;
		AlwaysOnTop = false;
		switch (desired)
		{
			case OptionList.AlwaysOnTop_All:
				Transient = false;
				AlwaysOnTop = true;
				_options.SetItemChecked(AlwaysOnTop_AllIdx, true);
				break;
			case OptionList.AlwaysOnTop_RCA:
				AlwaysOnTop = false;
				Transient = true;
				_options.SetItemChecked(AlwaysOnTop_RCAIdx, true);
				break;
			//case OptionList.AlwaysOnTop_None:
			default:
				Transient = false;
				AlwaysOnTop = false;
				_options.SetItemChecked(AlwaysOnTop_NoneIdx, true);
				break;
		}
	}
}
