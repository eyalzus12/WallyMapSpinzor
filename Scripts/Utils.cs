using Godot;
using System;
using System.Collections.Generic;
using System.Xml.Linq;
using System.Linq;

public static class Utils
{
	public const float PI_F = (float)Math.PI;
	
	public static Dictionary<(string, string), ImageTexture> Cache = new Dictionary<(string, string), ImageTexture>();
	
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
	
	
	public static Vector2 GetElementPosition(this XElement element, string prefix = "")
	{
		var x = float.Parse(element.GetAttribute($"{prefix}X", ""));
		var y = float.Parse(element.GetAttribute($"{prefix}Y", ""));
		return new Vector2(x, y);
	}
	
	public static Vector2 GetElementBounds(this XElement element)
	{
		var w = float.Parse(element.GetAttribute("W", ""));
		var h = float.Parse(element.GetAttribute("H", ""));
		return new Vector2(w, h);
	}
	
	public static Vector2 GetElementPositionOrDefault(this XElement element, string prefix = "")
	{
		var x = float.Parse(element.GetAttribute($"{prefix}X", "0"));
		var y = float.Parse(element.GetAttribute($"{prefix}Y", "0"));
		return new Vector2(x, y);
	}
	
	public static Vector2 GetElementBoundsOrDefault(this XElement element)
	{
		var w = float.Parse(element.GetAttribute("W", "0"));
		var h = float.Parse(element.GetAttribute("H", "0"));
		return new Vector2(w, h);
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
		var pos = element.GetElementPosition();
		var bounds = element.GetElementBoundsOrDefault();
		return new Rect2(pos.x, pos.y, bounds.x, bounds.y);
	}
	
	public static Action<T> Chain<T>(this Action<T> a, Action<T> b)
	{
		return (T t) => {a(t); b(t);};
	}
	
	public static Action<T> Combine<T>(this IEnumerable<Action<T>> e)
	{
		return (T t) => {foreach(var a in e) a(t);};
	}
	
	public static IEnumerable<Keyframe> GetElementKeyframes(this XElement element, float mult, bool hasCenter, Vector2 center)
	{
		foreach(var anmelem in element.Elements())
		{
			switch(anmelem.Name.LocalName)
			{
				case "KeyFrame":
					yield return anmelem.GetKeyframe(mult, hasCenter, center, 0f);
					break;
				case "Phase":
					foreach(var h in anmelem.GetPhase(mult, hasCenter, center)) yield return h;
					break;
			}
		}
	}
	
	public static IEnumerable<Keyframe> GetPhase(this XElement element, float mult, bool hasCenter, Vector2 center)
	{
		var start = mult*(int.Parse(element.GetAttribute("StartFrame", ""))-1);
		var result = element.Elements("KeyFrame").Select((k) => k.GetKeyframe(1f, hasCenter, center, start+mult));
		var firstkeyframenum = element.Elements("KeyFrame").First().GetIntAttribute("FrameNum");
		if(firstkeyframenum != 0) result = result.Prepend(new Keyframe(-1, 0, start*Vector2.One, false, Vector2.Zero));
		return result;
	}
	
	public static Keyframe GetKeyframe(this XElement element, float mult, bool hasCenter, Vector2 center, float offset)
	{
		var frame = mult*int.Parse(element.GetAttribute("FrameNum", "")) + offset - 1;
		var pos = element.GetElementPositionOrDefault();
		
		if(!hasCenter)
		{
			hasCenter = element.HasAttribute("CenterX") || element.HasAttribute("CenterY");
			center = element.GetElementPositionOrDefault("Center");
		}
		
		return new Keyframe(frame, 0, pos, hasCenter, center);
	}
	
	public static void ForEach<T>(this IEnumerable<T> enumerable, Action<T> action)
	{
		foreach(var e in enumerable) action(e);
	}
	
	public static void ForEach(this IEnumerable<object> enumerable, Action<object> action) => enumerable.ForEach<object>(action);
	
	public static ImageTexture LoadImageFromPath(string path, string instanceName, Vector2 bounds)
	{
		if(Cache.ContainsKey((path,instanceName))) return Cache[(path,instanceName)];
		
		var image = new Image();
		var er = image.Load(path);
		if(er != Error.Ok) 
		{
			GD.Print($"Got error {er} while attempting to load image from path {path}");
			Cache.Add((path,instanceName), null);
			return null;
		}
		
		if(bounds.x == 0f) bounds.x = image.GetWidth();
		if(bounds.y == 0f) bounds.y = image.GetHeight();
		bounds = bounds.Abs();
		image.Resize((int)(bounds.x), (int)(bounds.y), (Image.Interpolation)4);
		var texture = new ImageTexture();
		texture.CreateFromImage(image, 0b01);
		Cache.Add((path,instanceName), texture);
		return texture;
	}
	
	public static float ToRad(this float angle) => angle*((float)Math.PI)/180f;
	
	public static Vector2 Abs(this Vector2 v) => new Vector2(Math.Abs(v.x), Math.Abs(v.y));
	
	public static (float, float) RotationThing(Vector2 center, Vector2 current, Vector2 next)
	{
		float result1, result2;
		
		if(current.x == center.x) result1 = ((current.y <= center.y)?3:1) * PI_F / 2f;
		else if(current.x < center.x) result1 = PI_F;
		else result1 = (next.x == center.x && next.y <= center.y)?(2*PI_F):0;
		
		if(next.x == center.x) result2 = ((next.y <= center.y)?3:1) * PI_F / 2f;
		else if(next.x < center.x) result2 = PI_F;
		else result2 = (current.x == center.x && current.y <= center.y)?(2*PI_F):0;
		
		return (result1, result2);
	}
}
