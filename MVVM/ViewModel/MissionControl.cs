using Godot;
using RoverControlApp.Core;
using RoverControlApp.MVVM.ViewModel;
using System;
using System.Threading.Tasks;

public partial class MissionControl : Window
{
	private const string TEXT_START = "Start";
	private const string TEXT_RESUME = "Resume";
	private const string TEXT_PAUSE = "Pause";
	private const string TEXT_STOP = "Stop";


	[Export]
	private NodePath BtnStartNodePath = null!;
	[Export]
	private NodePath BtnStopNodePath = null!;
	[Export]
	private NodePath LabelStatusNodePath = null!;

	private Button _btnStart, _btnStop;
	private Label _labelStatus;

	public override void _Ready()
	{
		_btnStart = GetNode<Button>(BtnStartNodePath);
		_btnStop = GetNode<Button>(BtnStopNodePath);
		_labelStatus = GetNode<Label>(LabelStatusNodePath);

		_btnStart.Connect(Button.SignalName.Pressed, new Callable(this, MethodName.BtnStartPressedSubscriber));
		_btnStop.Connect(Button.SignalName.Pressed, new Callable(this, MethodName.BtnStopPressedSubscriber));

		UpdateVisual();
	}

	private void BtnStartPressedSubscriber()
	{
		switch (MainViewModel.MissionStatus?.Status?.MissionStatus)
		{
			case RoverControlApp.Core.MqttClasses.MissionStatus.Created:
			case RoverControlApp.Core.MqttClasses.MissionStatus.Stopped:
			case RoverControlApp.Core.MqttClasses.MissionStatus.Interrupted:
				MainViewModel.MissionStatus!.StartMission();
				break;
		}
	}

	private void BtnStopPressedSubscriber()
	{
		switch (MainViewModel.MissionStatus?.Status?.MissionStatus)
		{
			case RoverControlApp.Core.MqttClasses.MissionStatus.Started:
				MainViewModel.MissionStatus!.PauseMission();
				break;
			case RoverControlApp.Core.MqttClasses.MissionStatus.Interrupted:
				MainViewModel.MissionStatus!.StopMission();
				break;
		}
	}

	public void SaveSizeAndPos()
	{
		MainViewModel.Settings!.Settings!.MissionControlSize = Size.X.ToString() + ';' + Size.Y.ToString();
		MainViewModel.Settings!.Settings!.MissionControlPosition = Position.X.ToString() + ';' + Position.Y.ToString();
	}

	public Task MissionStatusUpdatedSubscriber(MqttClasses.RoverMissionStatus? status)
	{
		UpdateVisual();
		return Task.CompletedTask;
	}

	public void UpdateVisual()
	{
		_labelStatus.Text = MainViewModel.MissionStatus?.Status?.MissionStatus.ToString() ?? "N/A";
		switch (MainViewModel.MissionStatus?.Status?.MissionStatus)
		{
			case RoverControlApp.Core.MqttClasses.MissionStatus.Created:
			case RoverControlApp.Core.MqttClasses.MissionStatus.Stopped:
				_btnStart.Disabled = false;
				_btnStart.Text = TEXT_START;
				_btnStop.Disabled = true;
				_btnStop.Text = TEXT_STOP;
				break;
			case RoverControlApp.Core.MqttClasses.MissionStatus.Starting:
			case RoverControlApp.Core.MqttClasses.MissionStatus.Stopping:
				_btnStart.Disabled = true;
				_btnStart.Text = TEXT_RESUME;
				_btnStop.Disabled = true;
				_btnStop.Text = TEXT_PAUSE;
				break;
			case RoverControlApp.Core.MqttClasses.MissionStatus.Started:
				_btnStart.Disabled = true;
				_btnStart.Text = TEXT_RESUME;
				_btnStop.Disabled = false;
				_btnStop.Text = TEXT_PAUSE;
				break;
			case RoverControlApp.Core.MqttClasses.MissionStatus.Interrupted:
				_btnStart.Disabled = false;
				_btnStart.Text = TEXT_RESUME;
				_btnStop.Disabled = false;
				_btnStop.Text = TEXT_STOP;
				break;
			default:
				_btnStart.Disabled = true;
				_btnStart.Text = TEXT_START;
				_btnStop.Disabled = true;
				_btnStop.Text = TEXT_STOP;
				break;
		}
		
	}
}
