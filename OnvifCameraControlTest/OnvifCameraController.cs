using System;
using System.Net;
using System.Runtime.CompilerServices;
using System.ServiceModel;
using System.Threading.Tasks;
using Onvif.Core;
using Onvif.Core.Client;
using Onvif.Core.Client.Common;

namespace OnvifCameraControlTest;
/// <summary>
/// Wrapper class to rename more consistently Onvif.Core features that will be used through out this solution.
/// </summary>
public class OnvifCameraController
{
	public CommunicationState State { get; private set; }
	private Camera? _agent;
	private Thread? _movThread;

	public OnvifCameraController(string cameraUrl, string username, string password)
	{
		_movThread = new Thread(() =>
		{
			var account = new Account(cameraUrl, username,password);
			_agent = Camera.Create(account, ex =>
			{
				//State = CommunicationState.Faulted;

			});
		});

		_movThread.Start();

		//_movThread.Join();


		//var task = Task.Run(() => ConnectToCamera(cameraUrl, username, password));

		//_agent = task.Wait(TimeSpan.FromSeconds(60)) ? task.Result : null; // nie robi się w 10s (w ~30s) -> zmiana na 60s 

        //var account = new Account("192.168.5.35", "admin", "admin");
        //_agent = Camera.Create(account, ex =>
        //{
        //    //State = CommunicationState.Faulted;

        //});

        State = _agent == null ? CommunicationState.Faulted : CommunicationState.Opened;
	}

	private Camera? ConnectToCamera(string cameraUrl, string username, string password)
	{
		return Camera.Create(new Account(cameraUrl, username, password), ex =>
		{
			State = CommunicationState.Faulted;

		});
	}

	public void MoveLeft()
	{
		if (_agent == null) return;
		_movThread = new Thread(() =>
		{
			var speed2 = new PTZSpeed { PanTilt = new Vector2D { x = 1f, y = 0f } };
			_agent.MoveAsync(MoveType.Continuous, null, speed2, 5);
		});
		//movThread.SetApartmentState(ApartmentState.STA);
		_movThread.Start();
	}

	public async void MoveRight()
	{
		if (_agent == null) return;
		_movThread = new Thread(() =>
		{
			var speed2 = new PTZSpeed { PanTilt = new Vector2D { x = -1f, y = 0f } };
			_agent.MoveAsync(MoveType.Continuous, null, speed2, 5);
		});
		_movThread.Start();
		//var vector1 = new PTZVector { PanTilt = new Vector2D { x = -1f } };
		//var speed1 = new PTZSpeed { PanTilt = new Vector2D { x = 1f, y = 1f } };
		//await _agent.MoveAsync(MoveType.Relative, vector1, speed1, 0);
	}

	public async void MoveUp()
	{
		if (_agent == null) return;
		_movThread = new Thread(() =>
		{
			var speed2 = new PTZSpeed { PanTilt = new Vector2D { x = 0f, y = -1f } };
			_agent.MoveAsync(MoveType.Continuous, null, speed2, 5);
		});
		_movThread.Start();
		//var vector4 = new PTZVector { PanTilt = new Vector2D { y = 1f } };
		//var speed4 = new PTZSpeed { PanTilt = new Vector2D { x = 1f, y = 1f } };
		//await _agent.MoveAsync(MoveType.Relative, vector4, speed4, 0);
	}

	public async void MoveDown()
	{
		if (_agent == null) return;
		_movThread = new Thread(() =>
		{
			var speed2 = new PTZSpeed { PanTilt = new Vector2D { x = 0f, y = 1f } };
			_agent.MoveAsync(MoveType.Continuous, null, speed2, 5);
		});
		_movThread.Start();
		//var vector3 = new PTZVector { PanTilt = new Vector2D { y = -1f } };
		//var speed3 = new PTZSpeed { PanTilt = new Vector2D { x = 1f, y = 1f } };
		//await _agent.MoveAsync(MoveType.Relative, vector3, speed3, 0);
		//_agent?.Ptz.SetHomePositionAsync(_agent.Profile.token);
	}

	public async void MoveStop()
	{
		if (_agent == null) return;
		_movThread = new Thread(() =>
		{
			_agent?.Ptz.StopAsync(_agent.Profile.token, true, true);
		});
		_movThread.Start();
	}

	public async void GotoHomePosition()
	{
		if (_agent == null) return;
		var speed = new PTZSpeed { PanTilt = new Vector2D { x = 1f, y = 1f } };
		await _agent?.Ptz.GotoHomePositionAsync(_agent.Profile.token, speed)!;
	}

	public async void ZoomIn()
	{
		if (_agent == null) return;
		_movThread = new Thread(() =>
		{
			var speed2 = new PTZSpeed { Zoom = new Vector1D { x = 1f } };
			_agent.MoveAsync(MoveType.Continuous, null, speed2, 5);
		});
		_movThread.Start();

		//var vector2 = new PTZVector { Zoom = new Vector1D { x = 1f } };
		//var speed2 = new PTZSpeed { Zoom = new Vector1D { x = 1f } };
		//await _agent.MoveAsync(MoveType.Continuous, vector2, speed2, 0);
	}

	public async void ZoomOut()
	{
		if (_agent == null) return;
		_movThread = new Thread(() =>
		{
			var speed2 = new PTZSpeed { Zoom = new Vector1D { x = -1f } };
			_agent.MoveAsync(MoveType.Continuous, null, speed2, 5);
		});
		_movThread.Start();

		//var vector2 = new PTZVector { Zoom = new Vector1D { x = -1f } };
		//var speed2 = new PTZSpeed { Zoom = new Vector1D { x = 1f } };
		//await _agent.MoveAsync(MoveType.Continuous, vector2, speed2, 5);
	}

	public async void ZoomStop()
	{
		if (_agent == null) return;
		await _agent?.Ptz.StopAsync(_agent.Profile.token, false, true)!;
	}


}