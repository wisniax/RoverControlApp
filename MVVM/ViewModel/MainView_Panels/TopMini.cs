namespace RoverControlApp.MVVM.ViewModel.MainView_Panel;

public partial class TopMini : TopPanelBase
{
	protected override string UpdateRoverOverlay_SpeedLimit()
	{
		return true switch
		{
			true when _roverOverlay_speedLimiter < 0.0f => "",
			true when _roverOverlay_speedLimiter <= 0.26f => "\xeab4\xe9e1",
			true when _roverOverlay_speedLimiter <= 0.51f => "\xeab5\xe9e1",
			true when _roverOverlay_speedLimiter <= 0.76f => "\xeab3\xe9e1",
			true when _roverOverlay_speedLimiter <= 1.01f => "\xeab7\xe9e1",
			_ => ""
		};
	}
}
