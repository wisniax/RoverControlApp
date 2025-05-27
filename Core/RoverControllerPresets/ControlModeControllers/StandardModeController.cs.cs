using System;

using Godot;
using Godot.Collections;

using static RoverControlApp.Core.MqttClasses;

namespace RoverControlApp.Core.RoverControllerPresets.ControlModeControllers;

public class StandardModeController : IControlModeController
{
    private const ulong _fastSelectTime = 300;
    private int _fastSelectCount = 0;
    private ulong _fastSelectLastPress = 0;

    private static readonly string[] _usedActions =
    [
        "controlmode_estop",
        "controlmode_change",
    ];

    public bool EstopReq()
    {
        return Input.IsActionJustPressed("controlmode_estop", exactMatch: true);
    }

    public ControlMode GetControlMode(in InputEvent inputEvent, in ControlMode lastState)
    {
        ControlMode newState = lastState;

        if (inputEvent.IsActionPressed("controlmode_change", exactMatch: true))
        {
            if (Time.GetTicksMsec() - _fastSelectLastPress < _fastSelectTime)
            {
                _fastSelectCount++;
            }
            else
            {
                _fastSelectCount = 0;
            }

            _fastSelectLastPress = Time.GetTicksMsec();

            if (_fastSelectCount > 0)
            {
                newState = (ControlMode)(_fastSelectCount % Enum.GetNames<ControlMode>().Length);
            }
            else
            {
                if ((int)lastState + 1 >= Enum.GetNames<ControlMode>().Length)
                    newState = ControlMode.Rover;
                else
                    newState++;
            }
        }
        return newState;
    }

    public System.Collections.Generic.Dictionary<string, Array<InputEvent>> GetInputActions() =>
        IActionAwareController.FetchAllActionEvents(_usedActions);
}