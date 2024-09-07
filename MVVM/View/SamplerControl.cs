using Godot;
using System;
using System.Text.Json;
using System.Threading.Tasks;
using MQTTnet.Protocol;
using RoverControlApp.Core;
using RoverControlApp.MVVM.Model;

public partial class SamplerControl : Panel
{
	[Export] private Label _drillStateLabel;
	[Export] private Label _platformStateLabel;
	[Export] private Label _containerStateLabel;
	
	private MqttClasses.SamplerControl _samplerControl = new();

	[Export] private Button DrillUp;
	[Export] private Button DrillStop;
	[Export] private Button DrillDown;
	[Export] private Button PlatformUp;
	[Export] private Button PlatformStop;
	[Export] private Button PlatformDown;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		#region GodotFixYourShit
		Button DrillUp = GetNode<Button>("DrillMenu/VBoxContainer/UP");
		DrillUp.Pressed += () => OnDrillAction(MqttClasses.SamplerDirection.Up);
		DrillUp.ButtonUp += () => OnDrillAction(MqttClasses.SamplerDirection.Stop);
		Button DrillStop = GetNode<Button>("DrillMenu/VBoxContainer/STOP");
		DrillStop.Pressed += () => OnDrillAction(MqttClasses.SamplerDirection.Stop);
		Button DrillDown = GetNode<Button>("DrillMenu/VBoxContainer/DOWN");
		DrillDown.Pressed += () => OnDrillAction(MqttClasses.SamplerDirection.Down);
		DrillDown.ButtonUp += () => OnDrillAction(MqttClasses.SamplerDirection.Stop);

		Button DrillingStop = GetNode<Button>("DrillMenu/VBoxContainer2/STOP");
		DrillingStop.Pressed += () => OnDrillAction(MqttClasses.DrillState.Stopped);
		Button DrillingLeft = GetNode<Button>("DrillMenu/VBoxContainer2/LEFT");
		DrillingLeft.Pressed += () => OnDrillAction(MqttClasses.DrillState.Left);
		Button DrillingRight = GetNode<Button>("DrillMenu/VBoxContainer2/RIGHT");
		DrillingRight.Pressed += () => OnDrillAction(MqttClasses.DrillState.Right);

		Button MoveContainer = GetNode<Button>("ContainerMenu/MOVE");
		MoveContainer.Pressed += () => OnContainerAction();

		Button PlatformUp = GetNode<Button>("PlatformMenu/VBoxContainer/UP");
		PlatformUp.Pressed += () => OnPlatformAction(MqttClasses.SamplerDirection.Up);
		PlatformUp.ButtonUp += () => OnPlatformAction(MqttClasses.SamplerDirection.Stop);
		Button PlatformStop = GetNode<Button>("PlatformMenu/VBoxContainer/STOP");
		PlatformStop.Pressed += () => OnPlatformAction(MqttClasses.SamplerDirection.Stop);
		Button PlatformDown = GetNode<Button>("PlatformMenu/VBoxContainer/DOWN");
		PlatformDown.Pressed += () => OnPlatformAction(MqttClasses.SamplerDirection.Down);
		PlatformDown.ButtonUp += () => OnPlatformAction(MqttClasses.SamplerDirection.Stop);

		#endregion

	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		
	}

	public void OnDrillAction(MqttClasses.SamplerDirection direction)
	{
		if (_samplerControl.DrillCommand == direction)
		{
			return;
		}
		_samplerControl.DrillCommand = direction;

		DrillLabelUpdate();
		SendSamplerMsg();
	}



	public void OnDrillAction(MqttClasses.DrillState drillState)
	{
		if (_samplerControl.DrillState == drillState)
		{
			return;
		}
		_samplerControl.DrillState = drillState;

		DrillLabelUpdate();
		SendSamplerMsg();
	}

	void DrillLabelUpdate()
	{
		switch (_samplerControl.DrillCommand)
		{
			case MqttClasses.SamplerDirection.Down:
				_drillStateLabel.Text = "Moving: Down\n";
				break;
			case MqttClasses.SamplerDirection.Stop:
				_drillStateLabel.Text = "Moving: Stop\n";
				break;
			case MqttClasses.SamplerDirection.Up:
				_drillStateLabel.Text = "Moving: Up\n";
				break;
		}

		switch (_samplerControl.DrillState)
		{
			case MqttClasses.DrillState.Left:
				_drillStateLabel.Text += "Drilling: Left";
				break;
			case MqttClasses.DrillState.Right:
				_drillStateLabel.Text += "Drilling: Right";
				break;
			case MqttClasses.DrillState.Stopped:
				_drillStateLabel.Text += "Drilling: Stopped";
				break;
		}
	}

	public void OnPlatformAction(MqttClasses.SamplerDirection direction)
	{
		if (_samplerControl.PlatformCommand == direction)
		{
			return;
		}
		_samplerControl.PlatformCommand = direction;

		MqttClasses.SamplerDirection enumValue = (MqttClasses.SamplerDirection)direction;
		string enumName = Enum.GetName(typeof(MqttClasses.SamplerDirection), enumValue);
		_platformStateLabel.Text = $"State: {enumName}";


		SendSamplerMsg();


	}

	public void OnContainerAction()
	{
		_samplerControl.isContainerExtended = !_samplerControl.isContainerExtended;
		if(_samplerControl.isContainerExtended)
		{
			_containerStateLabel.Text = "State: Extended";
		}
		else
		{
			_containerStateLabel.Text = "State: Retracted";
		}

		SendSamplerMsg();
	}

	public async Task SendSamplerMsg()
	{
		await MqttNode.Singleton.EnqueueMessageAsync(LocalSettings.Singleton.Mqtt.TopicSampler, JsonSerializer.Serialize(_samplerControl), MqttQualityOfServiceLevel.ExactlyOnce);
	}
}
