using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class LevelBuilder : Node2D
{
    //initial speed of the moving platforms
    public float speed = 0.05f;
    public float baseSpeed = 0.05f;
    //how much to increase or decrease speed by
    public float speedInc = 0.01f;
    public int updateFreq = 1;
    public int roundSpeed => -(int)Math.Log10(Math.Abs(speedInc - Math.Truncate(speedInc)));
    
    public bool precise = false;
    public bool paused = false;
    
    public Func<string, bool> inputChecker => s => precise?Input.IsActionJustPressed(s):Input.IsActionPressed(s);
    
    public LevelReader levelreader;
    public ConfigReader configReader;
    public NavigationCamera camera;
    public Label displayLabel;
    public AnimationPlayer displayLabelAnimationPlayer;

    public FileDialog fd;
    
    public override void _Ready()
    {
        camera = GetNode<NavigationCamera>("Camera");
        speed = baseSpeed;
        fd = GetNode<FileDialog>("CanvasLayer/FileDialog");
        fd.Popup();
        displayLabel = GetNode<Label>("CanvasLayer/DisplayLabel");
        displayLabelAnimationPlayer = displayLabel.GetNode<AnimationPlayer>("DisplayLabelAnimationPlayer");
    }

    public void Display(string s)
    {
        displayLabel.Text = s;
        displayLabelAnimationPlayer.Stop(true);
        displayLabelAnimationPlayer.Play("Fade");
    }
    
    public override void _PhysicsProcess(double delta)
    {
        bool shouldRedraw = false;

        if(Input.IsActionJustPressed("toggle_precision")) {precise = !precise; Display($"Input: {(precise?"Tap":"Hold")}");}
        if(Input.IsActionJustPressed("pause")) {paused = !paused; Display($"{(paused?"P":"Unp")}aused");}
        if(Input.IsActionJustPressed("reset_speed")) {speed = baseSpeed; Display($"Speed {Math.Round(speed,roundSpeed)}");}

        if(inputChecker("increase_speed")) {speed += speedInc; Display($"Speed {Math.Round(speed,roundSpeed)}");}
        if(inputChecker("decrease_speed")) {speed -= speedInc; Display($"Speed {Math.Round(speed,roundSpeed)}");}
        
        if(inputChecker("increase_red_score")){levelreader.redCount++;shouldRedraw=true;}
        if(inputChecker("decrease_red_score")){levelreader.redCount--;shouldRedraw=true;}
        if(inputChecker("increase_blue_score")){levelreader.blueCount++;shouldRedraw=true;}
        if(inputChecker("decrease_blue_score")){levelreader.blueCount--;shouldRedraw=true;}
        
        if(shouldRedraw || (speed != 0f && !paused && updateFreq != 0 && Engine.GetPhysicsFrames()%(ulong)updateFreq == 0)) QueueRedraw();
    }
    
    public override void _Process(double delta)
    {
        bool shouldRedraw = false;

        if(Input.IsActionJustPressed("full_reload"))
        {
            configReader.Load(configReader.FilePath);
            SetSettings();
            levelreader.SetupReader();
            shouldRedraw = true;
        }

        if(inputChecker("change_path"))
        {levelreader.selectedPath++;Display($"Showing path {levelreader.selectedPath}");shouldRedraw = true;}

        if(inputChecker("change_dir"))
        {
            levelreader.selectedDir = levelreader.selectedDir switch
            {
                LevelReader.Dir.Top => LevelReader.Dir.Right,
                LevelReader.Dir.Right => LevelReader.Dir.Bottom,
                LevelReader.Dir.Bottom => LevelReader.Dir.Left,
                LevelReader.Dir.Left => LevelReader.Dir.Top,
                _ => LevelReader.Dir.Top
            };
            Display($"Showing dir {levelreader.selectedDir}");
            shouldRedraw = true;
        }

        if(Input.IsActionJustPressed("toggle_no_skulls"))
        {levelreader.noSkulls = !levelreader.noSkulls;shouldRedraw = true;}
        
        if(Input.IsActionJustPressed("reset_time"))
        {levelreader.ResetTime();shouldRedraw = true;}
        
        //so shit works during pause
        if(Input.IsActionJustPressed("fit_camera")||Input.IsActionJustPressed("fit_blastzones")||inputChecker("forward_once")||inputChecker("back_once"))
            shouldRedraw = true;
        
        if(Input.IsActionJustPressed("toggle_fullscreen"))
            DisplayServer.WindowSetMode(
                DisplayServer.WindowGetMode() switch
                {
                    DisplayServer.WindowMode.ExclusiveFullscreen or DisplayServer.WindowMode.Fullscreen => DisplayServer.WindowMode.Windowed,
                    _ => DisplayServer.WindowMode.Fullscreen
                }
            );

        if(shouldRedraw) QueueRedraw();

        if(Input.IsActionJustPressed("screenshot")) TakeScreenshot();
        if(Input.IsActionJustPressed("exit")) GetTree().Quit();
    }
    
    public void TakeScreenshot()
    {
        var image = GetViewport().GetTexture().GetImage();
        if(image is null) GD.PushError("SubViewport texture data is null!!! Complain to cheese.");
        else
        {
            var path = configReader.Paths["ScreenshotOutput"];
            if(!path.EndsWith("/"))path+="/";

            image.SavePng($"{path}{configReader.Paths["LevelName"]}.png");
        }
    }
    
    public override void _Draw()
    {
        if(levelreader is null) return;
        var mult = inputChecker("forward_once")?1:inputChecker("back_once")?-1:paused?0:1;
        levelreader.DrawAll(mult*speed*updateFreq);
    }
    
    public void _on_file_dialog_file_selected(string path)
    {
        fd.Visible = false;
        fd.QueueFree();
        configReader = new ConfigReader();
        configReader.Load(path);
        SetSettings();
        levelreader = new LevelReader(this, configReader);
        camera.cf = configReader;
        QueueRedraw();
    }
    
    public void SetSettings()
    {
        baseSpeed = configReader.Others["BaseSpeed"].AsSingle();
        speed = baseSpeed;
        speedInc = configReader.Others["SpeedIncrement"].AsSingle();
        updateFreq = configReader.Others["UpdateFreq"].AsInt32();
    }
}
