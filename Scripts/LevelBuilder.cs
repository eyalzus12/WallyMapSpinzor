using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public class LevelBuilder : Node2D
{
	//the path to the folder containing the maps
	public const string MAP_FOLDER = "C:/Users/eyalz/Desktop/scripts/bh dump/Dynamic";
	//the name of the map file
	public const string MAP_NAME = "ShipwreckFalls";
	
	//the path to the level types file
	//make empty string to not load anything
	public const string LEVEL_TYPES = "C:/Users/eyalz/Desktop/scripts/bh dump/Init/LevelTypes.xml";
	
	//the path to the mapArt folder
	public const string MAPART_FOLDER = "C:/Users/eyalz/Desktop/scripts/mapArt";
	
	//the path to the folder to get normally swf-only files from
	public const string SWF_FOLDER = "C:/Users/eyalz/Desktop/scripts/swfrep";
	
	//the path to output screenshots to
	public const string SCREENSHOT_FOLDER = "C:/Users/eyalz/Desktop/scripts/Renders";
	
	//initial speed of the moving platforms
	public float speed = 0.05f;
	//how much to increase or decrease speed by
	public const float SPEED_INC = 0.01f;
	public static readonly int roundSpeed = -(int)Math.Log10(Math.Abs(SPEED_INC - Math.Truncate(SPEED_INC)));
	
	public bool precise = false;
	public bool paused = false;
	
	public Func<string, bool> inputChecker => s => precise?Input.IsActionJustPressed(s):Input.IsActionPressed(s);
	
	public LevelReader levelreader;
	
	public override void _Ready()
	{
		levelreader = new LevelReader(MAP_FOLDER, MAP_NAME, LEVEL_TYPES, MAPART_FOLDER, SWF_FOLDER);
	}
	
	public override void _PhysicsProcess(float delta)
	{
		if(Input.IsActionJustPressed("toggle_precision")) precise = !precise;
		if(Input.IsActionJustPressed("pause")) {paused = !paused; GD.Print((paused?"P":"Unp") + "aused");}
		
		if(inputChecker("increase_speed")) {speed += SPEED_INC; GD.Print($"New speed {Math.Round(speed,roundSpeed)}");}
		if(inputChecker("decrease_speed")) {speed -= SPEED_INC; GD.Print($"New speed {Math.Round(speed,roundSpeed)}");}
		
		Update();
	}
	
	public override void _Process(float delta)
	{
		if(Input.IsActionJustPressed("toggle_fullscreen")) OS.WindowFullscreen = !OS.WindowFullscreen;
		if(Input.IsActionJustPressed("screenshot")) TakeScreenshot();
		if(Input.IsActionJustPressed("exit")) GetTree().Quit();
	}
	
	public void TakeScreenshot()
	{
		var image = GetViewport().GetTexture().GetData();
		if(image is null) GD.Print("Viewport texture data is null!!! Complain to cheese.");
		else
		{
			image.FlipY();
			image.SavePng($"{SCREENSHOT_FOLDER}/{MAP_NAME}.png");
		}
	}
	
	public override void _Draw()
	{
		var mult = inputChecker("forward_once")?1:inputChecker("back_once")?-1:paused?0:1;
		levelreader.GenerateDrawAction(mult*speed)(this);
	}
}
