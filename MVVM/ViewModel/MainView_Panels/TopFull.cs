namespace RoverControlApp.MVVM.ViewModel.MainView_Panel;

public partial class TopFull : TopPanelBase
{
	protected override string UpdateRoverOverlay_SpeedLimit()
	{
		return true switch
		{
			true when _roverOverlay_speedLimiter < 0.0f => "",
			_ => $"{_roverOverlay_speedLimiter * 100:F0}%\xe9e1"
		};
	}
}
