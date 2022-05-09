using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public class LevelBuilder : Node2D
{
	//change this path to read from somewhere else
	public const string PATH = "C:/Users/eyalz/Desktop/scripts/swz stuff/dump/Dynamic/GIDoJoe.xml";
	//increase this to make the moving platforms move faster. reduce to make slower.
	public const float SPEED = 1/10f;
	
	public LevelReader levelreader;
	
	public override void _Ready()
	{
		levelreader = new LevelReader(PATH);
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
		levelreader.GenerateActionList(SPEED).ForEach((a) => a(this));
	}
}
