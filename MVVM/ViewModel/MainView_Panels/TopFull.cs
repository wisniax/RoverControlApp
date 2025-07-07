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

	protected override string RtspPtzOverlay_Delay(float delay, out UIOverlay2.AnimationAlert suggestedAlert)
	{
		if (delay > 0.1f)
			suggestedAlert = AnimationSlow;
		else
			suggestedAlert = UIOverlay2.AnimationAlert.Off;

		if (delay >= 1.0f)
			return "> 1s";
		else
			return string.Format("{0:D}ms", delay * 1000.0f);
	}
}
