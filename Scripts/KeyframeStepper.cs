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
		keyframes = new();
		timeframes = new();
		foreach(var keyframe in frames)
		{
			var temp_key = keyframe.frame;
			var temp_pos = keyframe.position + offset;
			var temp_center = keyframe.center + offset;
			if(temp_key == -1)
			{
				temp_key = temp_pos.X;
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
		while(time < 0) time += numframes;
		time %= numframes;
		current = timeframes.BinarySearch(time);
		if(current < 0) current = ~current;
		if(current == keyframes.Count) current = 0;
	}

	public Keyframe GetUsedKeyframe()
	{
		var prev = current-1;
		if(prev == -1) prev = keyframes.Count-1;
		return keyframes[prev];
	}
	
	public Vector2 GetCurrent() => GetUsedKeyframe().StepTowards(keyframes[current], time);
}
