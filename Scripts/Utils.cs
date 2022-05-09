using Godot;
using System;
using System.Collections.Generic;
using System.Xml.Linq;
using System.Linq;

public static class Utils
{
	public static bool HasAttribute(this XElement element, string attribute) => (element.Attributes(attribute).Count() > 0);
	
	public static string GetAttribute(this XElement element, string attribute, string @default = "")
	{
		if(!element.HasAttribute(attribute)) return @default;
		return element.Attributes(attribute).First().Value;
	}
	
	public static bool GetBooleanAttribute(this XElement element, string attribute, bool @default = false)
	{
		if(!element.HasAttribute(attribute)) return @default;
		return bool.Parse(element.GetAttribute(attribute));
	}
	
	public static int GetIntAttribute(this XElement element, string attribute, int @default = 0)
	{
		if(!element.HasAttribute(attribute)) return @default;
		return int.Parse(element.GetAttribute(attribute));
	}
	
	
	public static float GetFloatAttribute(this XElement element, string attribute, float @default = 0f)
	{
		if(!element.HasAttribute(attribute)) return @default;
		return float.Parse(element.GetAttribute(attribute));
	}
	
	
	public static Vector2 GetElementPosition(this XElement element)
	{
		var x = float.Parse(element.GetAttribute("X", ""));
		var y = float.Parse(element.GetAttribute("Y", ""));
		return new Vector2(x, y);
	}
	
	public static (Vector2, Vector2) GetElementPoints(this XElement element)
	{
		float x1, y1, x2, y2;
		
		if(element.HasAttribute("X"))
		{
			var tempx = element.GetFloatAttribute("X");
			x1 = tempx;
			x2 = tempx;
		}
		else
		{
			x1 = float.Parse(element.GetAttribute("X1", ""));
			x2 = float.Parse(element.GetAttribute("X2", ""));
		}
		
		if(element.HasAttribute("Y"))
		{
			var tempy = element.GetFloatAttribute("Y");
			y1 = tempy;
			y2 = tempy;
		}
		else
		{
			y1 = float.Parse(element.GetAttribute("Y1", ""));
			y2 = float.Parse(element.GetAttribute("Y2", ""));
		}
		
		var fromPos = new Vector2(x1, y1);
		var toPos = new Vector2(x2, y2);
		return (fromPos, toPos);
	}
	
	public static Rect2 GetElementRect(this XElement element)
	{
		var x = float.Parse(element.GetAttribute("X", ""));
		var y = float.Parse(element.GetAttribute("Y", ""));
		var w = element.GetFloatAttribute("W");
		var h = element.GetFloatAttribute("H");
		return new Rect2(x, y, w, h);
	}
	
	public static Action<T> Chain<T>(this Action<T> a, Action<T> b)
	{
		return (T t) => {a(t); b(t);};
	}
	
	public static IEnumerable<(float, Vector2)> GetElementKeyframes(this XElement element, float mult = 1f)
	{
		foreach(var anmelem in element.Elements())
		{
			switch(anmelem.Name.LocalName)
			{
				case "KeyFrame":
					yield return anmelem.GetKeyframe(mult);
					break;
				case "Phase":
					foreach(var h in anmelem.GetPhase(mult)) yield return h;
					break;
			}
		}
	}
	
	public static IEnumerable<(float, Vector2)> GetPhase(this XElement element, float mult = 1f)
	{
		var start = mult*int.Parse(element.GetAttribute("StartFrame", ""));
		var result = element.Elements("KeyFrame").Select((k) => k.GetKeyframe(1f, start));
		var firstkeyframenum = element.Elements("KeyFrame").First().GetIntAttribute("FrameNum");
		if(firstkeyframenum != start-1) result = result.Prepend((-1,(start-1)*Vector2.One));
		return result;
	}
	
	public static (float, Vector2) GetKeyframe(this XElement element, float mult = 1f, float offset = 0)
	{
		var frame = mult*int.Parse(element.GetAttribute("FrameNum", "")) + offset - 1;
		var x = element.GetFloatAttribute("X");
		var y = element.GetFloatAttribute("Y");
		return (frame, new Vector2(x,y));
	}
	
	public static void ForEach<T>(this IEnumerable<T> enumerable, Action<T> action)
	{
		foreach(var e in enumerable) action(e);
	}
	
	public static void ForEach(this IEnumerable<object> enumerable, Action<object> action) => enumerable.ForEach<object>(action);
}
