#define HIDE_NAVNODES
#define HIDE_NORMALS
#define HIDE_TRAP_POWERS
#define HIDE_DYNAMICS

using Godot;
using System;
using System.Xml.Linq;
using System.Linq;
using System.Collections.Generic;
using DrawAction = System.Action<Godot.CanvasItem>;
using Generator = System.Func<System.Xml.Linq.XElement, Godot.Vector2, System.Action<Godot.CanvasItem>>;

public class LevelReader
{
	public const float DEFAULT_RADIUS = 50f;
	public const float ANCHOR_RADIUS = 25f;
	public const float FIRE_OFFSET_RADIUS = 10f;
	public const float DYNAMIC_RADIUS = 20f;
	public const float NAVNODE_RADIUS = 10f;
	public const float NORMAL_LENGTH = 50f;
	
	public const float TEAM_LINES_OFFSET = 5f;
	
	public const float PRESSURE_PLATE_POWERS_OFFSET = 50f;
	public const float PRESSURE_PLATE_COOLDOWN_OFFSET = 50f;
	public const float COLLISION_TAUNT_EVENT_OFFSET = 60f;
	public const float DYNAMIC_TIME_OFFSET = 45f;
	public const float DYNAMIC_PLATID_OFFSET = 10f;
	public const float NAVNODE_ID_OFFSET = 10f;
	
	public const float PRESSURE_PLATE_DIR_LINE_LENGTH = 50f;
	public const float PRESSURE_PLATE_DIR_LINE_SIDE_OFFSET_X = 10f;
	public const float PRESSURE_PLATE_DIR_LINE_SIDE_OFFSET_Y = 10f;
	
	public static readonly Font FONT = ResourceLoader.Load<Font>("res://BrawlhallaFont.tres");
	
	public static readonly DrawAction EMPTY_ACTION = (ci) => {};
	public static readonly Generator EMPTY_GENERATOR = (ci, offset) => EMPTY_ACTION;
	
	public static readonly Color CAMERA_BOUNDS = new Color(1, 0, 0, 1);
	public static readonly Color SPAWNBOT_BOUNDS = new Color(1, 1, 0.6f, 1);
	public static readonly Color BLASTZONE_BOUNDS = new Color(0, 0, 0, 1);
	
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
	
	public static readonly Color BOUNCY_HARD_COLLISION = new Color(0.2f, 0.5f, 0.2f, 1);
	public static readonly Color BOUNCY_SOFT_COLLISION = new Color(0.5f, 0.5f, 0.2f, 1);
	public static readonly Color BOUNCY_NOSLIDE_COLLISION = new Color(0.2f, 0.5f, 0.5f, 1);
	
	public static readonly Color PRESSURE_PLATE_COLLISION = new Color(0.8f, 0.4f, 0.1f, 1);
	public static readonly Color SOFT_PRESSURE_PLATE_COLLISION = new Color(0.5f, 0.1f, 0, 1);
	
	public static readonly Color PRESSURE_PLATE_LINE = new Color(1,1,1,1);
	public static readonly Color PRESSURE_PLATE_FIRE_OFFSET = new Color(0,0,0,0.3f);
	
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
		new Color(1, 0.65f, 0, 0.1f),
		new Color(1, 0, 0, 0.1f),
		new Color(0, 0, 1, 0.1f),
		new Color(0, 1, 0, 0.1f),
		new Color(1, 1, 0, 0.1f),
		new Color(1, 0, 1, 0.1f)
	};
	
	public static readonly Color DYNAMIC_COLLISION = new Color(0, 1, 0.84f, 0.3f);
	public static readonly Color DYNAMIC_RESPAWN = new Color(1, 0.84f, 0, 0.3f);
	public static readonly Color DYNAMIC_ITEM_SPAWN = new Color(0.84f, 1, 0, 0.3f);
	public static readonly Color DYNAMIC_NAVNODE = new Color(0.84f, 1, 0.84f, 0.3f);
	
	public static readonly Dictionary<string, Color> NAVNODE_COLORS = new Dictionary<string, Color>
	{
		{"", new Color(0.5f, 0.5f, 0.5f, 0.3f)},
		{"A", new Color(0, 0, 1, 0.3f)},
		{"D", new Color(1, 0, 0, 0.3f)},
		{"G", new Color(1, 0.647f, 0, 0.3f)},
		{"L", new Color(0, 1, 0, 0.3f)},
		{"W", new Color(1, 1, 0, 0.3f)}
	};
	
	public XDocument document;
	public Dictionary<string, Generator> generators;
	public Dictionary<int, KeyframeStepper> dynamicMovementDict;
	public Dictionary<int, Vector2> navnodesPositions;
	public int defaultNumFrames;
	public float defaultSlowMult;
	
	public LevelReader() {}
	
	public LevelReader(string filepath)
	{
		var content = Read(filepath);
		document = XDocument.Parse(content);
		Reset();
	}
	
	public void Reset()
	{
		InitGenerators();
		
		dynamicMovementDict = new Dictionary<int, KeyframeStepper>();
		navnodesPositions = new Dictionary<int, Vector2>();
		
		var first = document.FirstNode as XElement;
		defaultNumFrames = first.GetIntAttribute("NumFrames", -1);
		defaultSlowMult = first.GetFloatAttribute("SlowMult", 1f);
		
		first.Elements("MovingPlatform").ForEach(SetupMovingPlatform);
	}
	
	public void InitGenerators()
	{
		generators = new Dictionary<string, Generator>()
		{
			{"CameraBounds", GenerateCameraBoundsAction},
			{"SpawnBotBounds", GenerateSpawnBotBoundsAction},
			
			
			{"ItemSpawn", GenerateItemSpawnAction},
			{"ItemInitSpawn", GenerateInitialItemSpawnAction},
			{"ItemSet", GenerateItemSetAction},
			{"DynamicItemSpawn", GenerateDynamicItemSpawnAction},
			
			{"Respawn", GenerateRespawnAction},
			{"DynamicRespawn", GenerateDynamicRespawnAction},
			
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
			
			
			#if HIDE_NAVNODES
			#else
			{"NavNode", GenerateNavNodeAction},
			{"DynamicNavNode", GenerateDynamicNavNodeAction},
			#endif
			
			
			{"Goal", GenerateGoalAction}
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
	public void AddGenerator(string s, Generator gen) => generators.Add(s, gen);
	public IEnumerable<DrawAction> GenerateActionList(float timepass)
	{
		navnodesPositions = new Dictionary<int, Vector2>();
		foreach(var i in dynamicMovementDict.Keys) dynamicMovementDict[i].AdvanceTime(timepass);
		var first = document.FirstNode as XElement;
		var actions = first.Elements().Select(e => GetGenerator(e.Name.LocalName)(e,Vector2.Zero));
		actions = actions.Concat(GenerateNavNodeActionList(first));
		return actions;
	}
	
	public IEnumerable<DrawAction> GenerateNavNodeActionList(XElement element)
	{
		#if HIDE_NAVNODES
		yield break;
		#endif
		
		var iterateOver = (element.Elements("DynamicNavNode").Prepend(element));
		foreach(var e in iterateOver) foreach(var n in e.Elements("NavNode"))
			yield return GenerateNavLineAction(n);
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
	public DrawAction GenerateGenericSpawnAction(XElement element, Vector2 offset, Color color)
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
	
	public DrawAction GenerateItemSpawnAction(XElement element, Vector2 offset) => GenerateGenericSpawnAction(element, offset, ITEM_SPAWN);
	public DrawAction GenerateInitialItemSpawnAction(XElement element, Vector2 offset) => GenerateGenericSpawnAction(element, offset, INITIAL_ITEM_SPAWN);
	public DrawAction GenerateItemSetAction(XElement element, Vector2 offset) => GenerateGenericSpawnAction(element, offset, ITEM_SET);
	
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
		
		#if HIDE_NORMALS
		#else
		var normal = clockwise_dir;
		if(element.HasAttribute("NormalX")) normal.x = element.GetFloatAttribute("NormalX");
		if(element.HasAttribute("NormalY")) normal.y = element.GetFloatAttribute("NormalY");
		var normal_start = (@from+to)/2f;
		var normal_end = normal_start + NORMAL_LENGTH * normal;
		#endif
		
		DrawAction lineaction = (ci) =>
		{
			ci.DrawLine(@from, to, color);
			
			#if HIDE_NORMALS
			#else
			ci.DrawLine(normal_start, normal_end, NORMAL_LINE);
			#endif
		};
		
		if(element.HasAttribute("Team"))
		{
			var teamoffset = TEAM_LINES_OFFSET * clockwise_dir;
			var team = element.GetIntAttribute("Team");
			lineaction = lineaction.Chain<CanvasItem>(
				(ci) =>
				{
					ci.DrawLine(@from+teamoffset, to+teamoffset, TEAM_COLLISION[team]);
					ci.DrawLine(@from-teamoffset, to-teamoffset, TEAM_COLLISION[team]);
				}
			);
		}
		
		if(element.HasAttribute("TauntEvent"))
		{
			var tauntevent = element.GetAttribute("TauntEvent");
			var labelPos = new Vector2(Math.Min(@from.x, to.x), Math.Min(@from.y, to.y));
			lineaction = lineaction.Chain<CanvasItem>(
				(ci) => ci.DrawString(FONT, labelPos + COLLISION_TAUNT_EVENT_OFFSET*Vector2.Up, $"TauntEvent: {tauntevent}")
			);
		}
		
		if(element.HasAttribute("AnchorX") && element.HasAttribute("AnchorY"))
		{
			var anchorX = element.GetFloatAttribute("AnchorX");
			var anchorY = element.GetFloatAttribute("AnchorY");
			var anchor = new Vector2(anchorX, anchorY);
			var more_transparent = new Color(color.r, color.g, color.b, 0.3f);
			lineaction = lineaction.Chain<CanvasItem>(
				(ci) => ci.DrawCircle(anchor, ANCHOR_RADIUS, more_transparent)
			);
		}
		
		return lineaction;
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
		
		var fireoffsetX = float.Parse(element.GetAttribute("FireOffsetX",""));
		var fireoffsetY = float.Parse(element.GetAttribute("FireOffsetY",""));
		var fireoffset = new Vector2(fireoffsetX, fireoffsetY);
		
		var middle = (@from+to)/2f;
		var firePos = fireoffset + offset;
		
		var baseaction =  GenerateGenericCollisionAction(element, offset, color).Chain<CanvasItem>(
			(ci) =>
			{
				#if HIDE_TRAP_POWERS
				#else
				ci.DrawString(FONT, firePos + PRESSURE_PLATE_POWERS_OFFSET*Vector2.Up, $"Powers: {powers}");
				#endif
				
				ci.DrawString(FONT, labelPos + PRESSURE_PLATE_COOLDOWN_OFFSET*Vector2.Up, $"Cooldown: {cooldown}f");
				ci.DrawCircle(firePos, FIRE_OFFSET_RADIUS, PRESSURE_PLATE_FIRE_OFFSET);
				ci.DrawLine(middle, firePos, PRESSURE_PLATE_FIRE_OFFSET);
			}
		);
		
		var lineend = firePos + dirmult*PRESSURE_PLATE_DIR_LINE_LENGTH*Vector2.Left;
		var sideline1 = lineend + dirmult*new Vector2(PRESSURE_PLATE_DIR_LINE_SIDE_OFFSET_X, PRESSURE_PLATE_DIR_LINE_SIDE_OFFSET_Y);
		var sideline2 = lineend + dirmult*new Vector2(PRESSURE_PLATE_DIR_LINE_SIDE_OFFSET_X, -PRESSURE_PLATE_DIR_LINE_SIDE_OFFSET_Y);
		
		baseaction = baseaction.Chain<CanvasItem>(
			(ci) =>
			{
				ci.DrawLine(firePos, lineend, PRESSURE_PLATE_LINE);
				ci.DrawLine(lineend, sideline1, PRESSURE_PLATE_LINE);
				ci.DrawLine(lineend, sideline2, PRESSURE_PLATE_LINE);
			}
		);
		
		return baseaction;
	}
	
	public DrawAction GeneratePressurePlateCollisionAction(XElement element, Vector2 offset) => GenerateGenericPressurePlateCollisionAction(element, offset, PRESSURE_PLATE_COLLISION);
	public DrawAction GenerateSoftPressurePlateCollisionAction(XElement element, Vector2 offset) => GenerateGenericPressurePlateCollisionAction(element, offset, SOFT_PRESSURE_PLATE_COLLISION);
	
	//////////////////////////////////////////
	///////////////////Misc///////////////////
	//////////////////////////////////////////
	public DrawAction GenerateGoalAction(XElement element, Vector2 offset)
	{
		var goal = element.GetIntAttribute("Team", 1);
		return GenerateGenericSpawnAction(element, offset, GOAL_COLORS[goal]);
	}
	
	//////////////////////////////////////////
	//////////////Navigation//////////////////
	//////////////////////////////////////////
	
	public DrawAction GenerateNavNodeAction(XElement element, Vector2 offset)
	{
		var x = element.GetFloatAttribute("X", 0f);
		var y = element.GetFloatAttribute("Y", 0f);
		var pos = new Vector2(x,y) + offset;
		var navid = element.GetAttribute("NavID");
		var ID = NormalizeNavID(navid);
		navnodesPositions.Add(ID, pos);
		return (ci) =>
		{
			ci.DrawCircle(pos, NAVNODE_RADIUS, NAVNODE_COLORS[GetNavType(navid)]);
			ci.DrawString(FONT, pos + NAVNODE_ID_OFFSET*Vector2.Up, $"NavID: {navid}");
		};
	}
	
	public DrawAction GenerateNavLineAction(XElement element)
	{
		var ID = NormalizeNavID(element.GetAttribute("NavID"));
		var pos = navnodesPositions[ID];
		var action = EMPTY_ACTION;
		
		element
			.GetAttribute("Path")//get path
			.Split(",")//split to parts
			.ForEach(//add line actions
				(s) => action = action.Chain<CanvasItem>(
					(ci) => ci.DrawLine(pos, (pos+navnodesPositions[NormalizeNavID(s)])/2f, NAVNODE_COLORS[GetNavType(s)])
				)
			);
		
		return action;
	}
	
	public int NormalizeNavID(string s)
	{
		if(s[0] < '0' || '9' < s[0]) s = s.Substring(1);
		return int.Parse(s);
	}
	
	private readonly static HashSet<char> TYPES = new HashSet<char>{'A', 'D', 'G', 'L', 'W'};
	public string GetNavType(string s)
	{
		char first = s[0];
		if(TYPES.Contains(first)) return $"{first}";
		else return "";
	}
	
	//////////////////////////////////////////
	////////////////Dynamic///////////////////
	//////////////////////////////////////////
	public void SetupMovingPlatform(XElement element)
	{
		var platid = element.GetIntAttribute("PlatID");
		var animationElement = element.Elements("Animation").First();
		
		var numframes_defaultto = (defaultNumFrames == -1)?"":$"{defaultNumFrames}";
		var numframes = int.Parse(animationElement.GetAttribute("NumFrames", numframes_defaultto));
		var startframe = animationElement.GetIntAttribute("StartFrame", 1) - 1;
		var mult = animationElement.GetFloatAttribute("SlowMult", defaultSlowMult);
		
		var data = animationElement.GetElementKeyframes(mult);
		
		var stepper = new KeyframeStepper(data, numframes*mult);
		stepper.AdvanceTime((float)startframe);
		dynamicMovementDict.Add(platid, stepper);
	}
	
	public DrawAction GenerateGenericDynamicAction(XElement element, Vector2 offset, Color color)
	{
		var platid = element.GetIntAttribute("PlatID");
		var stepper = dynamicMovementDict[platid];
		var pos = stepper.GetCurrent() + element.GetElementPosition() + offset;
		return (ci) =>
		{
			#if HIDE_DYNAMICS
			#else
			ci.DrawString(FONT, pos + DYNAMIC_TIME_OFFSET*Vector2.Up, $"Time: {stepper.time}");
			ci.DrawString(FONT, pos + DYNAMIC_PLATID_OFFSET*Vector2.Up, $"PlatID: {platid}");
			ci.DrawCircle(pos, DYNAMIC_RADIUS, color);
			#endif
			
			foreach(var colelem in element.Elements())
				GetGenerator(colelem.Name.LocalName)(colelem, pos)(ci);
		};
	}
	
	public DrawAction GenerateDynamicCollisionAction(XElement element, Vector2 offset) => GenerateGenericDynamicAction(element, offset, DYNAMIC_COLLISION);
	public DrawAction GenerateDynamicRespawnAction(XElement element, Vector2 offset) => GenerateGenericDynamicAction(element, offset, DYNAMIC_RESPAWN);
	public DrawAction GenerateDynamicItemSpawnAction(XElement element, Vector2 offset) => GenerateGenericDynamicAction(element, offset, DYNAMIC_ITEM_SPAWN);
	public DrawAction GenerateDynamicNavNodeAction(XElement element, Vector2 offset) => GenerateGenericDynamicAction(element, offset, DYNAMIC_NAVNODE);
	
}
