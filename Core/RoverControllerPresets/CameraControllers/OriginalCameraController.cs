using System.Collections.Generic;

using Godot;

using static RoverControlApp.Core.MqttClasses;

namespace RoverControlApp.Core.RoverControllerPresets.CameraControllers;

public class OriginalCameraController : ICameraController
{

    private static readonly StringName[] _usedActions =
    [
        RcaInEvName.CameraMoveLeft,
        RcaInEvName.CameraMoveRight,
        RcaInEvName.CameraMoveDown,
        RcaInEvName.CameraMoveUp,
        RcaInEvName.CameraZoomOut,
        RcaInEvName.CameraZoomIn,
        RcaInEvName.CameraFocusOut,
        RcaInEvName.CameraFocusIn,
    ];

    public Vector4 CalculateMoveVector(in InputEvent inputEvent, DualSeatEvent.InputDevice targetInputDevice, in Vector4 lastState)
    {
        Vector4 absoluteVector4 = Vector4.Zero;

        Vector2 velocity = Input.GetVector(DualSeatEvent.GetName(RcaInEvName.CameraMoveLeft, targetInputDevice), DualSeatEvent.GetName(RcaInEvName.CameraMoveRight, targetInputDevice), DualSeatEvent.GetName(RcaInEvName.CameraMoveDown, targetInputDevice), DualSeatEvent.GetName(RcaInEvName.CameraMoveUp, targetInputDevice));
        velocity = velocity.Clamp(new Vector2(-1f, -1f), new Vector2(1f, 1f));
        absoluteVector4.X = Mathf.IsEqualApprox(velocity.X, 0f, Mathf.Max(0.1f, LocalSettings.Singleton.Joystick.MinimalInput)) ? 0 : velocity.X;
        absoluteVector4.Y = Mathf.IsEqualApprox(velocity.Y, 0f, Mathf.Max(0.1f, LocalSettings.Singleton.Joystick.MinimalInput)) ? 0 : velocity.Y;
        velocity = Input.GetVector(DualSeatEvent.GetName(RcaInEvName.CameraZoomOut, targetInputDevice), DualSeatEvent.GetName(RcaInEvName.CameraZoomIn, targetInputDevice), DualSeatEvent.GetName(RcaInEvName.CameraFocusOut, targetInputDevice), DualSeatEvent.GetName(RcaInEvName.CameraFocusIn, targetInputDevice));
        absoluteVector4.Z = Mathf.IsEqualApprox(velocity.X, 0f, Mathf.Max(0.1f, LocalSettings.Singleton.Joystick.MinimalInput)) ? 0 : velocity.X;
        absoluteVector4.W = Mathf.IsEqualApprox(velocity.Y, 0f, Mathf.Max(0.1f, LocalSettings.Singleton.Joystick.MinimalInput)) ? 0 : velocity.Y;

        return absoluteVector4.Clamp(new Vector4(-1f, -1f, -1f, -1f), new Vector4(1f, 1f, 1f, 1f));
    }

    public Dictionary<StringName, Godot.Collections.Array<InputEvent>> GetInputActions() =>
        IActionAwareController.FetchAllActionEvents(_usedActions);
    }
