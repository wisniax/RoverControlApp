using Godot;
using System;

public partial class KeyShow : Control
{
    [Export()]
    public float speed = 10.0f;

    Vector2 dir;

    TextureRect up;
    TextureRect down;
    TextureRect right;
    TextureRect left;
    public override void _Ready()
    {
        up = GetNode<TextureRect>("up");
        down = GetNode<TextureRect>("down");
        right = GetNode<TextureRect>("right");
        left = GetNode<TextureRect>("left");

        foreach (var child in GetChildren())
            if (child is TextureRect tchild)
            {
                tchild.Visible = false;
            }
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta)
    {
        Position += dir.Normalized() * speed * (float)delta;


        if(Input.IsKeyPressed(Key.R)) 
        {
            Position = new Vector2(10f, 10f);
        }
    }

    public override void _Input(InputEvent @event)
    {

        //up
        if (@event.IsActionPressed("cam_up"))
        {
            up.Visible = true;
            dir += Vector2.Up;
            GD.Print("UP_PRESS");
        }
        else if (@event.IsActionReleased("cam_up"))
        {
            up.Visible = false;
            dir -= Vector2.Up;
            GD.Print("UP_RELEASE");
        }

        //down
        if (@event.IsActionPressed("cam_down"))
        {
            down.Visible = true;
            dir += Vector2.Down;
            GD.Print("DOWN_PRESS");
        }
        else if (@event.IsActionReleased("cam_down"))
        {
            down.Visible = false;
            dir -= Vector2.Down;
            GD.Print("DOWN_RELEASE");
        }

        //right
        if (@event.IsActionPressed("cam_right"))
        {
            right.Visible = true;
            dir += Vector2.Right;
            GD.Print("RIGHT_PRESS");
        }
        else if (@event.IsActionReleased("cam_right"))
        {
            right.Visible = false;
            dir -= Vector2.Right;
            GD.Print("RIGHT_RELEASE");
        }

        //left
        if (@event.IsActionPressed("cam_left"))
        {
            left.Visible = true;
            dir += Vector2.Left;
            GD.Print("LEFT_PRESS");
        }
        else if (@event.IsActionReleased("cam_left"))
        {
            left.Visible = false;
            dir -= Vector2.Left;
            GD.Print("LEFT_RELEASE");
        }

    }
}
