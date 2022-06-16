using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public class LevelBuilder : Node2D
{
	//the path to the folder containing the maps
	public const string MAP_FOLDER = "C:/Users/eyalz/Desktop/scripts/bh dump/Dynamic";
	//the name of the map file
	public const string MAP_NAME = "BigTitansEnd";
	//the path to the mapArt folder
	public const string MAPART_FOLDER = "C:/Users/eyalz/Desktop/scripts/mapArt";
	//the path to output screenshots to
	public const string SCREENSHOT_FOLDER = "C:/Users/eyalz/Desktop/scripts/bh dump/Renders";
	//initial speed of the moving platforms
	public float speed = 0.05f;
	//how much to increase or decrease speed by
	public const float SPEED_INC = 0.01f;
	
	public bool paused = false;
	public LevelReader levelreader;
	
	public override void _Ready()
	{
		levelreader = new LevelReader($"{MAP_FOLDER}/{MAP_NAME}.xml", MAPART_FOLDER);
	}
	
	public override void _PhysicsProcess(float delta)
	{
		Update();
	}
	
	public override void _Process(float delta)
	{
		if(Input.IsActionJustPressed("toggle_fullscreen")) OS.WindowFullscreen = !OS.WindowFullscreen;
		if(Input.IsActionJustPressed("screenshot")) TakeScreenshot();
		if(Input.IsActionJustPressed("exit")) GetTree().Quit();
		
		if(Input.IsActionJustPressed("pause")) {paused = !paused; GD.Print((paused?"P":"Unp") + "aused");}
		if(Input.IsActionJustPressed("increase_speed")) {speed += SPEED_INC; GD.Print($"New speed {Math.Round(speed,2)}");}
		if(Input.IsActionJustPressed("decrease_speed")) {speed -= SPEED_INC; GD.Print($"New speed {Math.Round(speed,2)}");}
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
		var mult = Input.IsActionJustPressed("forward_once")?1:Input.IsActionJustPressed("back_once")?-1:paused?0:1;
		levelreader.GenerateDrawAction(mult*speed)(this);
	}
}
