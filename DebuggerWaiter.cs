using Godot;
using System;
using System.Diagnostics;
using System.Threading;


public partial class DebuggerWaiter : Node
{
    public override void _Ready()
    {
        //Disable wait when not in debug
#if !DEBUG
            //Die because u useless
            QueueFree();
            return;
#endif
        DateTime start = DateTime.Now;
        while (!Debugger.IsAttached)
        {
            Thread.Sleep(100);
            if ((DateTime.Now - start).TotalSeconds > 5)
                break;
        }
        if (Debugger.IsAttached)
            GD.Print("Debugger attached!");
        else
            GD.PrintErr("Debugger attach failed (timeout)");

        //commit suicide when no one looking
        QueueFree();
    }
}