using Godot;
using System;

public class NavigationCamera : Camera2D
{
	public ConfigReader cf = null;
	
	public float fitoffset => (cf is null)?0f:float.Parse(cf.Others["FitOffset"].ToString());
	
	public const float CAMERA_ACCELERATION = 40f;
	public const float CAMERA_SPEED = 400f;
	public const float MAXZOOM_IN = 0.1f;
	public const float MAXZOOM_OUT = 30f;
	public const float ZOOM_INTERVAL = 0.1f;
	
	public override void _PhysicsProcess(float delta)
	{
		var speedmult = (Zoom.x/10f);
		
		var offsetVector = Vector2.Zero;
		if(Input.IsActionPressed("ui_left")) offsetVector -= speedmult*CAMERA_ACCELERATION*Vector2.Right;
		if(Input.IsActionPressed("ui_right")) offsetVector += speedmult*CAMERA_ACCELERATION*Vector2.Right;
		if(Input.IsActionPressed("ui_up")) offsetVector -= speedmult*CAMERA_ACCELERATION*Vector2.Down;
		if(Input.IsActionPressed("ui_down")) offsetVector += speedmult*CAMERA_ACCELERATION*Vector2.Down;
		offsetVector.x = Math.Max(-CAMERA_SPEED, Math.Min(offsetVector.x, speedmult*CAMERA_SPEED));
		offsetVector.y = Math.Max(-CAMERA_SPEED, Math.Min(offsetVector.y, speedmult*CAMERA_SPEED));
		Offset += offsetVector;
		
		var zoomFactor = 0f;
		if(Input.IsActionPressed("zoom_in")) zoomFactor += speedmult*ZOOM_INTERVAL;
		if(Input.IsActionPressed("zoom_out")) zoomFactor -= speedmult*ZOOM_INTERVAL;
		Zoom += zoomFactor*Vector2.One;
		var zoomx = Math.Max(MAXZOOM_IN, Math.Min(Zoom.x, MAXZOOM_OUT));
		var zoomy = Math.Max(MAXZOOM_IN, Math.Min(Zoom.y, MAXZOOM_OUT));
		Zoom = new Vector2(zoomx, zoomy);
	}
	
	public void FitToRect(Rect2 rect)
	{
		Offset = rect.Center();
		var cameraZoomXY = rect.Size/GetViewportRect().Size;
		Zoom = (Math.Max(cameraZoomXY.x, cameraZoomXY.y)+fitoffset)*Vector2.One;
	}
}
