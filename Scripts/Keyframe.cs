using Godot;
using System;

public struct Keyframe
{
	public readonly float frame;
	public readonly Vector2 position;
	public readonly bool hasCenter;
	public readonly Vector2 center;
	public readonly float numframes;
	
	public Keyframe(float frame, float numframes, Vector2 position, bool hasCenter, Vector2 center)
	{
		this.frame = frame;
		this.numframes = numframes;
		this.position = position;
		this.hasCenter = hasCenter;
		this.center = center;
	}
	
	public Vector2 StepTowards(Keyframe other, float time)
	{
		var framediff = other.frame - frame;
		while(framediff < 0) framediff += numframes;
		framediff %= numframes;
		if(framediff == 0) return other.position;
		
		var timediff = time-frame;
		while(timediff < 0) timediff += numframes;
		timediff %= numframes;
		if(timediff == 0) return position;
		
		var weight = timediff/framediff;
		
		if(hasCenter)
		{
			var rp1 = (position-center).Normalized();
			var rp2 = (other.position-center).Normalized();
			var lp = rp1.Slerp(rp2, weight);
			var diff = (position-other.position).Abs();
			return diff*lp + center;
		}
		else
		{
			return position.Lerp(other.position, weight);
		}
	}
}
