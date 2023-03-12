using Godot;
using System;
using System.Collections.Generic;

public partial class ConfigReader
{
	public string FilePath{get; set;} = "";
	public ConfigFile cf{get; set;} = new();
	
	public Dictionary<string, string> Paths{get; set;} = new();
	public Dictionary<string, string> Swf{get; set;} = new();
	public Dictionary<string, Variant> Horde{get; set;} = new();
	public Dictionary<string, bool> Display{get; set;} = new();
	public Dictionary<string, float> Sizes{get; set;} = new();
	public Dictionary<string, Color> Colors{get; set;} = new();
	public Dictionary<string, Variant> Others{get; set;} = new();
	
	public ConfigReader() {}
	
	public void Load(string config)
	{
		FilePath = config;
		var er = cf.Load(config);//open file
		if(er != Error.Ok) throw new ArgumentException($"Error {er} while reading file {config}");
		StoreVars();
	}
	
	public void StoreVars()
	{
		StorePaths();
		StoreSWF();
		StoreHorde();
		StoreDisplays();
		StoreSizes();
		StoreColors();
		StoreOthers();
	}
	
	public const string PATHS = "Paths";
	public void StorePaths()
	{
		foreach(var key in cf.GetSectionKeys(PATHS)) Paths[key] = cf.GetValue(PATHS, key).AsString();
	}

	public const string SWF = "SWF";
	public void StoreSWF()
	{
		foreach(var key in cf.GetSectionKeys(SWF)) Swf[key] = cf.GetValue(SWF, key).AsString();
	}

	public const string HORDE = "Horde";
	public void StoreHorde()
	{
		foreach(var key in cf.GetSectionKeys(HORDE)) Horde[key] = cf.GetValue(HORDE, key);
	}
	
	public const string DISPLAY = "Display";
	public void StoreDisplays()
	{
		foreach(var key in cf.GetSectionKeys(DISPLAY)) Display[key] = cf.GetValue(DISPLAY, key).AsBool();
	}
	
	public const string SIZES = "Sizes";
	public void StoreSizes()
	{
		foreach(var key in cf.GetSectionKeys(SIZES)) Sizes[key] = cf.GetValue(SIZES, key).AsSingle();
	}
	
	public const string COLORS = "Colors";
	public void StoreColors()
	{
		foreach(var key in cf.GetSectionKeys(COLORS)) Colors[key] = cf.GetValue(COLORS, key).AsColor();
	}
	
	public const string OTHERS = "Others";
	public void StoreOthers()
	{
		foreach(var key in cf.GetSectionKeys(OTHERS)) Others[key] = cf.GetValue(OTHERS, key);
	}
}
