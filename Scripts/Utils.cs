using Godot;
using System;
using System.Collections.Generic;
using System.Xml.Linq;
using System.Linq;
using System.Text.RegularExpressions;
using SwfLib;
using SwfLib.Tags;
using SwfLib.Tags.ControlTags;
using SwfLib.Tags.DisplayListTags;
using SwfLib.Tags.ShapeTags;
using SwfLib.Shapes.Records;
using SwfLib.Shapes.FillStyles;
using SwfLib.Shapes.LineStyles;
using System.IO;

public static class Utils
{
	public static Dictionary<string, ImageTexture> Cache = new();
	
	public static string GetSubElementValue(this XElement element, string elementValue, string @default = "") => element.Element(elementValue)?.Value ?? @default;
	public static float GetFloatSubElementValue(this XElement element, string elementValue, float @default = 0f) => float.Parse(element.GetSubElementValue(elementValue, $"{@default}"));
	
	public static bool HasAttribute(this XElement element, string attribute) => (element.Attributes(attribute).Count() > 0);
	
	public static string GetAttribute(this XElement element, string attribute, string @default = "")
	{
		if(!element.HasAttribute(attribute)) return @default;
		return element.Attribute(attribute).Value;
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

	public static SwfFile swf = null;
	
	public static ImageTexture LoadImageFromSWF(string dir, string spriteName)
	{
		if(Cache.ContainsKey(spriteName)) return Cache[spriteName];

		var swfdir = DirAccess.Open(dir);
		var er = DirAccess.GetOpenError();
		if(er != Error.Ok)
		{
			GD.PushError($"Got error {er} while attempting to open dir {dir}");
			Cache.Add(spriteName, null);
			return null;
		}
		var dirs = swfdir.GetDirectories();
		var regex = new Regex($@"^DefineSprite_\d*_{spriteName}$", RegexOptions.Compiled);
		var desiredDir = dirs.Where(d => regex.IsMatch(d)).First();
		var fullpath = $"{dir}/{desiredDir}/1.png";
		
		var texture = LoadImageFromPath(fullpath);
		Cache.Add(spriteName, texture);
		return texture;

		/*if(swf is null)
		{
			using (var source = File.Open(path, FileMode.Open))
			{
				swf = SwfFile.ReadFrom(source);
			}
		}

		//get symbol class
		var symbolClass = swf.Tags.FirstOrDefault(t => t is SymbolClassTag) as SymbolClassTag;
		if(symbolClass is null) {GD.PushError($"Failed to find symbol class in swf file {path} looking for sprite {spriteName}");return null;}
		//get sprite id
		var spriteId = symbolClass.References.FirstOrDefault(r => r.SymbolName == spriteName)?.SymbolID;
		if(spriteId is null) {GD.PushError($"Failed to find sprite id for {spriteName} in swf file {path}");return null;}
		//get sprite
		var sprite = swf.Tags.FirstOrDefault(t => t is DefineSpriteTag st && st.SpriteID == spriteId) as DefineSpriteTag;
		if(sprite is null) {GD.PushError($"Failed to find define sprite for {spriteName} in swf file {path}");return null;}
		//get place object
		var placeTag = sprite.Tags.FirstOrDefault(t => t is PlaceObjectBaseTag) as PlaceObjectBaseTag;
		if(placeTag is null) {GD.PushError($"Failed to find place tag for sprite {spriteName} in swf file {path}");return null;}
		//get character id
		var characterID = placeTag.CharacterID;
		//get shape
		var shape = swf.Tags.FirstOrDefault(t => t is ShapeBaseTag st && st.ShapeID == characterID) as ShapeBaseTag;
		if(shape is null) {GD.PushError($"Failed to find define shape for {spriteName} in swf file {path}");return null;}
		
		GD.Print(shape.ToString());
		return null;*/
	}

	public static void TextureFromRecordsRGB(IList<IShapeRecordRGB> records, IList<FillStyleRGB> fillStyles, IList<LineStyleRGB> lineStyles)
	{

	}

	public static void TextureFromRecordsRGBA(IList<IShapeRecordRGBA> records, IList<FillStyleRGBA> fillStyles, IList<LineStyleRGBA> lineStyles)
	{

	}

	public static void TextureFromShape(DefineShapeTag t)
	{
		var fillStyles = t.FillStyles;//rgb
		var lineStyles = t.LineStyles;//rgb
		var records =  t.ShapeRecords;//rgb
		TextureFromRecordsRGB(records, fillStyles, lineStyles);
	}

	public static void TextureFromShape2(DefineShape2Tag t)
	{
		var fillStyles = t.FillStyles;//rgb
		var lineStyles = t.LineStyles;//rgb
		var records =  t.ShapeRecords;//rgb
		TextureFromRecordsRGB(records, fillStyles, lineStyles);
	}

	public static void TextureFromShape3(DefineShape3Tag t)
	{
		var fillStyles = t.FillStyles;//rgba
		var lineStyles = t.LineStyles;//rgba
		var records =  t.ShapeRecords;//rgba
		TextureFromRecordsRGBA(records, fillStyles, lineStyles);
	}

	public static void TextureFromShape4(DefineShape4Tag t)
	{
		//no
	}

	public static ImageTexture LoadImageFromPath(string path)
	{
		if(Cache.ContainsKey(path)) return Cache[path];
		if(OS.HasFeature("editor")) GD.Print($"Loading {path}");
		var image = new Image();
		var er = image.Load(path);
		if(er != Error.Ok) 
		{
			GD.PushError($"Got error {er} while attempting to load image from path {path}");
			Cache.Add(path, null);
			return null;
		}
		
		var texture = ImageTexture.CreateFromImage(image);
		Cache.Add(path, texture);
		return texture;
	}
	
	public static string Read(string filepath)
	{
		var f = Godot.FileAccess.Open(filepath, Godot.FileAccess.ModeFlags.Read);
		var er = Godot.FileAccess.GetOpenError();
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
