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

	public OnvifCameraController(string cameraUrl, string username, string password)
	{
		var task = Task.Run(() => ConnectToCamera(cameraUrl, username, password));

		_agent = task.Wait(TimeSpan.FromSeconds(10)) ? task.Result : null;
		State = _agent == null ? CommunicationState.Opening : CommunicationState.Faulted;
	}

	private Camera? ConnectToCamera(string cameraUrl, string username, string password)
	{
		return Camera.Create(new Account(cameraUrl, username, password), ex =>
		{
			State = CommunicationState.Faulted;

		});
	}

	public async void MoveLeft()
	{
		var vector2 = new PTZVector { PanTilt = new Vector2D { x = 1f } };
		var speed2 = new PTZSpeed { PanTilt = new Vector2D { x = 1f, y = 1f } };
		await _agent.MoveAsync(MoveType.Relative, vector2, speed2, 0);
	}

	public async void MoveRight()
	{
		var vector1 = new PTZVector { PanTilt = new Vector2D { x = -1f } };
		var speed1 = new PTZSpeed { PanTilt = new Vector2D { x = 1f, y = 1f } };
		await _agent.MoveAsync(MoveType.Relative, vector1, speed1, 0);
	}

	public async void MoveUp()
	{
		var vector4 = new PTZVector { PanTilt = new Vector2D { y = 1f } };
		var speed4 = new PTZSpeed { PanTilt = new Vector2D { x = 1f, y = 1f } };
		await _agent.MoveAsync(MoveType.Relative, vector4, speed4, 0);
	}

	public async void MoveDown()
	{
		var vector3 = new PTZVector { PanTilt = new Vector2D { y = -1f } };
		var speed3 = new PTZSpeed { PanTilt = new Vector2D { x = 1f, y = 1f } };
		await _agent.MoveAsync(MoveType.Relative, vector3, speed3, 0);
	}

	public async void MoveStop()
	{
		if (_agent == null) return;
		await _agent?.Ptz.StopAsync(_agent.Profile.token, true, false)!;
	}

	public async void GotoHomePosition()
	{
		if (_agent == null) return;
		var speed = new PTZSpeed { PanTilt = new Vector2D { x = 1f, y = 1f } };
		await _agent?.Ptz.GotoHomePositionAsync(_agent.Profile.token, speed)!;
	}

	public async void ZoomIn()
	{
		var vector2 = new PTZVector { Zoom = new Vector1D { x = 1f } };
		var speed2 = new PTZSpeed { Zoom = new Vector1D { x = 1f } };
		await _agent.MoveAsync(MoveType.Relative, vector2, speed2, 0);
	}

	public async void ZoomOut()
	{
		var vector2 = new PTZVector { Zoom = new Vector1D { x = -1f } };
		var speed2 = new PTZSpeed { Zoom = new Vector1D { x = 1f } };
		await _agent.MoveAsync(MoveType.Relative, vector2, speed2, 0);
	}

	public async void ZoomStop()
	{
		if (_agent == null) return;
		await _agent?.Ptz.StopAsync(_agent.Profile.token, false, true)!;
	}


}