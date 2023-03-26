using System;
using System.Net;
using System.Threading.Tasks;
using Onvif;

namespace OnvifCameraControlTest;

public class OnvifCameraController
{
	//private readonly PTZClient _ptzClient;
	//private readonly PTZNode _ptzNode;
	//private readonly PTZStatus _ptzStatus;
	private OnvifAgent? Agent;

	public OnvifCameraController(string cameraUrl, string username, string password)
	{
		Agent = new OnvifAgent(cameraUrl, username, password);
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

	public void EnableCameraKeybindings()
	{
		// q: How to enable the camera keybindings?
	}
	//witaj
	//Witam :)

}