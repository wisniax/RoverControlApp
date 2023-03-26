using System;
using System.Net;
using System.ServiceModel;
using System.Threading.Tasks;
using Onvif;

namespace OnvifCameraControlTest;

public class OnvifCameraController
{
	//private readonly PTZClient _ptzClient;
	//private readonly PTZNode _ptzNode;
	//private readonly PTZStatus _ptzStatus;
	public CommunicationState State { get; }
	private OnvifAgent? Agent;

	public OnvifCameraController(string cameraUrl, string username, string password)
	{
		try
		{
			Agent = new OnvifAgent(cameraUrl, username, password);
			State = Agent.Device.Device.State;
		}
		catch (Exception e)
		{
			Agent = null;
			State = CommunicationState.Faulted;
		}
	}


	public void MoveLeft()
	{
		Agent?.Ptz.MoveRight();
	}

	public void MoveRight()
	{
		Agent?.Ptz.MoveLeft();
	}

	public void MoveUp()
	{
		Agent?.Ptz.MoveDown();
	}

	public void MoveDown()
	{
		Agent?.Ptz.MoveUp();
	}

	public void MoveStop()
	{
		Agent?.Ptz.Stop();
	}

	public void GotoHomePosition()
	{
		Agent?.Ptz.GotoHomePosition();
	}

	public void ZoomIn()
	{
		Agent?.Ptz.ZoomIn();
	}

	public void ZoomOut()
	{
		Agent?.Ptz.ZoomOut();
	}


}