using Godot;
using System;
using System.Collections.Generic;
using System.Xml.Linq;
using System.Linq;

public static class Utils
{
	public static Dictionary<(string, string), ImageTexture> Cache = new();
	
	public static string GetSubElementValue(this XElement element, string elementValue, string @default = "") => element.Element(elementValue)?.Value ?? @default;
	public static float GetFloatSubElementValue(this XElement element, string elementValue, float @default = 0f) => float.Parse(element.GetSubElementValue(elementValue, $"{@default}"));
	
	public static bool HasAttribute(this XElement element, string attribute) => (element.Attributes(attribute).Count() > 0);
	
	public static string GetAttribute(this XElement element, string attribute, string @default = "")
	{
		if(!element.HasAttribute(attribute)) return @default;
		return element.Attribute(attribute).Value.Trim('\u202c');
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
	
	public static Vector2 GetElementPositionOrDefault(this XElement element, string prefix = "", float @default=0)
	{
		var x = element.GetFloatAttribute($"{prefix}X", @default);
		var y = element.GetFloatAttribute($"{prefix}Y", @default);
		return new Vector2(x, y);
	}
	
	public static Vector2 GetElementBoundsOrDefault(this XElement element)
	{
		var w = element.GetFloatAttribute("W", 0);
		var h = element.GetFloatAttribute("H", 0);
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
		return new Rect2(pos, bounds);
	}
	
	public static IEnumerable<Keyframe> GetElementKeyframes(this XElement element, float mult, bool hasCenter, Vector2 center) => 
		element.Elements().SelectMany(a =>
			a.Name.LocalName switch
			{
				"KeyFrame" => a.GetKeyframe(mult, hasCenter, center, 0),
				"Phase" => a.GetPhase(mult, hasCenter, center),
				_ => null
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
		
	public static ImageTexture LoadImageFromPath(string path, string instanceName, Vector2 bounds)
	{
		if(Cache.ContainsKey((path,instanceName))) return Cache[(path,instanceName)];
		if(OS.HasFeature("editor")) GD.Print($"Loading {path}" + ((instanceName == "")?"":$" with instance {instanceName}"));
		var image = new Image();
		var er = image.Load(path);
		if(er != Error.Ok) 
		{
			GD.PushError($"Got error {er} while attempting to load image from path {path}");
			Cache.Add((path,instanceName), null);
			return null;
		}
		
		if(bounds.X == 0f) bounds.X = image.GetWidth();
		if(bounds.Y == 0f) bounds.Y = image.GetHeight();
		
		bounds = bounds.Abs();

		image.Resize((int)bounds.X, (int)bounds.Y);
		
		var texture = ImageTexture.CreateFromImage(image);
		Cache.Add((path,instanceName), texture);
		return texture;
	}
	
	public static string Read(string filepath)
	{
		var f = FileAccess.Open(filepath, FileAccess.ModeFlags.Read);
		var er = FileAccess.GetOpenError();
		if(er != Error.Ok)
		{
			GD.PushError($"Error {er} while reading file {filepath}");
			return "";
		}
		
		var content = f.GetAsText();//read text
		f.Close();//flush buffer
		return content;
	}
}
