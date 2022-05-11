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
	public float numframes;
	
	public KeyframeStepper(IEnumerable<(float, Vector2)> frames, float numframes)
	{
		this.numframes = numframes;
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
		
		time = 0;
		current = 0;
	}
	
	public void AdvanceTime(float t)
	{
		time += t;
		time %= numframes;
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
		if(timediff < 0) timediff += numframes;
		timediff %= numframes;
		
		if(timediff == 0) return positions[prev];
		
		var speed = posdiff/timediff;
		
		var partialdiff = time-keyframes[prev];
		if(partialdiff < 0) partialdiff += numframes;
		partialdiff %= numframes;
		
		var dist = speed*partialdiff;
		return positions[prev]+dist;
	}
}
