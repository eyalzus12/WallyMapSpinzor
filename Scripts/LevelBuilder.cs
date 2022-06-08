using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public class LevelBuilder : Node2D
{
	//the path to the folder containing the maps
	public const string MAP_FOLDER = "C:/Users/eyalz/Desktop/scripts/bh dump/Dynamic";
	//the name of the map file
	public const string MAP_NAME = "BigLostLabyrinth";
	//the path to the mapArt folder
	public const string MAPART_FOLDER = "C:/Program Files (x86)/Steam/steamapps/common/Brawlhalla/mapArt";
	//increase this to make the moving platforms move faster. reduce to make slower.
	public const float SPEED = 1/20f;
	
	public LevelReader levelreader;
	
	public override void _Ready()
	{
		levelreader = new LevelReader($"{MAP_FOLDER}/{MAP_NAME}.xml", MAPART_FOLDER);
	}
	
	public override void _PhysicsProcess(float delta)
	{
		Update();
	}
	
	//TODO: move this to a different class and autoload it
	public override void _Process(float delta)
	{
		if(Input.IsActionJustPressed("exit")) GetTree().Quit();
		if(Input.IsActionJustPressed("toggle_fullscreen")) OS.WindowFullscreen = !OS.WindowFullscreen;
	}
	
	public override void _Draw()
	{
		levelreader.GenerateDrawAction(SPEED)(this);
	}
}
