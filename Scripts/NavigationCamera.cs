using Godot;
using System;

public partial class NavigationCamera : Camera2D
{
    public ConfigReader cf = null;
    
    public float fitoffset => (cf is null)?0f:cf.Others["FitOffset"].AsSingle();

    public const float CAMERA_ACCELERATION = 40f;
    public const float CAMERA_SPEED = 400f;
    public const float MAXZOOM_IN = 0f;
    public const float MAXZOOM_OUT = 30f;
    public const float ZOOM_INTERVAL = 0.1f;
    
    public override void _Process(double delta)
    {
        Zoom = Vector2.One/Zoom;
        var speedmult = (Zoom.X/10f);
        
        var offsetVector = Vector2.Zero;
        if(Input.IsActionPressed("ui_left")) offsetVector -= speedmult*CAMERA_ACCELERATION*Vector2.Right;
        if(Input.IsActionPressed("ui_right")) offsetVector += speedmult*CAMERA_ACCELERATION*Vector2.Right;
        if(Input.IsActionPressed("ui_up")) offsetVector -= speedmult*CAMERA_ACCELERATION*Vector2.Down;
        if(Input.IsActionPressed("ui_down")) offsetVector += speedmult*CAMERA_ACCELERATION*Vector2.Down;
        offsetVector.X = Math.Max(-CAMERA_SPEED, Math.Min(offsetVector.X, speedmult*CAMERA_SPEED));
        offsetVector.Y = Math.Max(-CAMERA_SPEED, Math.Min(offsetVector.Y, speedmult*CAMERA_SPEED));
        Offset += offsetVector;
        
        var zoomFactor = 0f;
        if(Input.IsActionPressed("zoom_in")) zoomFactor -= speedmult*ZOOM_INTERVAL;
        if(Input.IsActionPressed("zoom_out")) zoomFactor += speedmult*ZOOM_INTERVAL;
        Zoom += zoomFactor*Vector2.One;
        var zoomx = Math.Max(MAXZOOM_IN, Math.Min(Zoom.X, MAXZOOM_OUT));
        var zoomy = Math.Max(MAXZOOM_IN, Math.Min(Zoom.Y, MAXZOOM_OUT));
        Zoom = new Vector2(zoomx, zoomy);
        Zoom = Vector2.One/Zoom;
    }
    
    public void FitToRect(Rect2 rect)
    {
        Offset = rect.GetCenter();
        var cameraZoomXY = rect.Size/GetViewportRect().Size;
        Zoom = Vector2.One/(Math.Max(cameraZoomXY.X, cameraZoomXY.Y)+fitoffset);
    }
}
