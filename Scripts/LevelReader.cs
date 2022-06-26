#define SHOW_CAMERA_BOUNDS
//#define SHOW_SPAWNBOT_BOUNDS
#define SHOW_BLASTZONE_BOUNDS

//#define SHOW_NAVNODES
//#define SHOW_NAVMESH

//#define SHOW_MOVING_PLATFORM_POSITION
	#define SHOW_MOVING_PLATFORM_TIME

#define SHOW_ASSETS
	#define SHOW_BACKGROUND
	//#define SHOW_PLATFORM_LABEL
	//#define SHOW_ASSET_POSITION

#define SHOW_SPECIAL
	#define SHOW_RESPAWNS
	
	#define SHOW_ITEMS
	
	#define SHOW_COLLISION
		#define SHOW_TEAM_COL
		
		//#define SHOW_ANCHORS
		
		//#define SHOW_NORMALS
		
		//#define SHOW_TRAP_POWERS
		//#define SHOW_TRAP_POWER_OFFSET
		//#define SHOW_TRAP_COOLDOWN
		
		//#define SHOW_TAUNT_EVENT
	
	#define SHOW_GOALS

using Godot;
using System;
using System.Xml.Linq;
using System.Linq;
using System.Collections.Generic;
using DrawAction = System.Action<Godot.CanvasItem>;
using Generator = System.Func<System.Xml.Linq.XElement, Godot.Vector2, System.Action<Godot.CanvasItem>>;
using AssetGenerator = System.Func<System.Xml.Linq.XElement, Godot.Transform2D, System.Action<Godot.CanvasItem>>;

public class LevelReader
{
	public const float DEFAULT_RADIUS = 50f;
	public const float ANCHOR_RADIUS = 30f;
	public const float FIRE_OFFSET_RADIUS = 10f;
	public const float MOVING_PLATFORM_RADIUS = 10f;
	public const float NAVNODE_RADIUS = 10f;
	public const float NORMAL_LENGTH = 50f;
	
	public const float TEAM_LINES_OFFSET = 5f;
	
	public const float PRESSURE_PLATE_POWERS_OFFSET = 50f;
	public const float PRESSURE_PLATE_COOLDOWN_OFFSET = -50f;
	
	public const float COLLISION_TAUNT_EVENT_OFFSET = 60f;
	
	public const float MOVING_PLATFORM_TIME_OFFSET = 45f;
	public const float MOVING_PLATFORM_PLATID_OFFSET = 10f;
	
	public const float NAVNODE_ID_OFFSET = 10f;
	
	public const float PRESSURE_PLATE_DIR_LINE_LENGTH = 50f;
	public const float PRESSURE_PLATE_DIR_LINE_SIDE_OFFSET_X = 10f;
	public const float PRESSURE_PLATE_DIR_LINE_SIDE_OFFSET_Y = 10f;
	
	public static readonly Font FONT = ResourceLoader.Load<Font>("res://BrawlhallaFont.tres");
	
	public static readonly DrawAction EMPTY_ACTION = (ci) => {};
	public static readonly Generator EMPTY_GENERATOR = (ci, offset) => EMPTY_ACTION;
	public static readonly AssetGenerator EMPTY_ASSET_GENERATOR = (ci, trans) => EMPTY_ACTION;
	
	public static readonly Color CAMERA_BOUNDS = new Color(1, 0, 0, 0.5f);
	public static readonly Color SPAWNBOT_BOUNDS = new Color(1, 1, 0.8f, 0.5f);
	public static readonly Color BLASTZONE_BOUNDS = new Color(1, 1, 1, 1);
	
	public static readonly Color ITEM_SPAWN = new Color(0, 0.5f, 1, 0.5f);
	public static readonly Color INITIAL_ITEM_SPAWN = new Color(0.5f, 0, 0.5f, 0.5f);
	public static readonly Color ITEM_SET = new Color(0, 0, 0.5f, 0.5f);
	
	
	public static readonly Color RESPAWN = new Color(1, 0.5f, 0, 0.5f);
	public static readonly Color INITIAL_RESPAWN = new Color(1, 0, 0, 0.5f);
	public static readonly Color EXPANDED_INITIAL_RESPAWN = new Color(1, 0, 1, 0.5f);
	
	
	public static readonly Color HARD_COLLISION = new Color(0, 1, 0, 1);
	public static readonly Color SOFT_COLLISION = new Color(1, 1, 0, 1);
	public static readonly Color NOSLIDE_COLLISION = new Color(0, 1, 1, 1);
	
	public static readonly Color GAMEMODE_HARD_COLLISION = new Color(0.8f, 1, 0.8f, 1);
	public static readonly Color GAMEMODE_SOFT_COLLISION = new Color(1, 1, 0.8f, 1);
	public static readonly Color GAMEMODE_NOSLIDE_COLLISION = new Color(0.8f, 1, 1, 1);
	
	public static readonly Color BOUNCY_HARD_COLLISION = new Color(0.2f, 0.6f, 0.2f, 1);
	public static readonly Color BOUNCY_SOFT_COLLISION = new Color(0.6f, 0.6f, 0.2f, 1);
	public static readonly Color BOUNCY_NOSLIDE_COLLISION = new Color(0.2f, 0.6f, 0.6f, 1);
	
	public static readonly Color PRESSURE_PLATE_COLLISION = new Color(0.8f, 0.4f, 0.1f, 1);
	public static readonly Color SOFT_PRESSURE_PLATE_COLLISION = new Color(0.5f, 0.1f, 0, 1);
	
	public static readonly Color PRESSURE_PLATE_LINE = new Color(1,0.5f,0,1);
	public static readonly Color PRESSURE_PLATE_FIRE_OFFSET = new Color(1,0.1f,0,0.3f);
	
	public static readonly Color NORMAL_LINE = new Color(0.9f,0.9f,0.9f,0.5f);
	
	public static readonly Color[] TEAM_COLLISION = new Color[]
	{
		new Color(1, 0.65f, 0, 1),
		new Color(1, 0, 0, 1),
		new Color(0, 0, 1, 1),
		new Color(0, 1, 0, 1),
		new Color(1, 1, 0, 1),
		new Color(1, 0, 1, 1)
	};
	
	public static readonly Color[] GOAL_COLORS = new Color[]
	{
		new Color(1, 0.65f, 0, 0.2f),
		new Color(1, 0, 0, 0.2f),
		new Color(0, 0, 1, 0.2f),
		new Color(0, 1, 0, 0.2f),
		new Color(1, 1, 0, 0.2f),
		new Color(1, 0, 1, 0.2f)
	};
	
	public static readonly Color MOVING_PLATFORM = new Color(0, 1, 0.84f, 0.3f);
	
	public static readonly Dictionary<string, Color> NAVNODE_COLORS = new Dictionary<string, Color>
	{
		{"", new Color(0.5f, 0.5f, 0.5f, 0.3f)},
		{"A", new Color(0, 0, 1, 0.3f)},
		{"D", new Color(1, 0, 0, 0.3f)},
		{"G", new Color(1, 0.647f, 0, 0.3f)},
		{"L", new Color(0, 1, 0, 0.3f)},
		{"W", new Color(1, 1, 0, 0.3f)}
	};
	
	public XDocument parsedMapFile;
	public XDocument parsedLevelTypes;
	
	public Dictionary<string, Generator> generators;
	public Dictionary<string, AssetGenerator> assetGenerators;
	public Dictionary<int, KeyframeStepper> movingPlatformsDict;
	public Dictionary<int, Vector2> navnodesPositions;
	
	public int defaultNumFrames;
	public float defaultSlowMult;
	
	public float blastzoneLeft;
	public float blastzoneRight;
	public float blastzoneTop;
	public float blastzoneBottom;
	public float globalStartFrame;
	
	public long callCount;
	public string assetDir;
	
	public bool globalizeMovingPlatformPosition = false;
	public bool noSkulls = false;
	
	public string mapFolder;
	public string mapName;
	public string levelTypesPath;
	public string mapArtPath;
	
	public LevelReader() {}
	
	public LevelReader(string mapFolder, string mapName, string levelTypesPath, string mapArtPath)
	{
		SetupReader(mapFolder, mapName, levelTypesPath, mapArtPath);
	}
	
	public void SetupReader(string mapFolder, string mapName, string levelTypesPath, string mapArtPath)
	{
		this.mapFolder = mapFolder;
		this.mapName = mapName;
		this.levelTypesPath = levelTypesPath;
		this.mapArtPath = mapArtPath;
		
		parsedMapFile = XDocument.Parse(Read($"{mapFolder}/{mapName}.xml"));
		parsedLevelTypes = (levelTypesPath == "")?null:XDocument.Parse(Read(levelTypesPath));
		InitGenerators();
		Reset();
	}
	
	public void Reset()
	{
		callCount = 0;
		
		var firstMap = parsedMapFile.FirstNode as XElement;
		assetDir = firstMap.GetAttribute("AssetDir");
		
		
		var firstLevelTypes = parsedLevelTypes?.FirstNode as XElement;
		
		var levelTypeElement = firstLevelTypes?
									.Elements("LevelType")
									.Where(e => e.GetAttribute("LevelName", "") == mapName)
									.FirstOrDefault();
		
		blastzoneLeft = levelTypeElement?.GetFloatSubElementValue("LeftKill", 0)??0;
		blastzoneRight = levelTypeElement?.GetFloatSubElementValue("RightKill", 0)??0;
		blastzoneTop = levelTypeElement?.GetFloatSubElementValue("TopKill", 0)??0;
		blastzoneBottom = levelTypeElement?.GetFloatSubElementValue("BottomKill", 0)??0;
		globalStartFrame = levelTypeElement?.GetFloatSubElementValue("StartFrame", 0)??0;
		
		defaultNumFrames = firstMap.GetIntAttribute("NumFrames", -1);
		defaultSlowMult = firstMap.GetFloatAttribute("SlowMult", 1f);
		
		ResetTime();
		navnodesPositions = new Dictionary<int, Vector2>();
		foreach(var stepper in movingPlatformsDict.Values) stepper.AdvanceTime(globalStartFrame);
	}
	
	public void InitGenerators()
	{
		assetGenerators = new Dictionary<string, AssetGenerator>()
		{
			#if SHOW_ASSETS
			
			#if SHOW_BACKGROUND
			{"Background", GenerateBackgroundAction},
			#endif
			
			{"Platform", GeneratePlatformAction},
			{"MovingPlatform", GenerateMovingPlatformAction}
			
			#endif
		};
		
		generators = new Dictionary<string, Generator>()
		{
			#if SHOW_SPECIAL
			
			#if SHOW_CAMERA_BOUNDS
			{"CameraBounds", GenerateCameraBoundsAction},
			#endif
			
			#if SHOW_SPAWNBOT_BOUNDS
			{"SpawnBotBounds", GenerateSpawnBotBoundsAction},
			#endif
			
			#if SHOW_ITEMS
			{"ItemSpawn", GenerateItemSpawnAction},
			{"ItemInitSpawn", GenerateInitialItemSpawnAction},
			{"ItemSet", GenerateItemSetAction},
			{"DynamicItemSpawn", GenerateDynamicItemSpawnAction},
			#endif
			
			#if SHOW_RESPAWNS
			{"Respawn", GenerateRespawnAction},
			{"DynamicRespawn", GenerateDynamicRespawnAction},
			#endif
			
			#if SHOW_COLLISION
			{"HardCollision", GenerateHardCollisionAction},
			{"SoftCollision", GenerateSoftCollisionAction},
			{"NoSlideCollision", GenerateNoSlideCollisionAction},
			
			{"GameModeHardCollision", GenerateGamemodeHardCollisionAction},
			{"GameModeSoftCollision", GenerateGamemodeSoftCollisionAction},
			{"GameModeNoSlideCollision", GenerateGamemodeNoSlideCollisionAction},
			
			{"BouncyHardCollision", GenerateBouncyHardCollisionAction},
			{"BouncySoftCollision", GenerateBouncySoftCollisionAction},
			{"BouncyNoSlideCollision", GenerateBouncyNoSlideCollisionAction},
			
			{"PressurePlateCollision", GeneratePressurePlateCollisionAction},
			{"SoftPressurePlateCollision", GenerateSoftPressurePlateCollisionAction},
			
			{"DynamicCollision", GenerateDynamicCollisionAction},
			#endif
			
			{"MovingPlatform", GenerateSecondaryMovingPlatformAction},
			
			#if SHOW_NAVNODES
			{"NavNode", GenerateNavNodeAction},
			{"DynamicNavNode", GenerateDynamicNavNodeAction},
			#endif
			
			#if SHOW_GOALS
			{"Goal", GenerateGoalAction},
			#endif
			
			#endif
		};
	}
	
	public string Read(string filepath)
	{
		var f = new File();//create new file
		var er = f.Open(filepath, File.ModeFlags.Read);//open file
		if(er != Error.Ok) throw new ArgumentException($"Error {er} while reading file {filepath}");
		var content = f.GetAsText();//read text
		f.Close();//flush buffer
		return content;
	}
	
	public Generator GetGenerator(string s) => (generators.ContainsKey(s))?(generators[s]):EMPTY_GENERATOR;
	public AssetGenerator GetAssetGenerator(string s) => (assetGenerators.ContainsKey(s))?(assetGenerators[s]):EMPTY_ASSET_GENERATOR;
	
	public DrawAction GenerateDrawAction(float timepass)
	{
		if(Input.IsActionJustPressed("toggle_gloablized_moving_platform_position"))
		{
			globalizeMovingPlatformPosition = !globalizeMovingPlatformPosition;
			ResetTime();
		}
		
		if(Input.IsActionJustPressed("toggle_no_skulls")) noSkulls = !noSkulls;
		
		if(Input.IsActionJustPressed("reset_time")) ResetTime();
		
		navnodesPositions = new Dictionary<int, Vector2>();
		
		AdvanceTime(timepass);
		
		var first = parsedMapFile.FirstNode as XElement;
		
		return (ci) =>
		{
			#if SHOW_ASSETS
			first.Elements().Select(e => GetAssetGenerator(e.Name.LocalName)(e,Transform2D.Identity)).Combine()(ci);
			#endif
			
			#if SHOW_SPECIAL
			first.Elements().Select(e => GetGenerator(e.Name.LocalName)(e,Vector2.Zero)).Combine()(ci);
			#endif
			
			#if SHOW_NAVMESH
			GenerateNavMeshActionList(first).Combine()(ci);
			#endif
			
			#if SHOW_BLASTZONE_BOUNDS
			GenerateBlastzoneBoundsAction(first)(ci);
			#endif
			
			callCount++;
		};
	}
	
	public void ResetTime()
	{
		movingPlatformsDict = new Dictionary<int, KeyframeStepper>();
		(parsedMapFile.FirstNode as XElement).Elements("MovingPlatform").ForEach(SetupMovingPlatform);
	}
	
	public void AdvanceTime(float time) => movingPlatformsDict.Values.ForEach(s => s.AdvanceTime(time));
	
	public IEnumerable<DrawAction> GenerateNavMeshActionList(XElement element) => element.Elements("DynamicNavNode").Prepend(element).SelectMany(e => e.Elements("NavNode")).Select(GenerateNavLineAction);
	
	public DrawAction GenerateBlastzoneBoundsAction(XElement element)
	{
		var camerabounds = element.Elements("CameraBounds").First().GetElementRect();
		var rect = camerabounds.GrowIndividual(blastzoneLeft, blastzoneTop, blastzoneRight, blastzoneBottom);
		return (ci) =>
		{
			if(ci is Node n)
			{
				var navcam = n.GetNode<NavigationCamera>("Camera");
				if(Input.IsActionJustPressed("fit_camera")) navcam.FitToRect(camerabounds);
				if(Input.IsActionJustPressed("fit_blastzones")) navcam.FitToRect(rect);
			}
			
			#if SHOW_BLASTZONE_BOUNDS
			ci.DrawRect(rect, BLASTZONE_BOUNDS, false);
			#endif
		};
		
	}
	
	public void SetupMovingPlatform(XElement element)
	{
		var pos = element.GetElementPositionOrDefault();
		
		var platid = element.GetIntAttribute("PlatID");
		var animationElement = element.Elements("Animation").First();
		
		var numframes_defaultto = (defaultNumFrames == -1)?"":$"{defaultNumFrames}";
		var numframes = int.Parse(animationElement.GetAttribute("NumFrames", numframes_defaultto));
		var startframe = animationElement.GetIntAttribute("StartFrame", 1) - 1;
		var mult = animationElement.GetFloatAttribute("SlowMult", defaultSlowMult);
		var hasCenter = animationElement.HasAttribute("CenterX") || element.HasAttribute("CenterY");
		var center = animationElement.GetElementPositionOrDefault("Center");
		
		var data = animationElement.GetElementKeyframes(mult, hasCenter, center);
		
		var stepper = new KeyframeStepper(data, globalizeMovingPlatformPosition?pos:Vector2.Zero, numframes*mult-1);
		
		stepper.AdvanceTime(startframe*mult + globalStartFrame);
		movingPlatformsDict.Add(platid, stepper);
	}
	
	//////////////////////////////////////////
	//////////////////Bounds//////////////////
	//////////////////////////////////////////
	public DrawAction GenerateGenericBoundsAction(XElement element, Vector2 offset, Color color)
	{
		var rect = element.GetElementRect();
		rect.Position += offset;
		return (ci) => ci.DrawRect(rect, color, false);
	}
	
	public DrawAction GenerateCameraBoundsAction(XElement element, Vector2 offset) => GenerateGenericBoundsAction(element, offset, CAMERA_BOUNDS);
	public DrawAction GenerateSpawnBotBoundsAction(XElement element, Vector2 offset) => GenerateGenericBoundsAction(element, offset, SPAWNBOT_BOUNDS);
	
	//////////////////////////////////////////
	///////////////Item Spawns////////////////
	//////////////////////////////////////////
	public DrawAction GenerateGenericAreaAction(XElement element, Vector2 offset, Color color)
	{
		var rect = element.GetElementRect();
		rect.Position += offset;
		if(rect.Size.x == 0 && rect.Size.y == 0)
		{
			var newrect = new Rect2(rect.Position - DEFAULT_RADIUS*Vector2.One, 2f*DEFAULT_RADIUS*Vector2.One);
			return (ci) => ci.DrawRect(newrect, color, false);
		}
		else if(rect.Size.x == 0 || rect.Size.y == 0)
			return (ci) => ci.DrawLine(rect.Position, rect.End, color);
		else
			return (ci) => ci.DrawRect(rect, color, true);
	}
	
	public DrawAction GenerateItemSpawnAction(XElement element, Vector2 offset) => GenerateGenericAreaAction(element, offset, ITEM_SPAWN);
	public DrawAction GenerateInitialItemSpawnAction(XElement element, Vector2 offset) => GenerateGenericAreaAction(element, offset, INITIAL_ITEM_SPAWN);
	public DrawAction GenerateItemSetAction(XElement element, Vector2 offset) => GenerateGenericAreaAction(element, offset, ITEM_SET);
	
	//////////////////////////////////////////
	/////////////////Respawns/////////////////
	//////////////////////////////////////////
	public DrawAction GenerateRespawnAction(XElement element, Vector2 offset)
	{
		var initial = element.GetBooleanAttribute("Initial");
		var expandedInit = element.GetBooleanAttribute("ExpandedInit");
		
		Color chosenColor;
		if(initial) chosenColor = INITIAL_RESPAWN;
		else if(expandedInit) chosenColor = EXPANDED_INITIAL_RESPAWN;
		else chosenColor = RESPAWN;
		
		var pos = element.GetElementPosition();
		pos += offset;
		return (ci) => ci.DrawCircle(pos, DEFAULT_RADIUS, chosenColor);
	}
	
	//////////////////////////////////////////
	////////////////Collision/////////////////
	//////////////////////////////////////////
	public DrawAction GenerateGenericCollisionAction(XElement element, Vector2 offset, Color color)
	{
		(var @from, var to) = element.GetElementPoints();
		@from += offset; to += offset;
		var dir = (to-@from).Normalized();
		var clockwise_dir = new Vector2(-dir.y, dir.x);
		
		#if SHOW_NORMALS
		var normal = clockwise_dir;
		if(element.HasAttribute("NormalX")) normal.x = element.GetFloatAttribute("NormalX");
		if(element.HasAttribute("NormalY")) normal.y = element.GetFloatAttribute("NormalY");
		var normal_start = (@from+to)/2f;
		var normal_end = normal_start + NORMAL_LENGTH * normal;
		#endif
		
		DrawAction action = (ci) =>
		{
			ci.DrawLine(@from, to, color);
			
			#if SHOW_NORMALS
			ci.DrawLine(normal_start, normal_end, NORMAL_LINE);
			#endif
		};
		
		#if SHOW_TEAM_COL
		if(element.HasAttribute("Team"))
		{
			var teamoffset = TEAM_LINES_OFFSET * clockwise_dir;
			var team = element.GetIntAttribute("Team");
			action = action.Chain(
				(ci) =>
				{
					ci.DrawLine(@from+teamoffset, to+teamoffset, TEAM_COLLISION[team]);
					ci.DrawLine(@from-teamoffset, to-teamoffset, TEAM_COLLISION[team]);
				}
			);
		}
		#endif
		
		#if SHOW_TAUNT_EVENT
		if(element.HasAttribute("TauntEvent"))
		{
			var tauntevent = element.GetAttribute("TauntEvent");
			var labelPos = new Vector2(Math.Min(@from.x, to.x), Math.Min(@from.y, to.y));
			action = action.Chain(
				(ci) => ci.DrawString(FONT, labelPos + COLLISION_TAUNT_EVENT_OFFSET*Vector2.Up, $"TauntEvent: {tauntevent}")
			);
		}
		#endif
		
		#if SHOW_ANCHORS
		if(element.HasAttribute("AnchorX") && element.HasAttribute("AnchorY"))
		{
			var anchor = element.GetElementPosition("Anchor");
			var more_transparent = new Color(color.r, color.g, color.b, 0.3f);
			action = action.Chain(
				(ci) => ci.DrawCircle(anchor, ANCHOR_RADIUS, more_transparent)
			);
		}
		#endif
		
		return action;
	}
	
	public DrawAction GenerateHardCollisionAction(XElement element, Vector2 offset) => GenerateGenericCollisionAction(element, offset, HARD_COLLISION);
	public DrawAction GenerateSoftCollisionAction(XElement element, Vector2 offset) => GenerateGenericCollisionAction(element, offset, SOFT_COLLISION);
	public DrawAction GenerateNoSlideCollisionAction(XElement element, Vector2 offset) => GenerateGenericCollisionAction(element, offset, NOSLIDE_COLLISION);
	
	public DrawAction GenerateGenericGamemodeCollisionAction(XElement element, Vector2 offset, Color color) => GenerateGenericCollisionAction(element, offset, color);
	public DrawAction GenerateGamemodeHardCollisionAction(XElement element, Vector2 offset) => GenerateGenericGamemodeCollisionAction(element, offset, GAMEMODE_HARD_COLLISION);
	public DrawAction GenerateGamemodeSoftCollisionAction(XElement element, Vector2 offset) => GenerateGenericGamemodeCollisionAction(element, offset, GAMEMODE_SOFT_COLLISION);
	public DrawAction GenerateGamemodeNoSlideCollisionAction(XElement element, Vector2 offset) => GenerateGenericGamemodeCollisionAction(element, offset, GAMEMODE_NOSLIDE_COLLISION);
	
	public DrawAction GenerateGenericBouncyCollisionAction(XElement element, Vector2 offset, Color color) => GenerateGenericCollisionAction(element, offset, color);
	public DrawAction GenerateBouncyHardCollisionAction(XElement element, Vector2 offset) => GenerateGenericBouncyCollisionAction(element, offset, BOUNCY_HARD_COLLISION);
	public DrawAction GenerateBouncySoftCollisionAction(XElement element, Vector2 offset) => GenerateGenericBouncyCollisionAction(element, offset, BOUNCY_SOFT_COLLISION);
	public DrawAction GenerateBouncyNoSlideCollisionAction(XElement element, Vector2 offset) => GenerateGenericBouncyCollisionAction(element, offset, BOUNCY_NOSLIDE_COLLISION);
	
	public DrawAction GenerateGenericPressurePlateCollisionAction(XElement element, Vector2 offset, Color color)
	{
		(var @from, var to) = element.GetElementPoints();
		@from += offset; to += offset;
		var labelPos = new Vector2(Math.Min(@from.x, to.x), Math.Min(@from.y, to.y));
		var powers = element.GetAttribute("TrapPowers").Replace(",", " ");
		var cooldown = element.GetIntAttribute("Cooldown");
		
		var faceleft = bool.Parse(element.GetAttribute("FaceLeft",""));
		var dirmult = faceleft?1:-1;
		
		var fireoffset = element.GetElementPosition("FireOffset");
		
		var middle = (@from+to)/2f;
		var firePos = fireoffset + offset;
		
		var action =  GenerateGenericCollisionAction(element, offset, color).Chain(
			(ci) =>
			{
				#if SHOW_TRAP_POWERS
				ci.DrawString(FONT, firePos + PRESSURE_PLATE_POWERS_OFFSET*Vector2.Up, $"Powers: {powers}");
				#endif
				
				#if SHOW_TRAP_COOLDOWN
				ci.DrawString(FONT, labelPos + PRESSURE_PLATE_COOLDOWN_OFFSET*Vector2.Up, $"Cooldown: {cooldown}f");
				#endif
				
				#if SHOW_TRAP_POWER_OFFSET
				ci.DrawCircle(firePos, FIRE_OFFSET_RADIUS, PRESSURE_PLATE_FIRE_OFFSET);
				ci.DrawLine(middle, firePos, PRESSURE_PLATE_FIRE_OFFSET);
				#endif
			}
		);
		
		#if SHOW_TRAP_POWER_OFFSET
		var lineend = firePos + dirmult*PRESSURE_PLATE_DIR_LINE_LENGTH*Vector2.Left;
		var sideline1 = lineend + dirmult*new Vector2(PRESSURE_PLATE_DIR_LINE_SIDE_OFFSET_X, PRESSURE_PLATE_DIR_LINE_SIDE_OFFSET_Y);
		var sideline2 = lineend + dirmult*new Vector2(PRESSURE_PLATE_DIR_LINE_SIDE_OFFSET_X, -PRESSURE_PLATE_DIR_LINE_SIDE_OFFSET_Y);
		
		action = action.Chain(
			(ci) =>
			{
				ci.DrawLine(firePos, lineend, PRESSURE_PLATE_LINE);
				ci.DrawLine(lineend, sideline1, PRESSURE_PLATE_LINE);
				ci.DrawLine(lineend, sideline2, PRESSURE_PLATE_LINE);
			}
		);
		#endif
		
		return action;
	}
	
	public DrawAction GeneratePressurePlateCollisionAction(XElement element, Vector2 offset) => GenerateGenericPressurePlateCollisionAction(element, offset, PRESSURE_PLATE_COLLISION);
	public DrawAction GenerateSoftPressurePlateCollisionAction(XElement element, Vector2 offset) => GenerateGenericPressurePlateCollisionAction(element, offset, SOFT_PRESSURE_PLATE_COLLISION);
	
	//////////////////////////////////////////
	///////////////////Misc///////////////////
	//////////////////////////////////////////
	public DrawAction GenerateGoalAction(XElement element, Vector2 offset)
	{
		var goal = element.GetIntAttribute("Team", 1);
		return GenerateGenericAreaAction(element, offset, GOAL_COLORS[goal]);
	}
	
	//////////////////////////////////////////
	//////////////Navigation//////////////////
	//////////////////////////////////////////
	
	public DrawAction GenerateNavNodeAction(XElement element, Vector2 offset)
	{
		var pos = element.GetElementPositionOrDefault() + offset;
		var navid = element.GetAttribute("NavID");
		var id = NormalizeNavID(navid);
		var type = GetNavType(navid);
		navnodesPositions.Add(id, pos);
		return (ci) =>
		{
			ci.DrawCircle(pos, NAVNODE_RADIUS, NAVNODE_COLORS[type]);
			ci.DrawString(FONT, pos + NAVNODE_ID_OFFSET*Vector2.Up, $"NavID: {navid}");
		};
	}
	
	public DrawAction GenerateNavLineAction(XElement element)
	{
		var navid = element.GetAttribute("NavID");
		var id = NormalizeNavID(navid);
		var type = GetNavType(navid);
		
		var pos = navnodesPositions[id];
		var action = EMPTY_ACTION;
		
		var firstcall = (callCount == 0);
		
		return element
			.GetAttribute("Path")//get path
			.Split(",")//split to parts
			.Select(//add line actions
				(s) =>
				{
					var norms = NormalizeNavID(s);
					var types = GetNavType(s);
					if(!navnodesPositions.ContainsKey(norms))
					{
						if(firstcall)
						{
							var stypedesc = (types!="")?$" with type {types}":"";
							GD.Print($"NavNode {navid} connects to NavNode {norms}{stypedesc}, but there's no NavNode with ID {norms}");
						}
						return EMPTY_ACTION;
					}
					
					return (ci) => ci.DrawLine(pos, (pos+navnodesPositions[norms])/2f, NAVNODE_COLORS[types]);
				}
		).Combine();
	}
	
	private int NormalizeNavID(string s)
	{
		char first = s[0];
		if(first < '0' || '9' < first) s = s.Substring(1);
		return int.Parse(s);
	}
	
	private string GetNavType(string s)
	{
		string first = $"{s[0]}";
		return NAVNODE_COLORS.ContainsKey(first)?first:"";
	}
	
	//////////////////////////////////////////
	////////////////Assets////////////////////
	//////////////////////////////////////////
	public DrawAction GenerateGenericAssetAction(XElement element, Transform2D trans, bool doOffset, string assetfolder, string instanceName, string assetNameOverride = "")
	{
		var offset = doOffset?element.GetElementPositionOrDefault():Vector2.Zero;
		
		var bounds = element.GetElementBoundsOrDefault();
		if(bounds.x < 0f) trans *= Transform2D.FlipX;
		if(bounds.y < 0f) trans *= Transform2D.FlipY;
		
		var assetname = (assetNameOverride == "")?element.GetAttribute("AssetName"):assetNameOverride;
		
		var assetpath = assetname.StartsWith("../") ?
		assetname.Substring("../".Length) :
		$"{assetfolder}/{assetname}";
		
		var texture = Utils.LoadImageFromPath($"{mapArtPath}/{assetpath}", instanceName, bounds);
		
		if(
			texture is null ||
			(!noSkulls && instanceName == "am_NoSkulls") ||
			(instanceName == "am_Holiday")
		) return EMPTY_ACTION;
		
		return (ci) =>
		{
			#if SHOW_ASSET_POSITION
			ci.DrawCircle(offset, 10f, new Color(0,0,0,1));
			#endif
			
			ci.DrawSetTransformMatrix(trans);
			ci.DrawTexture(texture, offset);
			ci.DrawSetTransformMatrix(Transform2D.Identity);
		};
	}
	
	public DrawAction GenerateBackgroundAction(XElement element, Transform2D trans)
	{
		if(noSkulls && element.GetBooleanAttribute("HasSkulls", false)) return EMPTY_ACTION;
		return GenerateGenericAssetAction(element.Parent.Elements("CameraBounds").First(), trans, true, "Backgrounds", "", element.GetAttribute("AssetName"));
	}
	
	public DrawAction GeneratePlatformAction(XElement element, Transform2D trans)
	{
		var instanceName = element.GetAttribute("InstanceName");
		
		trans = trans.Translated(element.GetElementPositionOrDefault());
		
		trans.Scale *= element.GetFloatAttribute("Scale", 1);
		trans.Scale *= Vector2.Right * element.GetFloatAttribute("ScaleX", 1) + Vector2.Down;
		trans.Scale *= Vector2.Down * element.GetFloatAttribute("ScaleY", 1) + Vector2.Right;
		
		trans.Rotation += element.GetFloatAttribute("Rotation").ToRad();
		
		if(!element.HasAttribute("AssetName"))
		{
			var assetActions = element.Elements("Asset").Select(e => GenerateGenericAssetAction(e, trans, true, assetDir, instanceName));
			var platformActions = element.Elements("Platform").Select(e => GeneratePlatformAction(e, trans));
			var actions = assetActions.Concat(platformActions);
			
			#if SHOW_PLATFORM_LABEL
			actions = actions.Append((ci) => ci.DrawString(FONT, trans.origin, $"InstanceName: {instanceName}"));
			#endif
			
			return actions.Combine();
		}
		else return GenerateGenericAssetAction(element, trans, false, assetDir, instanceName);
	}
	
	public DrawAction GenerateMovingPlatformAction(XElement element, Transform2D trans)
	{
		var platid = element.GetIntAttribute("PlatID");
		var stepper = movingPlatformsDict[platid];
		trans = trans.Translated(stepper.GetCurrent());
		
		if(!globalizeMovingPlatformPosition) trans = trans.Translated(element.GetElementPositionOrDefault());
		
		return element.Elements().Select(e => GetAssetGenerator(e.Name.LocalName)(e,trans)).Combine();
	}
	
	public DrawAction GenerateSecondaryMovingPlatformAction(XElement element, Vector2 offset)
	{
		var platid = element.GetIntAttribute("PlatID");
		var stepper = movingPlatformsDict[platid];
		var pos = stepper.GetCurrent() + offset;
		
		if(!globalizeMovingPlatformPosition) pos += element.GetElementPositionOrDefault();
		
		return (ci) => {
			#if SHOW_MOVING_PLATFORM_POSITION
				#if SHOW_MOVING_PLATFORM_TIME
					ci.DrawString(FONT, pos + MOVING_PLATFORM_TIME_OFFSET*Vector2.Up, $"Time: {stepper.time}");
				#endif
				
				ci.DrawString(FONT, pos + MOVING_PLATFORM_PLATID_OFFSET*Vector2.Up, $"PlatID: {platid}");
				ci.DrawCircle(pos, MOVING_PLATFORM_RADIUS, MOVING_PLATFORM);
			#endif
		};
	}
	
	//////////////////////////////////////////
	////////////////Dynamic///////////////////
	//////////////////////////////////////////
	
	public DrawAction GenerateGenericDynamicAction(XElement element, Vector2 offset)
	{
		var platid = element.GetIntAttribute("PlatID");
		var stepper = movingPlatformsDict[platid];
		var pos = stepper.GetCurrent() + offset;
		
		if(globalizeMovingPlatformPosition) return element.Elements().Select(e => GetGenerator(e.Name.LocalName)(e,pos)).Combine();
		
		pos += element.GetElementPositionOrDefault();
		DrawAction dynAct = (ci) => {
			#if SHOW_MOVING_PLATFORM_POSITION
				#if SHOW_MOVING_PLATFORM_TIME
					ci.DrawString(FONT, pos + MOVING_PLATFORM_TIME_OFFSET*Vector2.Up, $"Time: {stepper.time}");
				#endif
				
				ci.DrawString(FONT, pos + MOVING_PLATFORM_PLATID_OFFSET*Vector2.Up, $"PlatID: {platid}");
				ci.DrawCircle(pos, MOVING_PLATFORM_RADIUS, MOVING_PLATFORM);
			#endif
		};
		return element.Elements().Select(e => GetGenerator(e.Name.LocalName)(e,pos)).Append(dynAct).Combine();
	}
	
	public DrawAction GenerateDynamicCollisionAction(XElement element, Vector2 offset) => GenerateGenericDynamicAction(element, offset);
	public DrawAction GenerateDynamicRespawnAction(XElement element, Vector2 offset) => GenerateGenericDynamicAction(element, offset);
	public DrawAction GenerateDynamicItemSpawnAction(XElement element, Vector2 offset) => GenerateGenericDynamicAction(element, offset);
	public DrawAction GenerateDynamicNavNodeAction(XElement element, Vector2 offset) => GenerateGenericDynamicAction(element, offset);
}
