using Onvif.Core.Client.Common;
using Onvif.Core.Client;
using System.ServiceModel;
using Godot;
using Microsoft.VisualBasic;
using Mutex = System.Threading.Mutex;

namespace OnvifCameraControlTest
{
	public class OnvifCameraThreadController
	{
		public TimeSpan MinSpanEveryCom = new(0, 0, 0, 0, 550);

		public TimeSpan MaxSpanEveryCom => 1.5 * MinSpanEveryCom;


		/// <summary>
		/// Should X and Y axis be inverted?
		/// </summary>
		public bool InvertControl { get; set; } = false;

		/// <summary>
		/// Meant to be used by ThreadWork ONLY <br/>
		/// </summary>
		private System.DateTime _lastComTimeStamp = System.DateTime.Now;

		private readonly Mutex _dataMutex = new();
		private readonly Barrier _threadBarrier = new(1);

		public void ChangeMoveVector(object sender, Vector3 vector3)
		{
			CameraMotion = vector3;
		}

		/// <summary>
		/// Current camera motion operation.
		/// </summary>
		public Vector3 CameraMotion
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
				//_threadBarrier.SignalAndWait(0);
			}
		}

		private Vector3 _cameraMotion = Vector3.Zero;

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
		/// <param name="cameraPtzUrl"></param>
		/// <param name="user"></param>
		/// <param name="password"></param>
		/// <returns><see cref="CommunicationState.Faulted"/> when there was error during startup, otherwise <see cref="CommunicationState.Opened"/></returns>
		/// <exception cref="InvalidOperationException"></exception>
		public void Start(string cameraPtzUrl, string user, string password)
		{
			if (_thread != null)
				throw new InvalidOperationException("Thread is still valid");

			State = CommunicationState.Created;
			_threadError = null;
			_thread = new Thread(ThreadWork) { IsBackground = true };

			var acc = new Account(cameraPtzUrl, user, password);
			_thread.Start(acc);

			//wait till startup completed

			//while (State is CommunicationState.Created or CommunicationState.Opening) await Task.Delay(69);

			//check health
			//if (State == CommunicationState.Faulted)
			//	_thread = null;

			//return State;
		}

		public void Stop()
		{
			State = CommunicationState.Closing;
			_threadBarrier.SignalAndWait(0);
			_thread = null;
		}

		private void ThreadWork(object? obj)
		{
			_threadBarrier.AddParticipant();

			//start up
			State = CommunicationState.Created;
			{
				_tcamera = Camera.Create((Account)obj!, (e) => _threadError = e);

				if (_threadError is not null)
				{
					State = CommunicationState.Faulted;
					_threadBarrier.RemoveParticipant();
					return;
				}
			}
			State = CommunicationState.Opening;

			_tcamera.Ptz.StopAsync(_tcamera.Profile.token, true, true).Wait();

			//camera operation loop pre
			State = CommunicationState.Opened;

			_dataMutex.WaitOne();
			Vector3 motionLast = _cameraMotion = Vector3.Zero;
			//CameraZoomState zoomLast = _cameraZoom = CameraZoomState.None;
			_dataMutex.ReleaseMutex();

			//camera operation loop
			while (State == CommunicationState.Opened)
			{
				//_threadBarrier.SignalAndWait();
				ComSleepTillCanRequest();
				if (!UpdateMotion(motionLast, CameraMotion, out Vector3 moveVector3)) continue;

				bool stopTilt = Mathf.IsEqualApprox(moveVector3.X, 0f, 0.1f) || Mathf.IsEqualApprox(moveVector3.Y, 0f, 0.1f);
				bool stopZoom = Mathf.IsEqualApprox(moveVector3.Z, 0f, 0.1f);

				//Check whether those WERE stopped previously
				//stopTilt &= !(Mathf.IsEqualApprox(motionLast.X, 0f, 0.1f) || Mathf.IsEqualApprox(motionLast.Y, 0f, 0.1f));
				stopZoom &= !Mathf.IsEqualApprox(motionLast.Z, 0f, 0.1f);

				//var cus = _tcamera.Ptz.GetStatusAsync(_tcamera.Profile.token).Result;

				if (stopTilt || stopZoom)
				{
					_tcamera.Ptz.StopAsync(_tcamera.Profile.token, stopTilt, stopZoom).Wait();
					//ComRequestSleep();
				}

				if (!moveVector3.IsZeroApprox())
				{
					PTZSpeed ptzSpeed = new()
					{
						PanTilt = new()
						{
							x = moveVector3.X,
							y = moveVector3.Y
						},
						Zoom = new()
						{
							x = moveVector3.Z
						}
					};
					ComSleepTillCanRequest();
					_tcamera.Ptz.ContinuousMoveAsync(_tcamera.Profile.token, ptzSpeed, string.Empty).Wait();
					//ComRequestSleep();
				}
				ComRequestSleep();

				motionLast = CameraMotion;
			}

			//closeup
			State = CommunicationState.Closing;
			{
				_tcamera.Ptz.Close();
				_tcamera = null; // no close idk, and closing internals is dumb idea
			}


			//closed
			State = CommunicationState.Closed;
			_threadBarrier.RemoveParticipant();
		}

		/// <summary>
		/// Meant to be used by ThreadWork ONLY <br/>
		/// </summary>
		private bool UpdateMotion(Vector3 old, Vector3 @new, out Vector3 speed)
		{
			speed = Vector3.Zero;
			if (@new.IsEqualApprox(old) && ((_lastComTimeStamp + MaxSpanEveryCom > System.DateTime.Now)))
			{
				Thread.Sleep(100);
				return false;
			}

			speed = InvertControl ? new Vector3(-@new.X, -@new.Y, @new.Z) : @new;

			//Have to make sure none scalar is |x| <= 0.1f bc camera treats it as a MAX SPEED
			if (Mathf.IsEqualApprox(speed.X, 0f, 0.1f)) speed.X = 0f;
			if (Mathf.IsEqualApprox(speed.Y, 0f, 0.1f)) speed.Y = 0f;
			if (Mathf.IsEqualApprox(speed.Z, 0f, 0.1f)) speed.Z = 0f;

			speed = speed.Clamp(new Vector3(-1f, -1f, -1f), new Vector3(1f, 1f, 1f));

			//speed = Vector2.Normalize(speed);
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
		}

		private void ComRequestSleep()
		{
			_lastComTimeStamp = System.DateTime.Now;
		}


	}
}
