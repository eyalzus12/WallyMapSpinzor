using Godot;
using System;
using System.Collections.Generic;
using System.Xml.Linq;
using System.Linq;

public static class Utils
{
	public const bool filter = false;
	public const bool mipmap = false;
	public const bool repeat = true;
	
	public static uint Filter => (filter)?0b100:0;
	public static uint Mipmap => (mipmap)?0b010:0;
	public static uint Repeat => (filter)?0b001:0;
	
	public static Dictionary<(string, string), ImageTexture> Cache = new Dictionary<(string, string), ImageTexture>();
	
	public static string GetSubElementValue(this XElement element, string elementValue, string @default = "") => element.Element(elementValue)?.Value ?? @default;
	public static float GetFloatSubElementValue(this XElement element, string elementValue, float @default = 0f) => float.Parse(element.GetSubElementValue(elementValue, $"{@default}"));
	
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
	
	public static float GetFloatAttribute(this XElement element, string attribute, float @default = 0)
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
	
	public static Action<T> Chain<T>(this Action<T> a, Action<T> b) => (t) => {a(t); b(t);};
	public static Action<object> Chain(this Action<object> a, Action<object> b) => a.Chain<object>(b);
	public static Action<T> Combine<T>(this IEnumerable<Action<T>> e) => (t) => e.ForEach(a => a(t));
	public static Action<object> Combine(this IEnumerable<Action<object>> e) => e.Combine<object>();
	public static T Identity<T>(T t) => t;
	public static object Identity(object o) => Identity<object>(o);
	
	public static IEnumerable<Keyframe> GetElementKeyframes(this XElement element, float mult, bool hasCenter, Vector2 center) => 
		element.Elements().SelectMany(a => {
			switch(a.Name.LocalName)
			{
				case "KeyFrame": return a.GetKeyframe(mult, hasCenter, center, 0);
				case "Phase": return a.GetPhase(mult, hasCenter, center);
				default: return null;
			}
		}
	);
	
	public static IEnumerable<Keyframe> GetPhase(this XElement element, float mult, bool hasCenter, Vector2 center)
	{
		var start = mult*(int.Parse(element.GetAttribute("StartFrame", ""))-1);
		var result = element.Elements("KeyFrame").SelectMany((k) => k.GetKeyframe(1, hasCenter, center, start+mult));
		var firstkeyframenum = element.Elements("KeyFrame").First().GetIntAttribute("FrameNum");
		if(firstkeyframenum != 0) result = result.Prepend(new Keyframe(-1, 0, start*Vector2.One, false, Vector2.Zero));
		return result;
	}
	
	public static IEnumerable<Keyframe> GetKeyframe(this XElement element, float mult, bool hasCenter, Vector2 center, float offset)
	{
		var frame = mult*int.Parse(element.GetAttribute("FrameNum", "")) + offset - 1;
		var pos = element.GetElementPositionOrDefault();
		
		if(!hasCenter)
		{
			hasCenter = element.HasAttribute("CenterX") || element.HasAttribute("CenterY");
			center = element.GetElementPositionOrDefault("Center");
		}
		
		yield return new Keyframe(frame, 0, pos, hasCenter, center);//IEnumerable to make main function cleaner
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
		image.Resize((int)bounds.x, (int)bounds.y, (Image.Interpolation)4);
		
		var texture = new ImageTexture();
		texture.CreateFromImage(image, Filter | Mipmap | Repeat);
		Cache.Add((path,instanceName), texture);
		return texture;
	}
	
	public static float ToRad(this float angle) => angle*((float)Math.PI)/180f;
	
	public static Vector2 Switch(this Vector2 v) => new Vector2(v.y, v.x);
	
	public static Vector2 Center(this Rect2 rec) => new Vector2(
		rec.Position.x + rec.Size.x / 2,
		rec.Position.y + rec.Size.y / 2
	);
}
