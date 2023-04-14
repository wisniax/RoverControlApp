using Onvif.Core.Client.Common;
using Onvif.Core.Client;
using System.ServiceModel;
using System.Numerics;

namespace OnvifCameraControlTest
{
	[Flags]
	public enum CameraMotionState
	{
		None = 0b0000,
		Pan_Lock = 0b1111,
		PanRight = 0b0001,
		PanLeft = 0b0010,
		PanRL_Lock = 0b0011,
		PanUp = 0b0100,
		PanDown = 0b1000,
		PanUD_Lock = 0b1100
	}

	public enum CameraZoomState
	{
		None,
		ZoomIn,
		ZoomOut
	}

	public class OnvifCameraThreadController
	{

		private static readonly TimeSpan MinSpanEveryCom = new(0, 0, 0,0,500);
		//private const int ComMaxInTimespan = 1;
		
		/// <summary>
		/// Should X and Y axis be inverted?
		/// </summary>
		public readonly bool InvertControl = false;
		
		/// <summary>
		/// Meant to be used by ThreadWork ONLY <br/>
		/// </summary>
		private System.DateTime _lastComTimeStamp = System.DateTime.Now;

		private readonly Mutex _dataMutex = new();
		private readonly Barrier _threadBarrier = new(1);

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
				_threadBarrier.SignalAndWait(0);
			}
		}

		private volatile CameraMotionState _cameraMotion = CameraMotionState.None;

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
				_threadBarrier.SignalAndWait(0);
			}
		}

		private volatile CameraZoomState _cameraZoom = CameraZoomState.None;

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

		private volatile CommunicationState _state;

		/// <summary>
		/// If state is <see cref="CommunicationState.Faulted"/>, here is the error.
		/// Otherwise null.
		/// </summary>
		public Exception? ThreadError
		{
			get
			{
				_dataMutex.WaitOne();
				var copy = _threadError;
				_dataMutex.ReleaseMutex();
				return copy;
			}
		}

		private Exception? _threadError = null;

		/// <summary>
		/// Meant to be used by ThreadWork ONLY <br/>
		/// Are u asking me why it is here then? <br/>
		/// better ask yourself WHY ARE YOU HERE and reading this
		/// </summary>
		private Camera? _tcamera = null;

		private Thread? _thread;

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
			_thread = new Thread(ThreadWork) { IsBackground = true };

			var acc = new Account(cameraUrl, user, password);
			_thread.Start(acc);

			//wait till startup completed

			while (State is CommunicationState.Created or CommunicationState.Opening) await Task.Delay(69);

			//check health
			if (State == CommunicationState.Faulted)
				_thread = null;

			return State;
		}

		public void Stop()
		{
			State = CommunicationState.Closing;
			_threadBarrier.SignalAndWait(0);
		}

		private void ThreadWork(object? obj)
		{
			_threadBarrier.AddParticipant();

			//start up
			State = CommunicationState.Opening;
			{
				_tcamera = Camera.Create((Account)obj!, (e) => _threadError = e);

				if (_threadError is not null)
				{
					State = CommunicationState.Faulted;
					_threadBarrier.RemoveParticipant();
					return;
				}
			}
			//camera operation loop pre
			State = CommunicationState.Opened;

			_dataMutex.WaitOne();
			CameraMotionState motionLast = _cameraMotion = CameraMotionState.None;
			CameraZoomState zoomLast = _cameraZoom = CameraZoomState.None;
			_dataMutex.ReleaseMutex();

			//camera operation loop
			while (State == CommunicationState.Opened)
			{
				_threadBarrier.SignalAndWait();

				if (UpdateMotion(motionLast, CameraMotion, out Vector2 panVector))
					if (panVector.LengthSquared() < 0.1f)
					{
						ComSleepTillCanRequest();
						_tcamera.Ptz.StopAsync(_tcamera.Profile.token, true, false).Wait();
					}
					else
					{
						PTZSpeed ptzSpeed = new()
						{
							PanTilt = new()
							{
								x = panVector.X,
								y = panVector.Y
							}
						};
						ComSleepTillCanRequest();
						_tcamera.Ptz.ContinuousMoveAsync(_tcamera.Profile.token, ptzSpeed, string.Empty).Wait();
					}

				if (UpdateZoom(zoomLast, CameraZoom, out float zoom))
					if (Math.Abs(zoom) < 0.1f)
					{
						ComSleepTillCanRequest();
						_tcamera.Ptz.StopAsync(_tcamera.Profile.token, false, true).Wait();
					}
					else
					{
						PTZSpeed ptzSpeed = new()
						{
							Zoom = new()
							{
								x = zoom
							}
						};
						ComSleepTillCanRequest();
						_tcamera.Ptz.ContinuousMoveAsync(_tcamera.Profile.token, ptzSpeed, string.Empty).Wait();
					}

				motionLast = CameraMotion;
				zoomLast = CameraZoom;
			}

			//closeup
			State = CommunicationState.Closing;
			{
				_tcamera = null; // no close idk, and closing internals is dumb idea
			}


			//closed
			State = CommunicationState.Closed;
			_threadBarrier.RemoveParticipant();
		}

		/// <summary>
		/// Meant to be used by ThreadWork ONLY <br/>
		/// </summary>
		private static bool UpdateMotion(CameraMotionState old, CameraMotionState @new, out Vector2 speed)
		{
			speed = new Vector2(0.0f);
			if (@new.Equals(old))
				return false;

			switch (@new & CameraMotionState.PanRL_Lock)
			{
				case CameraMotionState.PanRight:
					speed.X = 1.0f;
					break;
				case CameraMotionState.PanLeft:
					speed.X = -1.0f;
					break;
				default:
					//no change on lock or none
					break;
			}

			switch (@new & CameraMotionState.PanUD_Lock)
			{
				case CameraMotionState.PanUp:
					speed.Y = 1.0f;
					break;
				case CameraMotionState.PanDown:
					speed.Y = -1.0f;
					break;
				default:
					//no change on lock or none
					break;
			}

			//speed = Vector2.Normalize(speed);
			return true;
		}

		/// <summary>
		/// Meant to be used by ThreadWork ONLY <br/>
		/// </summary>
		private static bool UpdateZoom(CameraZoomState old, CameraZoomState @new, out float zoom)
		{
			zoom = 0.0f;
			if (@new.Equals(old))
				return false;

			zoom = @new switch
			{
				CameraZoomState.ZoomIn => 1.0f,
				CameraZoomState.ZoomOut => -1.0f,
				_ => 0.0f
			};

			return true;
		}

		/// <summary>
		/// Meant to be used by ThreadWork ONLY <br/>
		/// </summary>
		private void ComSleepTillCanRequest()
		{
			//check if limit not passed
			while (_lastComTimeStamp + MinSpanEveryCom > System.DateTime.Now)
			{
				//go sleep
				Thread.Sleep(69);
				////go work
				//foreach (var val in _lastComTimeStamp.ToList())
				//{
				//	if (System.DateTime.Now - val > MinSpanEveryCom)
				//		_lastComTimeStamp.Remove(val);
				//}
			}

			_lastComTimeStamp = System.DateTime.Now;
		}


	}
}
