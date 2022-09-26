using Godot;
using System;
using System.Collections.Generic;

public class ConfigReader
{
	public string FilePath{get; set;} = "";
	public ConfigFile cf{get; set;} = new ConfigFile();
	
	public Dictionary<string, string> Paths{get; set;} = new Dictionary<string, string>();
	public Dictionary<string, bool> Display{get; set;} = new Dictionary<string, bool>();
	public Dictionary<string, float> Sizes{get; set;} = new Dictionary<string, float>();
	public Dictionary<string, Color> Colors{get; set;} = new Dictionary<string, Color>();
	public Dictionary<string, object> Others{get; set;} = new Dictionary<string, object>();
	
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
		StoreDisplays();
		StoreSizes();
		StoreColors();
		StoreOthers();
	}
	
	public const string PATHS = "Paths";
	public void StorePaths()
	{
		foreach(var key in cf.GetSectionKeys(PATHS)) Paths[key] = (string)cf.GetValue(PATHS, key);
	}
	
	public const string DISPLAY = "Display";
	public void StoreDisplays()
	{
		foreach(var key in cf.GetSectionKeys(DISPLAY)) Display[key] = Convert.ToBoolean(cf.GetValue(DISPLAY, key));
	}
	
	public const string SIZES = "Sizes";
	public void StoreSizes()
	{
		foreach(var key in cf.GetSectionKeys(SIZES)) Sizes[key] = Convert.ToSingle(cf.GetValue(SIZES, key));
	}
	
	public const string COLORS = "Colors";
	public void StoreColors()
	{
		foreach(var key in cf.GetSectionKeys(COLORS)) Colors[key] = (Color)cf.GetValue(COLORS, key);
	}
	
	public const string OTHERS = "Others";
	public void StoreOthers()
	{
		foreach(var key in cf.GetSectionKeys(OTHERS)) Others[key] = cf.GetValue(OTHERS, key);
	}
}
