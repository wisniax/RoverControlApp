using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Godot;
using RoverControlApp.MVVM.ViewModel;

namespace RoverControlApp.Core
{
	public class RoverControllerPresets
	{
		public interface IRoverDriveController
		{
			public bool CalculateMoveVector(out MqttClasses.RoverControl roverControl);
		}

		public interface IRoverManipulatorController
		{
			public bool CalculateMoveVector(out MqttClasses.ManipulatorControl manipulatorControl);
		}

		public class ForzaLikeController : IRoverDriveController
		{
			public bool CalculateMoveVector(out MqttClasses.RoverControl roverControl)
			{
				roverControl = new MqttClasses.RoverControl();

				Vector2 velocity = Input.GetVector("rover_move_left", "rover_move_right", "rover_move_backward",
					"rover_move_forward", Mathf.Max(0.1f, MainViewModel.Settings.Settings.JoyPadDeadzone));
				velocity.X = Mathf.IsEqualApprox(velocity.X, 0f, Mathf.Max(0.1f, MainViewModel.Settings.Settings.JoyPadDeadzone)) ? 0 : velocity.X;
				velocity.Y = Mathf.IsEqualApprox(velocity.Y, 0f, 0.005f) ? 0 : velocity.Y;
				velocity = velocity.Clamp(new Vector2(-Math.Abs(velocity.Y) / 2.5f, -1f), new Vector2(Math.Abs(velocity.Y) / 2.5f, 1f)); // Max turn angle: 36 deg.
				float forcedX = Input.GetAxis("rover_rotate_left", "rover_rotate_right");
				if (!Mathf.IsEqualApprox(forcedX, 0f, 0.05f)) velocity.X = forcedX;

				if (Input.IsActionPressed("camera_zoom_mod"))
				{
					velocity.X /= 8f;
					velocity.Y /= 8f;
				}

				var oldVelocity = new Vector2((float)MainViewModel.PressedKeys.RoverMovement.ZRotAxis,
					(float)MainViewModel.PressedKeys.RoverMovement.XVelAxis);
				if (oldVelocity.IsEqualApprox(velocity)) return false;


				roverControl.ZRotAxis = velocity.X;
				roverControl.XVelAxis = velocity.Y;
				return true;
			}
		}

		public class GoodOldGamesLikeController : IRoverDriveController
		{
			public bool CalculateMoveVector(out MqttClasses.RoverControl roverControl)
			{
				roverControl = new MqttClasses.RoverControl();

				Vector2 velocity = Input.GetVector("rover_move_left", "rover_move_right", "rover_move_down",
					"rover_move_up", Mathf.Max(0.1f, MainViewModel.Settings.Settings.JoyPadDeadzone));
				// velocity = velocity.Clamp(new Vector2(-1f, -1f), new Vector2(1f, 1f));
				velocity.X = Mathf.IsEqualApprox(velocity.X, 0f, Mathf.Max(0.1f, MainViewModel.Settings.Settings.JoyPadDeadzone)) ? 0 : velocity.X;
				velocity.Y = Mathf.IsEqualApprox(velocity.Y, 0f, Mathf.Max(0.1f, MainViewModel.Settings.Settings.JoyPadDeadzone)) ? 0 : velocity.Y;
				if (Input.IsActionPressed("camera_zoom_mod"))
				{
					velocity.X /= 8f;
					velocity.Y /= 8f;
				}
				if (new Vector2((float)MainViewModel.PressedKeys.RoverMovement.ZRotAxis, (float)MainViewModel.PressedKeys.RoverMovement.XVelAxis)
					.IsEqualApprox(velocity)) return false;


				roverControl.ZRotAxis = velocity.X;
				roverControl.XVelAxis = velocity.Y;
				return true;
			}
		}

		public class SingleAxisManipulatorController : IRoverManipulatorController
		{
			public bool CalculateMoveVector(out MqttClasses.ManipulatorControl manipulatorControl)
			{
				manipulatorControl = new();

				float velocity = Input.GetAxis("manipulator_speed_backward", "manipulator_speed_forward");

				manipulatorControl = new MqttClasses.ManipulatorControl()
				{
					Axis1 = Input.IsActionPressed("manipulator_axis_1") ? velocity : 0f,
					Axis2 = Input.IsActionPressed("manipulator_axis_2") ? velocity : 0f,
					Axis3 = Input.IsActionPressed("manipulator_axis_3") ? velocity : 0f,
					Axis4 = Input.IsActionPressed("manipulator_axis_4") ? velocity : 0f,
					Axis5 = Input.IsActionPressed("manipulator_axis_5") ? velocity : 0f,
					Gripper = Input.IsActionPressed("manipulator_gripper") ? velocity : 0f
				};

				return !manipulatorControl.Equals(MainViewModel.PressedKeys.ManipulatorMovement);
			}
		}
	}
}
