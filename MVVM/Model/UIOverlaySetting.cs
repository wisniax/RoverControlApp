using Godot;

namespace RoverControlApp.MVVM.Model;

[GlobalClass]
public partial class UIOverlaySetting : Resource
{

	[Export(hint: PropertyHint.ColorNoAlpha)]
	public Color BackColor { get; set; } = Colors.DarkGreen;

	[Export(hint: PropertyHint.ColorNoAlpha)]
	public Color FontColor { get; set; } = Colors.LimeGreen;

	[Export]
	public bool UseFontAsBackColor { get; set; } = false;

	[Export]
	public string Text { get; set; } = "Text";

	public bool Equals(UIOverlaySetting? other)
	{
		if (other is null)
			return false;

		return BackColor == other.BackColor &&
		FontColor == other.FontColor &&
		Text == other.Text;
	}

	public override bool Equals(object? obj)
	{
		return Equals(obj as UIOverlaySetting);
	}

	public override int GetHashCode()
	{
		unchecked
		{
			int hash = 420;
			hash += BackColor.GetHashCode() * 69;
			hash += FontColor.GetHashCode() * 69;
			hash += Text.GetHashCode() * 69;
			return hash;
		}
	}
}
