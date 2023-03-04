using Godot;
using System;
using System.Xml.Linq;
using System.Linq;
using System.Collections.Generic;
using Generator = System.Action<System.Xml.Linq.XElement, Godot.Vector2>;
using AssetGenerator = System.Action<System.Xml.Linq.XElement, Godot.Transform2D>;

public class LevelReader
{
	const int PRIORITY_COUNT = 5;

	const int ASSET_PRIORITY = 0;
	const int DIGIT_PRIORITY = 1;
	const int DATA_PRIORITY = 2;
	const int NAVNODE_PRIORITY = 3;
	const int TEXT_PRIORITY = 4;

	public LevelBuilder ci;
	
	public XDocument parsedMapFile;
	public XDocument parsedLevelTypes;
	
	public Dictionary<string, Generator> generators = new();
	public Dictionary<string, AssetGenerator> assetGenerators = new();
	public Dictionary<int, KeyframeStepper> movingPlatformsDict = new();
	public Dictionary<int, Vector2> navnodesPositions = new();
	public Dictionary<string, int> instanceNameCounter = new();
	public DumbPriorityQueue<Action> draws = new(PRIORITY_COUNT);
	
	public int defaultNumFrames;
	public float defaultSlowMult;
	
	public float blastzoneLeft;
	public float blastzoneRight;
	public float blastzoneTop;
	public float blastzoneBottom;
	public float globalStartFrame;
	
	public long callCount;
	public string assetDir;
	
	public bool noSkulls = false;
	public HashSet<string> themes = new();
	
	public string mapFolder;
	public string swfPath;
	public string mapName;
	public string levelTypesPath;
	public string mapArtPath;
	
	public int redCount = 0;
	public int blueCount = 0;
	
	public ConfigReader cf;
	
	public Font font;
	
	public LevelReader() {}
	
	public LevelReader(LevelBuilder ci, ConfigReader configReader)
	{
		this.ci = ci;
		this.cf = configReader;
		SetupReader();
	}
	
	public void SetupReader()
	{
		callCount = 0;

		//load paths
		mapFolder = cf.Paths["LevelsFolder"];
		if(!mapFolder.EndsWith("/"))mapFolder+="/";

		mapName = cf.Paths["LevelName"];

		levelTypesPath = cf.Paths["LevelTypes"];

		mapArtPath = cf.Paths["MapArt"];
		if(!mapArtPath.EndsWith("/"))mapArtPath+="/";

		swfPath = cf.Paths["SWFReplacement"];
		if(!swfPath.EndsWith("/"))swfPath+="/";

		//load some settings
		redCount = cf.Others["RedScore"].AsInt32();
		blueCount = cf.Others["BlueScore"].AsInt32();
		noSkulls = cf.Others["NoSkulls"].AsBool();
		themes.Clear();themes.Add("");
		var themesArr = cf.Others["Themes"].AsString().Split(",");
		foreach(var theme in themesArr) themes.Add(theme.Trim());
		
		//load font
		font = ResourceLoader.Load<Font>(cf.Paths["Font"]);
		
		//parse map and level types
		parsedMapFile = XDocument.Parse(Utils.Read($"{mapFolder}{mapName}.xml"));
		parsedLevelTypes = (levelTypesPath == "")?null:XDocument.Parse(Utils.Read(levelTypesPath));
		
		//init generators
		InitGenerators();
		
		var firstMap = parsedMapFile.FirstNode as XElement;
		
		//get asset dir
		assetDir = firstMap.GetAttribute("AssetDir");
		
		var firstLevelTypes = parsedLevelTypes?.FirstNode as XElement;
		
		var levelTypeElement = firstLevelTypes?
									.Elements("LevelType")
									.Where(e => e.GetAttribute("LevelName", "") == mapName)
									.FirstOrDefault();
		//get blastzones
		blastzoneLeft = levelTypeElement?.GetFloatSubElementValue("LeftKill", 0)??0;
		blastzoneRight = levelTypeElement?.GetFloatSubElementValue("RightKill", 0)??0;
		blastzoneTop = levelTypeElement?.GetFloatSubElementValue("TopKill", 0)??0;
		blastzoneBottom = levelTypeElement?.GetFloatSubElementValue("BottomKill", 0)??0;
		globalStartFrame = levelTypeElement?.GetFloatSubElementValue("StartFrame", 0)??0;
		
		//get defaults
		defaultNumFrames = firstMap.GetIntAttribute("NumFrames", -1);
		defaultSlowMult = firstMap.GetFloatAttribute("SlowMult", 1f);
		
		//reset data
		ResetTime();
		navnodesPositions.Clear();
		foreach(var stepper in movingPlatformsDict.Values) stepper.AdvanceTime(globalStartFrame);
	}
	
	public void LoadExtraAssets()
	{
		var first = parsedMapFile?.FirstNode as XElement;

		var tempred = redCount;
		var tempblue = blueCount;
		
		//load all scoreboard assets
		for(int i = 0; i < 10; ++i)
		{
			redCount = i;
			for(int j = 0; j < 10; ++j)
			{
				blueCount = j;
				
				first.Elements("TeamScoreboard")
					.ForEach(e => DrawScoreboard(e));
			}
		}
		
		redCount = tempred;
		blueCount = tempblue;

		//clear all draws
		RenderingServer.CanvasItemClear(ci.GetCanvasItem());
	}
	
	//this method initializes dictionaries of strings to functions.
	//this dictionary is later used to assign the correct draw function to each component.
	public void InitGenerators()
	{
		assetGenerators.Clear();

		if(cf.Display["Assets"])
		{
			assetGenerators.Add("Platform", DrawPlatform);
			assetGenerators.Add("MovingPlatform", DrawMovingPlatform);
			
			if(cf.Display["ScoreboardDigits"])
			assetGenerators.Add("TeamScoreboard", DrawScoreboard);
		}
		
		if(cf.Display["Background"]) assetGenerators.Add("Background", DrawBackground);
		
		generators.Clear();
		
		if(cf.Display["Bounds"])
		{
			if(cf.Display["CameraBounds"]) generators.Add("CameraBounds", DrawCameraBounds);
			if(cf.Display["SpawnbotBounds"]) generators.Add("SpawnBotBounds", DrawSpawnBotBounds);
		}
		
		if(cf.Display["Items"])
		{
			generators.Add("ItemSpawn", DrawItemSpawn);
			generators.Add("ItemInitSpawn", DrawInitialItemSpawn);
			generators.Add("ItemSet", DrawItemSet);
			generators.Add("TeamItemInitSpawn", DrawTeamItemInitSpawn);
			generators.Add("DynamicItemSpawn", DrawDynamicItemSpawn);
		}
		
		if(cf.Display["Respawns"])
		{
			generators.Add("Respawn", DrawRespawn);
			generators.Add("DynamicRespawn", DrawDynamicRespawn);
		}
		
		if(cf.Display["Collision"])
		{
			generators.Add("HardCollision", DrawHardCollision);
			generators.Add("SoftCollision", DrawSoftCollision);
			generators.Add("NoSlideCollision", DrawNoSlideCollision);
			
			generators.Add("GameModeHardCollision", DrawGamemodeHardCollision);
			generators.Add("GameModeSoftCollision", DrawGamemodeSoftCollision);
			generators.Add("GameModeNoSlideCollision", DrawGamemodeNoSlideCollision);
			
			generators.Add("BouncyHardCollision", DrawBouncyHardCollision);
			generators.Add("BouncySoftCollision", DrawBouncySoftCollision);
			generators.Add("BouncyNoSlideCollision", DrawBouncyNoSlideCollision);
			
			generators.Add("TriggerCollision", DrawTriggerCollision);
			generators.Add("StickyCollision", DrawStickyCollision);
			generators.Add("ItemIgnoreCollision", DrawItemIgnoreCollision);

			generators.Add("PressurePlateCollision", DrawHardPressurePlateCollision);
			generators.Add("SoftPressurePlateCollision", DrawSoftPressurePlateCollision);
			
			generators.Add("DynamicCollision", DrawDynamicCollision);
		}
		
		if(cf.Display["MovingPlatformData"])
		generators.Add("MovingPlatform", DrawMovingPlatformData);
		
		if(cf.Display["Navmesh"])
		{
			generators.Add("NavNode", DrawNavNode);
			generators.Add("DynamicNavNode", DrawDynamicNavNode);
		}
		
		if(cf.Display["Goals"]) generators.Add("Goal", DrawGoal);
		if(cf.Display["NoDodgeZones"]) generators.Add("NoDodgeZone", DrawNoDodgeZone);
	}
	
	//helper functions
	public bool HasGenerator(string s) => generators.ContainsKey(s);
	public bool HasGenerator(XElement e) => HasGenerator(e.Name.LocalName);
	public Generator GetGenerator(string s) => generators[s];
	public Generator GetGenerator(XElement e) => GetGenerator(e.Name.LocalName);
	public void InvokeGenerator(XElement e) => GetGenerator(e)(e, Vector2.Zero);
	public bool HasAssetGenerator(string s) => assetGenerators.ContainsKey(s);
	public bool HasAssetGenerator(XElement e) => HasAssetGenerator(e.Name.LocalName);
	public AssetGenerator GetAssetGenerator(string s) => assetGenerators[s];
	public AssetGenerator GetAssetGenerator(XElement e) => GetAssetGenerator(e.Name.LocalName);
	public void InvokeAssetGenerator(XElement e) => GetAssetGenerator(e)(e, Transform2D.Identity);
	
	//advance time by timepass, and then draw everything
	public void DrawAll(float timepass)
	{
		//clear lists
		draws.Clear();
		navnodesPositions.Clear();
		instanceNameCounter.Clear();

		//correct scores
		redCount = Math.Max(Math.Min(redCount,99), 0);
		blueCount = Math.Max(Math.Min(blueCount,99), 0);

		//if first time, load anything extra
		if(callCount == 0) LoadExtraAssets();
		
		//advance time
		AdvanceTime(timepass);
		
		var first = parsedMapFile.FirstNode as XElement;

		//draw blastzones
		DrawBlastzoneBounds(first);

		//draw assets
		first.Elements()
				.Where(HasAssetGenerator)
				.ForEach(InvokeAssetGenerator);
		
		//draw non-asset things
		first.Elements()
				.Where(HasGenerator)
				.ForEach(InvokeGenerator);
		
		//draw navmesh
		if(cf.Display["Navmesh"]) DrawNavMesh(first);
		
		

		//draw anything that was delayed (text and asset positions)
		while(!draws.Empty)
			draws.Dequeue()();

		callCount++;
	}
	
	//reset moving platforms
	public void ResetTime()
	{
		movingPlatformsDict.Clear();
		(parsedMapFile.FirstNode as XElement).Elements("MovingPlatform").ForEach(SetupMovingPlatform);
	}
	
	public void AdvanceTime(float time) => movingPlatformsDict.Values.ForEach(s => s.AdvanceTime(time));
	
	//store a delayed action for drawing a text and its outline
	public void DrawString(string text, Vector2 pos, int priority=TEXT_PRIORITY) => 
		draws.Enqueue(()=>
		{
			ci?.DrawString(font, pos, text, modulate: cf.Colors["LabelModulate"], fontSize: cf.Others["FontSize"].AsInt32());
			ci?.DrawStringOutline(font, pos, text, modulate: new Color(0,0,0,1)*cf.Colors["LabelModulate"], fontSize: cf.Others["FontSize"].AsInt32(), size: 5);
		}, priority);

	public void DrawCircle(Vector2 pos, float rad, Color color, int priority=DATA_PRIORITY) =>
		draws.Enqueue(() =>
			ci?.DrawCircle(pos,rad,color),
			priority
		);

	public void DrawLine(Vector2 from, Vector2 to, Color color, int priority=DATA_PRIORITY) =>
		draws.Enqueue(() =>
			ci?.DrawLine(from,to,color),
			priority
		);

	public void DrawRect(Rect2 rect, Color color, bool filled, int priority=DATA_PRIORITY) => DrawRect(rect,color,filled,Transform2D.Identity,priority);
	public void DrawRect(Rect2 rect, Color color, bool filled, Transform2D trans, int priority=DATA_PRIORITY) =>
		draws.Enqueue(() => 
		{
			ci?.DrawSetTransformMatrix(trans);
			ci?.DrawRect(rect,color,filled);
			ci?.DrawSetTransformMatrix(Transform2D.Identity);
		},priority);
	
	public void DrawTexture(Texture2D texture, Transform2D trans, int priority = ASSET_PRIORITY) =>
		draws.Enqueue(() => 
		{
			ci?.DrawSetTransformMatrix(trans);
			ci?.DrawTexture(texture,Vector2.Zero);
			ci?.DrawSetTransformMatrix(Transform2D.Identity);
		}, priority);

	public void DrawNavMesh(XElement element) =>
		element.Elements("DynamicNavNode")
			.Prepend(element)
			.SelectMany(e => e.Elements("NavNode"))
			.ForEach(DrawNavLine);
	
	public void DrawBlastzoneBounds(XElement element)
	{
		//get camera bounds
		var camerabounds = element.Element("CameraBounds").GetElementRect();
		//get blastzone rect
		var rect = camerabounds.GrowIndividual(blastzoneLeft, blastzoneTop, blastzoneRight, blastzoneBottom);
		
		//handle fit to camera or fit to blastzones inputs
		var navcam = ci.camera;
		if(Input.IsActionJustPressed("fit_camera")) navcam.FitToRect(camerabounds);
		if(Input.IsActionJustPressed("fit_blastzones")) navcam.FitToRect(rect);
		
		//draw
		if(cf.Display["Bounds"] && cf.Display["BlastzoneBounds"])
			DrawRect(rect, cf.Colors["BlastzoneBounds"], false);
	}
	
	//parse the MovingPlatform
	public void SetupMovingPlatform(XElement element)
	{
		//get position
		var pos = element.GetElementPositionOrDefault();
		
		//get plat id
		var platid = element.GetIntAttribute("PlatID");
		//get animation element
		var animationElement = element.Element("Animation");
		
		//get default number of frames
		var numframes_defaultto = (defaultNumFrames == -1)?"":$"{defaultNumFrames}";
		//get number of frames
		var numframes = int.Parse(animationElement.GetAttribute("NumFrames", numframes_defaultto));
		//get start frame
		var startframe = animationElement.GetIntAttribute("StartFrame", 1) - 1;
		//get speed multiplier
		var mult = animationElement.GetFloatAttribute("SlowMult", defaultSlowMult);
		//check if has animation center
		var hasCenter = animationElement.HasAttribute("CenterX") || animationElement.HasAttribute("CenterY");
		//get center
		var center = animationElement.GetElementPositionOrDefault("Center");
		
		//get keyframes
		var data = animationElement.GetElementKeyframes(mult, hasCenter, center);
		
		//create a moving platform position calculator
		var stepper = new KeyframeStepper(data, Vector2.Zero, numframes*mult-1);
		
		//apply start frame
		stepper.AdvanceTime(startframe*mult + globalStartFrame);

		//add to dictionary
		movingPlatformsDict.Add(platid, stepper);
	}
	
	//////////////////////////////////////////
	//////////////////Bounds//////////////////
	//////////////////////////////////////////
	public void DrawBounds(XElement element, Vector2 offset, Color color)
	{
		var rect = element.GetElementRect();
		rect.Position += offset;
		DrawRect(rect, color, false);
	}
	
	public void DrawCameraBounds(XElement element, Vector2 offset = default) => DrawBounds(element, offset, cf.Colors["CameraBounds"]);
	public void DrawSpawnBotBounds(XElement element, Vector2 offset = default) => DrawBounds(element, offset, cf.Colors["SpawnbotBounds"]);
	
	//////////////////////////////////////////
	///////////////Item Spawns////////////////
	//////////////////////////////////////////
	public void DrawArea(XElement element, Vector2 offset, Color color)
	{
		var rect = element.GetElementRect();
		rect.Position += offset;
		//if point
		if(rect.Size.X == 0 && rect.Size.Y == 0)
		{
			var rad = cf.Sizes["DefaultAreaRadius"];
			var newrect = new Rect2(rect.Position - rad*Vector2.One, 2f*rad*Vector2.One);
			DrawRect(newrect, color, false);
			DrawCircle(rect.Position, rad/2f, color);
		}
		//if line
		else if(rect.Size.X == 0 || rect.Size.Y == 0)
			DrawLine(rect.Position, rect.End, color);
		//if proper rect
		else
			DrawRect(rect, color, true);
	}
	
	public void DrawItemSpawn(XElement element, Vector2 offset = default) => DrawArea(element, offset, cf.Colors["ItemSpawn"]);
	public void DrawInitialItemSpawn(XElement element, Vector2 offset = default) => DrawArea(element, offset, cf.Colors["InitialItemSpawn"]);
	public void DrawItemSet(XElement element, Vector2 offset = default) => DrawArea(element, offset, cf.Colors["ItemSet"]);
	public void DrawTeamItemInitSpawn(XElement element, Vector2 offset = default) => DrawArea(element, offset, cf.Colors["TeamItemInitSpawn"]);
	
	//////////////////////////////////////////
	/////////////////Respawns/////////////////
	//////////////////////////////////////////
	public void DrawRespawn(XElement element, Vector2 offset = default)
	{
		var initial = element.GetBooleanAttribute("Initial");
		var expandedInit = element.GetBooleanAttribute("ExpandedInit");
		
		Color chosenColor;
		if(initial) chosenColor = cf.Colors["InitialRespawn"];
		else if(expandedInit) chosenColor = cf.Colors["ExtendedInitialRespawn"];
		else chosenColor = cf.Colors["Respawn"];
		
		var pos = element.GetElementPosition();
		pos += offset;
		DrawCircle(pos, cf.Sizes["RespawnRadius"], chosenColor);
	}
	
	//////////////////////////////////////////
	////////////////Collision/////////////////
	//////////////////////////////////////////
	public void DrawCollision(XElement element, Vector2 offset, Color color)
	{
		//get line
		(var from, var to) = element.GetElementPoints();
		from += offset; to += offset;
		//get normalized line
		var dir = (to-from).Normalized();
		//get normal line
		var clockwise_dir = new Vector2(-dir.Y, dir.X);
		
		//apply normal overrides
		var normal = clockwise_dir;
		if(element.HasAttribute("NormalX")) normal.X = element.GetFloatAttribute("NormalX");
		if(element.HasAttribute("NormalY")) normal.Y = element.GetFloatAttribute("NormalY");
		//start of displayed normal line
		var normal_start = (from+to)/2f;
		//end of displayed normal line
		var normal_end = normal_start + cf.Sizes["NormalLength"] * normal;
		
		//draw collision line
		DrawLine(from, to, color);
		
		//draw normal line
		if(cf.Display["CollisionNormals"])
		DrawLine(normal_start, normal_end, cf.Colors["NormalLine"]);
		
		//draw team indicator
		if(cf.Display["TeamCollision"] && element.HasAttribute("Team"))
		{
			//get offset for drawing the indicators
			var teamoffset = cf.Sizes["TeamLinesOffset"] * clockwise_dir;
			//get team
			var team = element.GetIntAttribute("Team");
			//get team color
			var teamcolor = cf.Colors[$"TeamColor{team}"];
			//draw team inidicators
			DrawLine(from+teamoffset, to+teamoffset, teamcolor);
			DrawLine(from-teamoffset, to-teamoffset, teamcolor);
		}
		
		//draw taunt events
		if(cf.Display["TauntEvent"] && element.HasAttribute("TauntEvent"))
		{
			//get taunt event
			var tauntevent = element.GetAttribute("TauntEvent");
			//get desired position: heighest and leftest possible
			var labelPos = new Vector2(Math.Min(from.X, to.X), Math.Min(from.Y, to.Y));
			//draw taunt event
			DrawString($"TauntEvent: {tauntevent}", labelPos + cf.Sizes["CollisionTauntEventOffset"]*Vector2.Up);
		}
		
		//draw anchors
		if(cf.Display["Anchors"] && element.HasAttribute("AnchorX") && element.HasAttribute("AnchorY"))
		{
			//get anchor position
			var anchor = element.GetElementPosition("Anchor");
			//take collision color, and make it more transparent
			var more_transparent = new Color(color.R, color.G, color.B, 0.3f);
			//draw anchor
			DrawCircle(anchor, cf.Sizes["AnchorRadius"], more_transparent);
		}
	}
	
	public void DrawHardCollision(XElement element, Vector2 offset = default) => DrawCollision(element, offset, cf.Colors["HardCollision"]);
	public void DrawSoftCollision(XElement element, Vector2 offset = default) => DrawCollision(element, offset, cf.Colors["SoftCollision"]);
	public void DrawNoSlideCollision(XElement element, Vector2 offset = default) => DrawCollision(element, offset, cf.Colors["NoSlideCollision"]);
	
	public void DrawGamemodeCollision(XElement element, Vector2 offset, Color color) => DrawCollision(element, offset, color);
	public void DrawGamemodeHardCollision(XElement element, Vector2 offset = default) => DrawGamemodeCollision(element, offset, cf.Colors["GamemodeHardCollision"]);
	public void DrawGamemodeSoftCollision(XElement element, Vector2 offset = default) => DrawGamemodeCollision(element, offset, cf.Colors["GamemodeSoftCollision"]);
	public void DrawGamemodeNoSlideCollision(XElement element, Vector2 offset = default) => DrawGamemodeCollision(element, offset, cf.Colors["GamemodeNoSlideCollision"]);
	
	public void DrawBouncyCollision(XElement element, Vector2 offset, Color color) => DrawCollision(element, offset, color);
	public void DrawBouncyHardCollision(XElement element, Vector2 offset = default) => DrawBouncyCollision(element, offset, cf.Colors["BouncyHardCollision"]);
	public void DrawBouncySoftCollision(XElement element, Vector2 offset = default) => DrawBouncyCollision(element, offset, cf.Colors["BouncySoftCollision"]);
	public void DrawBouncyNoSlideCollision(XElement element, Vector2 offset = default) => DrawBouncyCollision(element, offset, cf.Colors["BouncyNoSlideCollision"]);

	public void DrawTriggerCollision(XElement element, Vector2 offset = default) => DrawCollision(element, offset, cf.Colors["TriggerCollision"]);
	public void DrawStickyCollision(XElement element, Vector2 offset = default) => DrawCollision(element, offset, cf.Colors["StickyCollision"]);
	public void DrawItemIgnoreCollision(XElement element, Vector2 offset = default) => DrawCollision(element, offset, cf.Colors["ItemIgnoreCollision"]);
	
	public void DrawPressurePlateCollision(XElement element, Vector2 offset, Color color)
	{
		//get line
		(var from, var to) = element.GetElementPoints();
		from += offset; to += offset;
		//get desired label position (heightest and leftest possible)
		var labelPos = new Vector2(Math.Min(from.X, to.X), Math.Min(from.Y, to.Y));
		//get powers
		var powers = element.GetAttribute("TrapPowers").Replace(",", " ");
		//get cooldown
		var cooldown = element.GetIntAttribute("Cooldown");
		
		//get if facing left
		var faceleft = bool.Parse(element.GetAttribute("FaceLeft",""));
		var dirmult = faceleft?1:-1;
		
		//get fire "offset"
		var fireoffset = element.GetElementPosition("FireOffset");
		
		//get middle of collision line
		var middle = (from+to)/2f;

		//get fire position
		var firePos = fireoffset + offset;
		
		//draw collision line
		DrawCollision(element, offset, color);

		//draw trap powers
		if(cf.Display["TrapPowers"])
		DrawString($"Powers: {powers}", firePos + cf.Sizes["PressurePlatePowerOffset"]*Vector2.Up);

		//draw trap cooldown
		if(cf.Display["TrapCooldown"])
		DrawString($"Cooldown: {cooldown}f", labelPos + cf.Sizes["PressurePlateCooldownOffset"]*Vector2.Up);

		//draw power offset
		if(cf.Display["TrapPowerOffset"])
		{
			//draw fire location
			DrawCircle(firePos, cf.Sizes["FireOffsetRadius"], cf.Colors["PressurePlateFireOffset"]);
			DrawLine(middle, firePos, cf.Colors["PressurePlateFireOffset"]);

			//draw arrow in firing direction
			var lineend = firePos + dirmult*cf.Sizes["PressurePlateDirLineLength"]*Vector2.Left;
			var offX = cf.Sizes["PressurePlateDirLineOffsetX"];
			var offY = cf.Sizes["PressurePlateDirLineOffsetY"];
			var sideline1 = lineend + dirmult*new Vector2(offX, offY);
			var sideline2 = lineend + dirmult*new Vector2(offX, -offY);
			var plateLine = cf.Colors["PressurePlateLine"];
			DrawLine(firePos, lineend, plateLine);
			DrawLine(lineend, sideline1, plateLine);
			DrawLine(lineend, sideline2, plateLine);
		}
	}
	
	public void DrawHardPressurePlateCollision(XElement element, Vector2 offset = default) => DrawPressurePlateCollision(element, offset, cf.Colors["PressurePlateCollision"]);
	public void DrawSoftPressurePlateCollision(XElement element, Vector2 offset = default) => DrawPressurePlateCollision(element, offset, cf.Colors["SoftPressurePlateCollision"]);
	
	//////////////////////////////////////////
	///////////////////Misc///////////////////
	//////////////////////////////////////////
	public void DrawGoal(XElement element, Vector2 offset = default)
	{
		//get team
		var goal = element.GetIntAttribute("Team", 1);
		//draw with matching color
		DrawArea(element, offset, cf.Colors[$"GoalColor{goal}"]);
	}

	public void DrawNoDodgeZone(XElement element, Vector2 offset = default) => DrawArea(element, offset, cf.Colors["NoDodgeZone"]);
	
	//////////////////////////////////////////
	//////////////Navigation//////////////////
	//////////////////////////////////////////
	
	public void DrawNavNode(XElement element, Vector2 offset = default)
	{
		//get position
		var pos = element.GetElementPositionOrDefault() + offset;
		//get navid
		var navid = element.GetAttribute("NavID");
		//get numerical id
		var id = NormalizeNavID(navid);
		//get type
		var type = GetNavType(navid);
		//add to navnodes
		navnodesPositions.Add(id, pos);
		//draw navnode
		DrawCircle(pos, cf.Sizes["NavnodeRadius"], cf.Colors[$"Navnode{type}"]);
		//display navid
		if(cf.Display["NavID"])
		DrawString($"NavID: {navid}", pos + cf.Sizes["NavnodeIDOffset"]*Vector2.Up);
	}
	
	public void DrawNavLine(XElement element)
	{
		//get navid	
		var navid = element.GetAttribute("NavID");
		//get numerical id
		var id = NormalizeNavID(navid);
		//get type
		var type = GetNavType(navid);
		
		//get position
		var pos = navnodesPositions[id];
		
		//whether to output missing navnodes to console
		var firstcall = (callCount == 0);
		
		//draw lines
		element
			.GetAttribute("Path")//get path
			.Split(",")//split to parts
			.ForEach(//for each connected nav node, draw a line
				(s) =>
				{
					//get connected navnode id
					var norms = NormalizeNavID(s);
					//get connected navnode type
					var types = GetNavType(s);
					//if missing, output warning
					if(!navnodesPositions.ContainsKey(norms))
					{
						if(firstcall)
						{
							var stypedesc = (types!="")?$" with type {types}":"";
							GD.PushWarning($"NavNode {navid} connects to NavNode {norms}{stypedesc}, but there's no NavNode with ID {norms}");
						}
						return;
					}
					
					//draw line
					DrawLine(pos, (pos+navnodesPositions[norms])/2f, cf.Colors[$"Navnode{types}"], priority: NAVNODE_PRIORITY);
				}
		);
	}
	
	private int NormalizeNavID(string s)
	{
		char first = s[0];
		if(first < '0' || '9' < first) s = s.Substring(1);
		return int.Parse(s);
	}
	
	public static readonly HashSet<char> NAVTYPES = new HashSet<char>{'A', 'D', 'G', 'L', 'W'};
	private string GetNavType(string s) => NAVTYPES.Contains(s[0])?s[0].ToString():"";
	
	//////////////////////////////////////////
	////////////////Assets////////////////////
	//////////////////////////////////////////
	public void DrawAsset(XElement element, Transform2D trans, bool doOffset, string assetfolder, string instanceName, string assetNameOverride = "", int priority=ASSET_PRIORITY)
	{
		//get position
		var offset = doOffset?element.GetElementPositionOrDefault():Vector2.Zero;
		
		//get asset size
		var bounds = element.GetElementBoundsOrDefault();
		
		//get asset name
		var assetname = (assetNameOverride == "")?element.GetAttribute("AssetName"):assetNameOverride;
		
		//get asset path, including ../ override
		var assetpath = assetname.StartsWith("../") ?
		assetname.Substring("../".Length) :
		$"{assetfolder}/{assetname}";
		
		//get if has skulls
		var hasSkulls = element.GetBooleanAttribute("HasSkulls", false);
		//get theme
		var theme = element.GetAttribute("Theme");
		
		//draw asset
		DrawAssetFromData(trans, offset, $"{mapArtPath}{assetpath}", instanceName, bounds, hasSkulls, theme, priority);
	}
	
	public void DrawAssetFromData(Transform2D trans, Vector2 offset, string path, string instanceName = "", Vector2 bounds = default, bool hasSkulls = false, string theme="", int priority=ASSET_PRIORITY)
	{
		//check for conditionally drawn assets
		if(
			//check for no skulls
			(!noSkulls && instanceName.EndsWith("am_NoSkulls")) ||
			(!noSkulls && path.EndsWith("_NoSkulls.jpg")) ||
			(noSkulls && hasSkulls) ||
			//check for theme
			(theme != "" && theme.Split(",").All((s)=>!themes.Contains(s)))
		) return;
		
		//load texture
		var texture = Utils.LoadImageFromPath(path, instanceName, bounds);

		//texture doesn't exist
		if(texture is null) return;

		//move
		trans = trans.TranslatedLocal(offset);

		//flip if negative width/height
		if(bounds.X < 0f) trans = trans.ScaledLocal(new Vector2(-1,1));
		if(bounds.Y < 0f) trans = trans.ScaledLocal(new Vector2(1,-1));
		
		//draw asset position
		if(cf.Display["AssetPosition"])
		DrawCircle(trans.Origin, cf.Sizes["AssetPositionRadius"], cf.Colors["Asset"]);
		if(cf.Display["AssetRect"])
		DrawRect(new Rect2(Vector2.Zero, texture.GetSize()), cf.Colors["Asset"], false, trans: trans);
		
		DrawTexture(texture, trans, priority);
	}
	
	public void DrawBackground(XElement element, Transform2D trans = default) => DrawAsset(element.Parent.Elements("CameraBounds").First(), trans, true, "Backgrounds", "", element.GetAttribute("AssetName"));
	
	public void DrawPlatform(XElement element, Transform2D trans = default)
	{
		//check theme
		var theme = element.GetAttribute("Theme");
		if(theme != "" && theme.Split(",").All((s)=>!themes.Contains(s))) return;

		//get instance name
		var instanceName = element.GetAttribute("InstanceName");
		//first time this instance name happens
		if(!instanceNameCounter.ContainsKey(instanceName)) instanceNameCounter[instanceName] = -1;
		//increase instance name count
		instanceNameCounter[instanceName]++;
		//change instance name to ensure no conflicts
		instanceName = instanceNameCounter[instanceName].ToString() + "_" + instanceName;
		
		//apply position, scale, and rotation
		trans = trans
		.TranslatedLocal(//move
			element.GetElementPositionOrDefault()
		)
		.ScaledLocal(//scale
			element.GetElementPositionOrDefault("Scale",1)*element.GetFloatAttribute("Scale", 1)
		);

		//for some reason, using RotatedLocal doesn't work properly
		//so need to manually add the rotation
		trans = new Transform2D(
			trans.Rotation+Mathf.DegToRad(element.GetFloatAttribute("Rotation")),
			trans.Scale,trans.Skew,trans.Origin
		);

		//doesn't have its own asset. load children.
		if(!element.HasAttribute("AssetName"))
		{
			element.Elements().ForEach(
				e =>
				{
					switch(e.Name.LocalName)
					{
						//child is an asset
						case "Asset":
							DrawAsset(e, trans, true, assetDir, instanceName);
							return;
						//child is a platofrm
						case "Platform":
							DrawPlatform(e, trans);
							return;
						default:
							return;
					}
				}
			);
		}
		//has its own asset
		else DrawAsset(element, trans, false, assetDir, instanceName);

		//display instance name
		if(cf.Display["PlatformLabel"])
			DrawString($"InstanceName: {instanceName}", trans.Origin);
	}
	
	public void DrawMovingPlatform(XElement element, Transform2D trans = default)
	{
		//get plat id
		var platid = element.GetIntAttribute("PlatID");
		//get moving platform position calculator
		var stepper = movingPlatformsDict[platid];
		//move display accourdingly
		trans = trans
			.TranslatedLocal(stepper.GetCurrent())
			.TranslatedLocal(element.GetElementPositionOrDefault());
		
		//draw assets
		element.Elements()
			.Where(HasAssetGenerator)
			.ForEach(e => GetAssetGenerator(e)(e,trans));
	}
	
	public void DrawMovingPlatformData(XElement element, Vector2 offset = default)
	{
		//get animation element
		var animationElement = element.Element("Animation");
		//get platid
		var platid = element.GetIntAttribute("PlatID");
		//get moving platform position calculator
		var stepper = movingPlatformsDict[platid];
		//var hasCenter = animationElement.HasAttribute("CenterX")||animationElement.HasAttribute("CenterY");
		//var center = animationElement.GetElementPositionOrDefault("Center");
		//get starting position
		var originpos = offset + element.GetElementPositionOrDefault();
		//get current position
		var pos = stepper.GetCurrent() + originpos;
		
		//display time
		if(cf.Display["MovingPlatformsTime"])
		DrawString($"Time: {stepper.time}", pos + cf.Sizes["MovingPlatformTimeOffset"]*Vector2.Up);

		//display position
		if(cf.Display["MovingPlatformsPosition"])
		DrawCircle(pos, cf.Sizes["MovingPlatformRadius"], cf.Colors["MovingPlatform"]);

		/*if(cf.Display["ShowCenter"] && hasCenter)
		{
			DrawCircle(center+originpos, cf.Sizes["CenterRadius"], cf.Colors["Center"]);
			if(cf.Display["CenterPlatID"])
			DrawString($"PlatID: {platid}", center+originpos+cf.Sizes["CenterPlatIDOffset"]*Vector2.Up);
		}*/
		
		//display current center
		var currKeyframe = stepper.GetUsedKeyframe();
		if(cf.Display["ShowCenter"] && currKeyframe.hasCenter)
		{
			DrawCircle(currKeyframe.center+originpos, cf.Sizes["CenterRadius"], cf.Colors["Center"]);
			//display the center's plat id
			if(cf.Display["CenterPlatID"])
			DrawString($"PlatID: {platid}", currKeyframe.center+originpos+cf.Sizes["CenterPlatIDOffset"]*Vector2.Up);
		}
	}
	
	public void DrawScoreboard(XElement element, Transform2D trans = default)
	{
		//get red team x position
		var redX = element.GetFloatAttribute("RedTeamX");
		//get blue team x position
		var blueX = element.GetFloatAttribute("BlueTeamX");
		//get y position
		var y = element.GetFloatAttribute("Y");
		//get relative x position of ones digit
		var oneDigitX = element.GetFloatAttribute("DoubleDigitsOnesX");
		//get relative x position of tens digit
		var tenDigitX = element.GetFloatAttribute("DoubleDigitsTensX");
		//get y when using 2 digits
		var doubleY = element.GetFloatAttribute("DoubleDigitsY");
		//get the reciprocal of the digit scale when using 1 digit (and not what the name implies)
		var digitScale = element.GetFloatAttribute("DoubleDigitsScale");
		
		//get blue ones digit
		var blueOne = (blueCount%10)/1;
		//get blue tens digit
		var blueTen = (blueCount%100)/10;

		//get red ones digit
		var redOne = (redCount%10)/1;
		//get red tens digit
		var redTen = (redCount%100)/10;
		
		//get if blue has 2 digits
		var blueDouble = (blueTen != 0);
		//get if red has 2 digits
		var redDouble = (redTen != 0);
		
		//get position of the blue ones digit
		var blueOneDigit = new Vector2(blueX + (blueDouble?oneDigitX:0), blueDouble?doubleY:y);
		//get position of the blue tens digit
		var blueTenDigit = new Vector2(blueX + tenDigitX, doubleY);

		//get position of the red ones digit
		var redOneDigit = new Vector2(redX + (redDouble?oneDigitX:0), redDouble?doubleY:y);
		//get position of the red tens digit
		var redTenDigit = new Vector2(redX + tenDigitX, doubleY);
		
		//get blue transform
		var blueOnesTrans = trans.TranslatedLocal(blueOneDigit);
		var blueTensTrans = trans.TranslatedLocal(blueTenDigit);
		//get red transform
		var redOnesTrans = trans.TranslatedLocal(redOneDigit);
		var redTensTrans = trans.TranslatedLocal(redTenDigit);
		
		//if not two digits, scale accourdingly
		if(!blueDouble) blueOnesTrans = blueOnesTrans.ScaledLocal(Vector2.One/digitScale);
		if(!redDouble) redOnesTrans = redOnesTrans.ScaledLocal(Vector2.One/digitScale);
		
		//get font names
		var redFont = element.GetAttribute("RedDigitFont");
		var blueFont = element.GetAttribute("BlueDigitFont");
		
		//get file names
		var redOneName = $"Digit{redOne}_{redFont}.png";
		var redTenName = $"Digit{redTen}_{redFont}.png";
		
		var blueOneName = $"Digit{blueOne}_{blueFont}.png";
		var blueTenName = $"Digit{blueTen}_{blueFont}.png";
		
		//draw
		
		DrawAssetFromData(blueOnesTrans, Vector2.Zero, $"{swfPath}/{blueOneName}", priority: DIGIT_PRIORITY);
		if(blueDouble)DrawAssetFromData(blueTensTrans, Vector2.Zero, $"{swfPath}/{blueTenName}", priority: DIGIT_PRIORITY);
		DrawAssetFromData(redOnesTrans, Vector2.Zero, $"{swfPath}/{redOneName}", priority: DIGIT_PRIORITY);
		if(redDouble)DrawAssetFromData(redTensTrans, Vector2.Zero, $"{swfPath}/{redTenName}", priority: DIGIT_PRIORITY);
	}
	
	//////////////////////////////////////////
	////////////////Dynamic///////////////////
	//////////////////////////////////////////
	
	public void DrawGenericDynamic(XElement element, Vector2 offset = default)
	{
		var platid = element.GetIntAttribute("PlatID");
		var stepper = movingPlatformsDict[platid];
		var pos = stepper.GetCurrent() + offset - stepper.keyframes[0].position + element.GetElementPositionOrDefault();
		
		if(cf.Display["MovingPlatformData"])
		{
			if(cf.Display["MovingPlatformsTime"])
			DrawString($"Time: {stepper.time}", pos + cf.Sizes["MovingPlatformTimeOffset"]*Vector2.Up);
			
			if(cf.Display["MovingPlatformsID"])
			DrawString($"PlatID: {platid}", pos + cf.Sizes["MovingPlatformPlatIDOffset"]*Vector2.Up);
			
			if(cf.Display["MovingPlatformsPosition"])
			DrawCircle(pos, cf.Sizes["MovingPlatformRadius"], cf.Colors["MovingPlatform"]);
		}

		element.Elements()
			.Where(HasGenerator)
			.ForEach(e => GetGenerator(e)(e,pos));
	}
	
	public void DrawDynamicCollision(XElement element, Vector2 offset = default) => DrawGenericDynamic(element, offset);
	public void DrawDynamicRespawn(XElement element, Vector2 offset = default) => DrawGenericDynamic(element, offset);
	public void DrawDynamicItemSpawn(XElement element, Vector2 offset = default) => DrawGenericDynamic(element, offset);
	public void DrawDynamicNavNode(XElement element, Vector2 offset = default) => DrawGenericDynamic(element, offset);
}
