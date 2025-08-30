using System.Collections.Generic;

using Godot;

using static RoverControlApp.Core.MqttClasses;

namespace RoverControlApp.Core.RoverControllerPresets.CameraControllers;

public class OriginalCameraController : ICameraController
{
    private static readonly string[] _usedActions =
    [
        "camera_move_left",
        "camera_move_right",
        "camera_move_down",
        "camera_move_up",
        "camera_zoom_out",
        "camera_zoom_in",
        "camera_focus_out",
        "camera_focus_in"
    ];

    public Vector4 CalculateMoveVector(in string actionSurffix, in InputEvent inputEvent, in Vector4 lastState)
    {
        Vector4 absoluteVector4 = Vector4.Zero;

        Vector2 velocity = Input.GetVector("camera_move_left" + actionSurffix, "camera_move_right" + actionSurffix, "camera_move_down" + actionSurffix, "camera_move_up" + actionSurffix);
        velocity = velocity.Clamp(new Vector2(-1f, -1f), new Vector2(1f, 1f));
        absoluteVector4.X = Mathf.IsEqualApprox(velocity.X, 0f, Mathf.Max(0.1f, LocalSettings.Singleton.Joystick.MinimalInput)) ? 0 : velocity.X;
        absoluteVector4.Y = Mathf.IsEqualApprox(velocity.Y, 0f, Mathf.Max(0.1f, LocalSettings.Singleton.Joystick.MinimalInput)) ? 0 : velocity.Y;
        velocity = Input.GetVector("camera_zoom_out" + actionSurffix, "camera_zoom_in" + actionSurffix, "camera_focus_out" + actionSurffix, "camera_focus_in" + actionSurffix);
        absoluteVector4.Z = Mathf.IsEqualApprox(velocity.X, 0f, Mathf.Max(0.1f, LocalSettings.Singleton.Joystick.MinimalInput)) ? 0 : velocity.X;
        absoluteVector4.W = Mathf.IsEqualApprox(velocity.Y, 0f, Mathf.Max(0.1f, LocalSettings.Singleton.Joystick.MinimalInput)) ? 0 : velocity.Y;

        return absoluteVector4.Clamp(new Vector4(-1f, -1f, -1f, -1f), new Vector4(1f, 1f, 1f, 1f));
    }

    public Dictionary<string, Godot.Collections.Array<InputEvent>> GetInputActions() =>
        IActionAwareController.FetchAllActionEvents(_usedActions);
    }
