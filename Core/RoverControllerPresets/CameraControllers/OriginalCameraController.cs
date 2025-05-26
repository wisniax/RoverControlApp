using Godot;

using static RoverControlApp.Core.MqttClasses;

namespace RoverControlApp.Core.RoverControllerPresets.CameraControllers;

public class OriginalCameraController : ICameraController
{
    public Vector4 CalculateMoveVector(in InputEvent inputEvent, in Vector4 lastState)
    {
        Vector4 absoluteVector4 = Vector4.Zero;

		Vector2 velocity = Input.GetVector("camera_move_left", "camera_move_right", "camera_move_down", "camera_move_up");
		velocity = velocity.Clamp(new Vector2(-1f, -1f), new Vector2(1f, 1f));
		absoluteVector4.X = Mathf.IsEqualApprox(velocity.X, 0f, Mathf.Max(0.1f, LocalSettings.Singleton.Joystick.Deadzone)) ? 0 : velocity.X;
		absoluteVector4.Y = Mathf.IsEqualApprox(velocity.Y, 0f, Mathf.Max(0.1f, LocalSettings.Singleton.Joystick.Deadzone)) ? 0 : velocity.Y;
		velocity = Input.GetVector("camera_zoom_out", "camera_zoom_in", "camera_focus_out", "camera_focus_in");
		absoluteVector4.Z = Mathf.IsEqualApprox(velocity.X, 0f, Mathf.Max(0.1f, LocalSettings.Singleton.Joystick.Deadzone)) ? 0 : velocity.X;
		absoluteVector4.W = Mathf.IsEqualApprox(velocity.Y, 0f, Mathf.Max(0.1f, LocalSettings.Singleton.Joystick.Deadzone)) ? 0 : velocity.Y;

		return absoluteVector4.Clamp(new Vector4(-1f, -1f, -1f, -1f), new Vector4(1f, 1f, 1f, 1f));
    }
}