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

	protected override string RtspPtzOverlay_Delay(float delay, out UIOverlay2.AnimationAlert suggestedAlert)
	{
		if (delay > 0.1f)
			suggestedAlert = AnimationSlow;
		else
			suggestedAlert = UIOverlay2.AnimationAlert.Off;

		return true switch
		{
			true when delay < 0.001f => "",
			true when delay <= 0.1f => "\xeab4\xe953",
			true when delay <= 0.2f => "\xeab5\xe90a",
			true when delay <= 0.4f => "\xeab3\xe909",
			true when delay <= 1.0f => "\xeab7\xe909",
			_ => ""
		};
	}
}
