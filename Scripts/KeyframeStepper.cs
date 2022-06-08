using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public class KeyframeStepper
{
	public List<Keyframe> keyframes;
	public List<float> timeframes;
	public float time;
	public int current;
	public float numframes;
	
	public KeyframeStepper(IEnumerable<Keyframe> frames, Vector2 offset, float numframes)
	{
		this.numframes = numframes;
		keyframes = new List<Keyframe>();
		timeframes = new List<float>();
		foreach(var keyframe in frames)
		{
			var temp_key = keyframe.frame;
			var temp_pos = keyframe.position + offset;
			var temp_center = keyframe.center + offset;
			if(temp_key == -1)
			{
				temp_key = temp_pos.x;
				temp_pos = keyframes.Last().position + offset;
				temp_center = keyframes.Last().center + offset;
			}
			
			keyframes.Add(new Keyframe(temp_key, numframes, temp_pos, keyframe.hasCenter, temp_center));
			timeframes.Add(temp_key);
		}
		
		time = 0;
		current = 0;
	}
	
	public void AdvanceTime(float t)
	{
		time += t;
		time %= numframes;
		current = timeframes.BinarySearch(time);
		if(current < 0) current = ~current;
		if(current == keyframes.Count) current = 0;
	}
	
	public Vector2 GetCurrent()
	{
		var prev = current-1;
		if(prev == -1) prev = keyframes.Count-1;
		return keyframes[prev].StepTowards(keyframes[current], time);
		/*var posdiff = positions[current]-positions[prev];
		
		var timediff = (keyframes[current]-keyframes[prev]);
		if(timediff < 0) timediff += numframes;
		timediff %= numframes;
		
		if(timediff == 0) return positions[prev];
		
		var speed = posdiff/timediff;
		
		var partialdiff = time-keyframes[prev];
		if(partialdiff < 0) partialdiff += numframes;
		partialdiff %= numframes;
		
		var dist = speed*partialdiff;
		return positions[prev]+dist;*/
	}
}
