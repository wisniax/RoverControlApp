using System;
using System.Threading.Tasks;

using Godot;

using RoverControlApp.Core;
using RoverControlApp.MVVM.Model;

namespace RoverControlApp.MVVM.ViewModel;

public partial class MissionControl : VBoxContainer
{
	private MainViewModel mainView = null!;

	private const string TEXT_START = "\xea71 Start";
	private const string TEXT_RESUME = "\xea71 Resume";
	private const string TEXT_PAUSE = "\xea5e Pause";
	private const string TEXT_STOP = "\xeac9 Stop";

	[ExportGroup("Mission Control")]
	[Export]
	private Button SMissionControlStartBtn = null!, SMissionControlStopBtn = null!, SMissionControlRefreshBtn = null!;
	[ExportGroup("Mission Control")]
	[Export]
	private Label SMissionControlPOITimestampLab = null!;

	[ExportGroup("Mission Control")]
	[Export]
	private UIOverlay2 SMissionControlStatus = null!;

	[ExportGroup("POI Add")]
	[Export]
	private OptionButton SPoiAddTypeOpBtn = null!;
	[ExportGroup("POI Add")]
	[Export]
	private LineEdit SPoiAddTargetStrLEdit = null!;
	[ExportGroup("POI Add")]
	[Export]
	private LineEdit SPoiAddDescriptionStrLEdit = null!;
	[ExportGroup("POI Add")]
	[Export]
	private OptionButton SPoiAddPhotoTypeOpBtn = null!;
	[ExportGroup("POI Add")]
	[Export]
	private Button SPoiAddConfirmBtn = null!;


	[ExportGroup("POI Remove")]
	[Export]
	private OptionButton SPoiRemoveTypeOpBtn = null!;
	[ExportGroup("POI Remove")]
	[Export]
	private OptionButton SPoiRemoveTargetOpBtn = null!;
	[ExportGroup("POI Remove")]
	[Export]
	private Button SPoiRemoveConfirmBtn = null!;

	public bool PendingSend
	{
		get => _pendingSend;
		set
		{
			_pendingSend = value;
			OnSPoiAddChanged();
			OnSPoiRemoveChanged();
		}
	}
	private bool _pendingSend = false;

	public override void _Ready()
	{
		mainView = GetNode<MainViewModel>("/root/MainView");
		SMissionControlVisualUpdate();
		SPoiAddReset();
		SPoiRemoveReset();
	}

	public override void _GuiInput(InputEvent @event)
	{
		if (@event is not InputEventMouseButton)
			return;

		if(GetViewport().GuiGetFocusOwner() is not null)
			GetViewport().SetInputAsHandled();

		GetViewport().GuiReleaseFocus();
	}

	private void OnSMissionControlRefreshBtn()
	{
		SPoiRemoveReset();
		SMissionControlVisualUpdate();
	}

	private void OnSMissionControlStartBtn()
	{
		switch (MissionStatus.Singleton.Status?.MissionStatus)
		{
			case RoverControlApp.Core.MqttClasses.MissionStatus.Created:
			case RoverControlApp.Core.MqttClasses.MissionStatus.Stopped:
			case RoverControlApp.Core.MqttClasses.MissionStatus.Interrupted:
				MissionStatus.Singleton.StartMission();
				break;
		}
	}


	private void OnSMissionControlStopBtn()
	{
		switch (MissionStatus.Singleton.Status?.MissionStatus)
		{
			case RoverControlApp.Core.MqttClasses.MissionStatus.Started:
				MissionStatus.Singleton.PauseMission();
				break;
			case RoverControlApp.Core.MqttClasses.MissionStatus.Interrupted:
				MissionStatus.Singleton.StopMission();
				break;
		}
	}

	private void OnSPoiAddChanged()
	{
		SPoiAddTypeOpBtn.Disabled = PendingSend;
		SPoiAddTargetStrLEdit.Editable = !PendingSend;
		SPoiAddDescriptionStrLEdit.Editable = !PendingSend;
		SPoiAddPhotoTypeOpBtn.Disabled = PendingSend;

		bool SPoiAddDataSufficient =
			SPoiAddTypeOpBtn.Selected != -1
			&& !string.IsNullOrEmpty(SPoiAddTargetStrLEdit.Text)
			&& SPoiAddPhotoTypeOpBtn.Selected != -1
			&& !PendingSend;

		SPoiAddConfirmBtn.Disabled = !SPoiAddDataSufficient;
	}

	private async void OnSPoiAddConfirmPressed()
	{
		var request = MissionSetPoint.GenerateNewPointRequest((MqttClasses.PointType)SPoiAddTypeOpBtn.GetSelectedId(), SPoiAddTargetStrLEdit.Text, SPoiAddDescriptionStrLEdit.Text, (MqttClasses.PhotoType)SPoiAddPhotoTypeOpBtn.GetSelectedId());
		if (MissionSetPoint.Singleton is null)
		{
			EventLogger.LogMessage("MissionControl", EventLogger.LogLevel.Error, "Cannot add POIs, MissionSetPoint.Singleton is null!");
			return;
		}

		if ((MqttClasses.PhotoType)SPoiAddPhotoTypeOpBtn.GetSelectedId() != MqttClasses.PhotoType.None)
		{
			await Task.Run(() => mainView.CaptureCameraImage("POIImages", SPoiAddTargetStrLEdit.Text, "jpg"));
		}

		PendingSend = true;
		await MissionSetPoint.Singleton.SendNewPointRequest(request);
		PendingSend = false;

		SPoiAddReset();
		OnSMissionControlRefreshBtn();
	}

	private void OnSPoiRemoveChanged()
	{
		SPoiRemoveTypeOpBtn.Disabled = PendingSend;
		SPoiRemoveTargetOpBtn.Disabled = PendingSend;

		bool SPoiRemoveDataSufficient =
			SPoiRemoveTypeOpBtn.Selected != -1
			&& SPoiRemoveTargetOpBtn.Selected != -1
			&& !PendingSend;

		SPoiRemoveConfirmBtn.Disabled = !SPoiRemoveDataSufficient;
	}

	private void OnSPoiRemoveTypeChanged(int index)
	{
		SPoiRemoveTargetOpBtn.Clear();

		if (MissionSetPoint.Singleton.ActiveKmlObjects is null)
			return;

		var KmlList = MissionSetPoint.Singleton.ActiveKmlObjects;

		switch ((MqttClasses.PointType)SPoiRemoveTypeOpBtn.GetItemId(index))
		{
			case MqttClasses.PointType.RemovePoint:
				foreach (var point in KmlList.poi)
					SPoiRemoveTargetOpBtn.AddItem(point);

				break;
			case MqttClasses.PointType.RemovePoly:
				foreach (var poly in KmlList.area)
					SPoiRemoveTargetOpBtn.AddItem(poly);
				break;
		}

		SPoiRemoveTargetOpBtn.Select(-1);
		OnSPoiRemoveChanged();
	}

	private async void OnSPoiRemoveConfirmPressed()
	{
		if (MissionSetPoint.Singleton.ActiveKmlObjects is null)
		{
			EventLogger.LogMessage("MissionControl", EventLogger.LogLevel.Error, "Cannot remove POIs, MissionSetPoint.Singleton is null!");
			return;
		}

		var KmlList = MissionSetPoint.Singleton.ActiveKmlObjects;
		string targetStr = null!;
		switch ((MqttClasses.PointType)SPoiRemoveTypeOpBtn.GetSelectedId())
		{
			case MqttClasses.PointType.RemovePoint:
				targetStr = KmlList.poi[SPoiRemoveTargetOpBtn.GetSelectedId()];
				break;
			case MqttClasses.PointType.RemovePoly:
				targetStr = KmlList.area[SPoiRemoveTargetOpBtn.GetSelectedId()];
				break;
		}

		var request = MissionSetPoint.GenerateNewPointRequest((MqttClasses.PointType)SPoiRemoveTypeOpBtn.GetSelectedId(), targetStr, string.Empty, MqttClasses.PhotoType.None);

		PendingSend = true;
		await MissionSetPoint.Singleton.SendNewPointRequest(request);
		PendingSend = false;

		OnSMissionControlRefreshBtn();
		SPoiRemoveReset();
	}

	private void SPoiAddReset()
	{
		SPoiAddTypeOpBtn.Select(-1);
		SPoiAddTargetStrLEdit.Text = string.Empty;
		SPoiAddPhotoTypeOpBtn.Select(-1);
		SPoiAddDescriptionStrLEdit.Text = string.Empty;

		OnSPoiAddChanged();
	}

	private void SPoiRemoveReset()
	{
		SPoiRemoveTypeOpBtn.Select(-1);
		SPoiRemoveTargetOpBtn.Select(-1);
		SPoiRemoveTargetOpBtn.Clear();

		OnSPoiRemoveChanged();
	}

	public void LoadSizeAndPos()
	{
		return; //TODO migrate to widget
		var vec2String = LocalSettings.Singleton.General.MissionControlSize.Split(';');
		Size = new Vector2I(Convert.ToInt32(vec2String[0]), Convert.ToInt32(vec2String[1]));
		vec2String = LocalSettings.Singleton.General.MissionControlPosition.Split(';');
		Position = new Vector2I(Convert.ToInt32(vec2String[0]), Convert.ToInt32(vec2String[1]));
	}

	public void SaveSizeAndPos()
	{
		var maxSize = GetTree().Root.GetViewport().GetVisibleRect().Size;

		Size = new Vector2(Math.Clamp(Size.X, CustomMinimumSize.X, maxSize.X), Size.Y);
		Position = new Vector2(Math.Clamp(Position.X, 0, maxSize.X - Size.X), Math.Clamp(Position.Y, 30, maxSize.Y - Size.Y));

		LocalSettings.Singleton.General.MissionControlSize = Size.X.ToString() + ';' + Size.Y.ToString();
		LocalSettings.Singleton.General.MissionControlPosition = Position.X.ToString() + ';' + Position.Y.ToString();
	}

	public Task MissionStatusUpdatedSubscriber(MqttClasses.RoverMissionStatus? status)
	{
		CallDeferred(MethodName.SMissionControlVisualUpdate);
		return Task.CompletedTask;
	}

	public void SMissionControlVisualUpdate()
	{
		SMissionControlStatus.ControlMode = (int?)MissionStatus.Singleton.Status?.MissionStatus ?? 6;

		switch (MissionStatus.Singleton.Status?.MissionStatus)
		{
			case RoverControlApp.Core.MqttClasses.MissionStatus.Created:
			case RoverControlApp.Core.MqttClasses.MissionStatus.Stopped:
				SMissionControlStartBtn.Disabled = false;
				SMissionControlStartBtn.Text = TEXT_START;
				SMissionControlStopBtn.Disabled = true;
				SMissionControlStopBtn.Text = TEXT_STOP;
				break;
			case RoverControlApp.Core.MqttClasses.MissionStatus.Starting:
			case RoverControlApp.Core.MqttClasses.MissionStatus.Stopping:
				SMissionControlStartBtn.Disabled = true;
				SMissionControlStartBtn.Text = TEXT_RESUME;
				SMissionControlStopBtn.Disabled = true;
				SMissionControlStopBtn.Text = TEXT_PAUSE;
				break;
			case RoverControlApp.Core.MqttClasses.MissionStatus.Started:
				SMissionControlStartBtn.Disabled = true;
				SMissionControlStartBtn.Text = TEXT_RESUME;
				SMissionControlStopBtn.Disabled = false;
				SMissionControlStopBtn.Text = TEXT_PAUSE;
				break;
			case RoverControlApp.Core.MqttClasses.MissionStatus.Interrupted:
				SMissionControlStartBtn.Disabled = false;
				SMissionControlStartBtn.Text = TEXT_RESUME;
				SMissionControlStopBtn.Disabled = false;
				SMissionControlStopBtn.Text = TEXT_STOP;
				break;
			default:
				SMissionControlStartBtn.Disabled = true;
				SMissionControlStartBtn.Text = TEXT_START;
				SMissionControlStopBtn.Disabled = true;
				SMissionControlStopBtn.Text = TEXT_STOP;
				break;
		}

		//SMissionControlRefreshBtn.Disabled = MissionSetPoint.Singleton is null;
		string? timestampStr =
			MissionSetPoint.Singleton.ActiveKmlObjects?.Timestamp is null
			? null
			: DateTimeOffset.FromUnixTimeSeconds(MissionSetPoint.Singleton.ActiveKmlObjects!.Timestamp ?? 0).ToLocalTime().ToString("s");
		SMissionControlPOITimestampLab.Text = $"ActiveKmlObject Timestamp: {timestampStr ?? "N/A"}";
	}
}
