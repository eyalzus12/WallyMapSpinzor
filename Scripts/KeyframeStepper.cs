using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public class KeyframeStepper
{
	public List<float> keyframes;
	public List<Vector2> positions;
	public float time;
	public int current;
	
	public KeyframeStepper(IEnumerable<(float, Vector2)> frames, float numframes)
	{
		keyframes = new List<float>();
		positions = new List<Vector2>();
		
		foreach((float key, Vector2 position) in frames)
		{
			var temp_key = key;
			var temp_pos = position;
			if(temp_key == -1)
			{
				temp_key = temp_pos.x;
				temp_pos = positions.Last();
			}
			
			keyframes.Add(temp_key);
			positions.Add(temp_pos);
		}
		
		if(keyframes[keyframes.Count-1] != numframes)
		{
			keyframes.Add(numframes);
			positions.Add(positions[0]);
		}
		
		time = 0;
		current = 0;
	}
	
	public void AdvanceTime(float t)
	{
		time += t;
		time %= keyframes.Last();
		current = keyframes.BinarySearch(time);
		if(current < 0) current = ~current;
		if(current == keyframes.Count) current = 0;
	}
	
	public Vector2 GetCurrent()
	{
		var prev = current-1;
		if(prev == -1) prev = keyframes.Count-1;
		
		var posdiff = positions[current]-positions[prev];
		var timediff = (keyframes[current]-keyframes[prev]);
		timediff %= keyframes.Last();
		
		var speed = posdiff/timediff;
		var dist = speed*(time-keyframes[prev]);
		return positions[prev]+dist;
	}
}
