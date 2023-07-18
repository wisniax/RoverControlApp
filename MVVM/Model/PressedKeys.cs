﻿using Godot;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Onvif.Core.Client.Common;
using RoverControlApp.Core;
using RoverControlApp.MVVM.ViewModel;

namespace RoverControlApp.MVVM.Model
{
	public class PressedKeys
	{
		public event EventHandler<Vector4> OnAbsoluteVectorChanged;
		public event EventHandler<MqttClasses.RoverControl> OnRoverMovementVector;
		public event Func<bool, Task> OnPadConnectionChanged;
		public event Func<MqttClasses.ControlMode, Task>? OnControlModeChanged;

		private volatile MqttClasses.ControlMode _controlMode;
		public MqttClasses.ControlMode ControlMode
		{
			get => _controlMode;
			private set => _controlMode = value;
		}

		public bool PadConnected
		{
			get => Input.GetConnectedJoypads().Count > 0;
			private set
			{
				OnPadConnectionChanged.Invoke(value);
				HandleInputEvent(null!);
			}
		}

		private Vector4 _lastAbsoluteVector;
		public Vector4 LastAbsoluteVector
		{
			get => _lastAbsoluteVector;
			private set
			{
				_lastAbsoluteVector = value;
				OnAbsoluteVectorChanged?.Invoke(this, value);
			}
		}

		private MqttClasses.RoverControl _roverMovement;
		public MqttClasses.RoverControl RoverMovement
		{
			get => _roverMovement;
			private set
			{
				_roverMovement = value;
				OnRoverMovementVector?.Invoke(this, value);
			}
		}

		private RoverControllerPresets.IRoverDriveController _roverDriveControllerPreset;

		public PressedKeys()
		{
			Input.JoyConnectionChanged += (device, connected) => { PadConnected = connected; };
			_lastAbsoluteVector = Vector4.Zero;
			_roverMovement = new MqttClasses.RoverControl();
			_roverDriveControllerPreset = MainViewModel.Settings.Settings.NewFancyRoverController
				? new RoverControllerPresets.ForzaLikeController()
				: new RoverControllerPresets.GoodOldGamesLikeController();
		}

		public void HandleInputEvent(InputEvent @event)
		{
			HandleFunctionInputEvent();
			HandleCameraInputEvent();
			HandleMovementInputEvent();
		}

		private void HandleCameraInputEvent()
		{
			if (ControlMode == MqttClasses.ControlMode.Manipulator) return;

			Vector4 absoluteVector4 = Vector4.Zero;

			Vector2 velocity = Input.GetVector("camera_move_left", "camera_move_right", "camera_move_down", "camera_move_up");
			velocity = velocity.Clamp(new Vector2(-1f, -1f), new Vector2(1f, 1f));
			absoluteVector4.X = Mathf.IsEqualApprox(velocity.X, 0f, Mathf.Max(0.1f, MainViewModel.Settings.Settings.JoyPadDeadzone)) ? 0 : velocity.X;
			absoluteVector4.Y = Mathf.IsEqualApprox(velocity.Y, 0f, Mathf.Max(0.1f, MainViewModel.Settings.Settings.JoyPadDeadzone)) ? 0 : velocity.Y;
			velocity = Input.GetVector("camera_zoom_out", "camera_zoom_in", "camera_focus_out", "camera_focus_in");
			absoluteVector4.Z = Mathf.IsEqualApprox(velocity.X, 0f, Mathf.Max(0.1f, MainViewModel.Settings.Settings.JoyPadDeadzone)) ? 0 : velocity.X;
			absoluteVector4.W = Mathf.IsEqualApprox(velocity.Y, 0f, Mathf.Max(0.1f, MainViewModel.Settings.Settings.JoyPadDeadzone)) ? 0 : velocity.Y;

			if (Input.IsActionPressed("camera_zoom_mod"))
			{
				absoluteVector4.X /= 8f;
				absoluteVector4.Y /= 8f;
			}

			absoluteVector4 = absoluteVector4.Clamp(new Vector4(-1f, -1f, -1f, -1f), new Vector4(1f, 1f, 1f, 1f));
			LastAbsoluteVector = absoluteVector4;
		}

		private void HandleMovementInputEvent()
		{
			if (ControlMode != MqttClasses.ControlMode.Rover) return;
			if (!_roverDriveControllerPreset.CalculateMoveVector(out MqttClasses.RoverControl roverControl)) return;
			RoverMovement = roverControl;
		}
		private void HandleFunctionInputEvent()
		{
			HandleControlModeChange();
		}

		private void HandleControlModeChange()
		{
			if (!Input.IsActionJustPressed("ControlModeChange", true)) return;

			if ((int)ControlMode + 1 >= Enum.GetNames<MqttClasses.ControlMode>().Length)
				ControlMode = MqttClasses.ControlMode.EStop;
			else ControlMode++;

			StopAll();

			OnControlModeChanged?.Invoke(ControlMode);
		}

		private void StopAll()
		{
			RoverMovement = new MqttClasses.RoverControl() { XVelAxis = 0, ZRotAxis = 0 };
			LastAbsoluteVector = Vector4.Zero;
		}
	}
}