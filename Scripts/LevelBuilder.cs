using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public class LevelBuilder : Node2D
{
	//initial speed of the moving platforms
	public float speed = 0.05f;
	//how much to increase or decrease speed by
	public float speedInc = 0.01f;
	public int roundSpeed => -(int)Math.Log10(Math.Abs(speedInc - Math.Truncate(speedInc)));
	
	public bool precise = false;
	public bool paused = false;
	
	public Func<string, bool> inputChecker => s => precise?Input.IsActionJustPressed(s):Input.IsActionPressed(s);
	
	public LevelReader levelreader;
	public ConfigReader configReader;
	
	public override void _Ready()
	{
		var fd = GetNode<FileDialog>("CanvasLayer/FileDialog");
		fd.Popup_();
	}
	
	public override void _PhysicsProcess(float delta)
	{
		if(Input.IsActionJustPressed("toggle_precision")) precise = !precise;
		if(Input.IsActionJustPressed("pause")) {paused = !paused; GD.Print((paused?"P":"Unp") + "aused");}
		
		if(inputChecker("increase_speed")) {speed += speedInc; GD.Print($"New speed {Math.Round(speed,roundSpeed)}");}
		if(inputChecker("decrease_speed")) {speed -= speedInc; GD.Print($"New speed {Math.Round(speed,roundSpeed)}");}
		
		Update();
	}
	
	public override void _Process(float delta)
	{
		if(Input.IsActionJustPressed("full_reload"))
		{
			configReader.Load(configReader.FilePath);
			SetSettings();
			levelreader = new LevelReader(configReader);
		}
		
		if(Input.IsActionJustPressed("clear_cache")) Utils.Cache.Clear();
		if(Input.IsActionJustPressed("toggle_fullscreen")) OS.WindowFullscreen = !OS.WindowFullscreen;
		if(Input.IsActionJustPressed("screenshot")) TakeScreenshot();
		if(Input.IsActionJustPressed("exit")) GetTree().Quit();
	}
	
	public void TakeScreenshot()
	{
		var image = GetViewport().GetTexture().GetData();
		if(image is null) GD.PushError("Viewport texture data is null!!! Complain to cheese.");
		else
		{
			image.FlipY();
			image.SavePng(configReader.Paths["ScreenshotOutput"] + configReader.Paths["LevelName"] + ".png");
		}
	}
	
	public override void _Draw()
	{
		if(levelreader is null) return;
		var mult = inputChecker("forward_once")?1:inputChecker("back_once")?-1:paused?0:1;
		levelreader.GenerateDrawAction(mult*speed)(this);
	}
	
	public void _on_FileDialog_file_selected(string path)
	{
		configReader = new ConfigReader();
		configReader.Load(path);
		SetSettings();
		levelreader = new LevelReader(configReader);
	}
	
	public void SetSettings()
	{
		speed = Convert.ToSingle(configReader.Others["BaseSpeed"]);
		speedInc = Convert.ToSingle(configReader.Others["SpeedIncrement"]);
	}
}
