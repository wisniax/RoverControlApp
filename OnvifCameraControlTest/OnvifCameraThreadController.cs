using Onvif.Core.Client.Common;
using Onvif.Core.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace OnvifCameraControlTest
{
	public enum CameraMotionState
	{
		Stop,
		PanLeft,
		PanRight,
		PanUp,
		PanDown
	}

	public enum CameraZoomState
	{
		Stop,
		Increase,
		Decrease
	}
	public class OnvifCameraThreadController
	{
		private readonly Mutex _dataMutex = new();
		private readonly Mutex _threadMutex = new();

		/// <summary>
		/// Current camera motion operation.
		/// </summary>
		public CameraMotionState CameraMotion
		{
			get
			{
				_dataMutex.WaitOne();
				var copy = _cameraMotion;
				_dataMutex.ReleaseMutex();
				return copy;
			}
			set
			{
				_dataMutex.WaitOne();
				_cameraMotion = value;
				_dataMutex.ReleaseMutex();
			}
		}
		volatile CameraMotionState _cameraMotion = CameraMotionState.Stop;

		/// <summary>
		/// Current camera zoom operation.
		/// </summary>
		public CameraZoomState CameraZoom
		{
			get
			{
				_dataMutex.WaitOne();
				var copy = _cameraZoom;
				_dataMutex.ReleaseMutex();
				return copy;
			}
			set
			{
				_dataMutex.WaitOne();
				_cameraZoom = value;
				_dataMutex.ReleaseMutex();
			}
		}
		volatile CameraZoomState _cameraZoom = CameraZoomState.Stop;

		/// <summary>
		/// State of onvif.
		/// </summary>
		public CommunicationState State
		{
			get
			{
				_dataMutex.WaitOne();
				var copy = _state;
				_dataMutex.ReleaseMutex();
				return copy;
			}
			private set
			{
				_dataMutex.WaitOne();
				_state = value;
				_dataMutex.ReleaseMutex();
			}
		}
		volatile CommunicationState _state;

		/// <summary>
		/// If state is <see cref="CommunicationState.Faulted"/>, here is the error.
		/// Otherwise null.
		/// </summary>
		Exception? ThreadError
		{
			get
			{
				_threadMutex.WaitOne();
				var copy = _threadError;
				_threadMutex.ReleaseMutex();
				return copy;
			}
		}
		Exception? _threadError = null;

		/// <summary>
		/// Meant to be used by ThreadWork ONLY <br/>
		/// Are u asking me why it is here then? <br/>
		/// better ask yourself WHY ARE YOU HERE and reading this
		/// </summary>
		private Camera? _camera = null;

		Thread? _thread;

		public OnvifCameraThreadController()
		{
			State = CommunicationState.Closed;
		}

		/// <summary>
		/// Starts camera connection attempt 
		/// </summary>
		/// <param name="cameraUrl"></param>
		/// <param name="user"></param>
		/// <param name="password"></param>
		/// <returns><see cref="CommunicationState.Faulted"/> when there was error during startup, otherwise <see cref="CommunicationState.Opened"/></returns>
		/// <exception cref="InvalidOperationException"></exception>
		public async Task<CommunicationState> Start(string cameraUrl, string user, string password)
		{
			if (_thread != null)
				throw new InvalidOperationException("Thread is still valid");

			State = CommunicationState.Created;
			_threadError = null;
			_thread = new(ThreadWork) { IsBackground = true };

			var acc = new Account(cameraUrl, user, password);
			_thread.Start(acc);

			//wait till startup completed
			do
				await Task.Delay(420);
			while (State == CommunicationState.Created || State == CommunicationState.Opening);

			//check health
			if (State == CommunicationState.Faulted)
				_thread = null;

			return State;
		}

		private void ThreadWork(object? obj)
		{
			//start up
			State = CommunicationState.Opening;
			{
				_threadMutex.WaitOne();

				_camera = Camera.Create((Account)obj!, (e) => _threadError = e);

				if (_threadError is not null)
				{
					State = CommunicationState.Faulted;
					_threadMutex.ReleaseMutex();
					return;
				}

				_threadMutex.ReleaseMutex();
			}

			//camera operation loop
			State = CommunicationState.Opened;

			CameraMotionState motionLast = CameraMotion = CameraMotionState.Stop;
			CameraZoomState zoomLast = CameraZoom = CameraZoomState.Stop;

			while (State == CommunicationState.Opened)
			{
				_threadMutex.WaitOne();
				if (!CameraMotion.Equals(motionLast))
					Thread.Yield();
				_threadMutex.ReleaseMutex();
			}

			//closeup
			State = CommunicationState.Closing;
			{
				_threadMutex.WaitOne();
				_camera = null; // no close idk, and closing internals is dumb idea
				_threadMutex.ReleaseMutex();
			}

			State = CommunicationState.Closed;
		}

		//public async void MoveLeft()
		//{
		//    var vector2 = new PTZVector { PanTilt = new Vector2D { x = 1f } };
		//    var speed2 = new PTZSpeed { PanTilt = new Vector2D { x = 1f, y = 1f } };
		//    await _agent.MoveAsync(MoveType.Relative, vector2, speed2, 0);
		//}

		//public async void MoveRight()
		//{
		//    var vector1 = new PTZVector { PanTilt = new Vector2D { x = -1f } };
		//    var speed1 = new PTZSpeed { PanTilt = new Vector2D { x = 1f, y = 1f } };
		//    await _agent.MoveAsync(MoveType.Relative, vector1, speed1, 0);
		//}

		//public async void MoveUp()
		//{
		//    var vector4 = new PTZVector { PanTilt = new Vector2D { y = 1f } };
		//    var speed4 = new PTZSpeed { PanTilt = new Vector2D { x = 1f, y = 1f } };
		//    await _agent.MoveAsync(MoveType.Relative, vector4, speed4, 0);
		//}

		//public async void MoveDown()
		//{
		//    var vector3 = new PTZVector { PanTilt = new Vector2D { y = -1f } };
		//    var speed3 = new PTZSpeed { PanTilt = new Vector2D { x = 1f, y = 1f } };
		//    await _agent.MoveAsync(MoveType.Relative, vector3, speed3, 0);
		//}

		//public async void MoveStop()
		//{
		//    if (_agent == null) return;
		//    await _agent?.Ptz.StopAsync(_agent.Profile.token, true, false)!;
		//}

		//public async void GotoHomePosition()
		//{
		//    if (_agent == null) return;
		//    var speed = new PTZSpeed { PanTilt = new Vector2D { x = 1f, y = 1f } };
		//    await _agent?.Ptz.GotoHomePositionAsync(_agent.Profile.token, speed)!;
		//}

		//public async void ZoomIn()
		//{
		//    var vector2 = new PTZVector { Zoom = new Vector1D { x = 1f } };
		//    var speed2 = new PTZSpeed { Zoom = new Vector1D { x = 1f } };
		//    await _agent.MoveAsync(MoveType.Relative, vector2, speed2, 0);
		//}

		//public async void ZoomOut()
		//{
		//    var vector2 = new PTZVector { Zoom = new Vector1D { x = -1f } };
		//    var speed2 = new PTZSpeed { Zoom = new Vector1D { x = 1f } };
		//    await _agent.MoveAsync(MoveType.Relative, vector2, speed2, 0);
		//}

		//public async void ZoomStop()
		//{
		//    if (_agent == null) return;
		//    await _agent?.Ptz.StopAsync(_agent.Profile.token, false, true)!;
		//}
	}
}
