using System;
using System.Threading.Tasks;

namespace OnvifCameraControlTest
{
	public class Program
	{
		static async Task Main(string[] args)
		{
			// Initialize the camera controller
			var controller = new OnvifCameraController("http://192.168.1.100/onvif/device_service", "username", "password");

			// Start the camera movement key bindings
			
		}
	}
}
