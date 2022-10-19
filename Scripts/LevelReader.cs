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
	public static readonly DrawAction EMPTY_ACTION = (ci) => {};
	public static readonly Generator EMPTY_GENERATOR = (ci, offset) => EMPTY_ACTION;
	public static readonly AssetGenerator EMPTY_ASSET_GENERATOR = (ci, trans) => EMPTY_ACTION;
	
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
	public string swfPath;
	public string mapName;
	public string levelTypesPath;
	public string mapArtPath;
	
	public int redCount = 0;
	public int blueCount = 0;
	
	public ConfigReader cf;
	
	public Font font;
	
	public LevelReader() {}
	
	public LevelReader(ConfigReader configReader)
	{
		this.cf = configReader;
		SetupReader();
	}
	
	public void SetupReader()
	{
		mapFolder = cf.Paths["LevelsFolder"];
		mapName = cf.Paths["LevelName"];;
		levelTypesPath = cf.Paths["LevelTypes"];
		mapArtPath = cf.Paths["MapArt"];
		swfPath = cf.Paths["SWFReplacement"];
		font = ResourceLoader.Load<Font>(cf.Paths["Font"]);
		
		parsedMapFile = XDocument.Parse(Utils.Read($"{mapFolder}/{mapName}.xml"));
		parsedLevelTypes = (levelTypesPath == "")?null:XDocument.Parse(Utils.Read(levelTypesPath));
		
		InitGenerators();
		Reset();
		LoadAssets();
	}
	
	public void LoadAssets()
	{
		var first = parsedMapFile?.FirstNode as XElement;
		
		//load all normal assets
		first.Elements()
			.Where(HasAssetGenerator)
			.Select(InvokeAssetGenerator)
			.Combine()(null);
		
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
					.Select(e => GenerateScoreboardAction(e))
					.Combine()(null);
			}
		}
		
		redCount = tempred;
		blueCount = tempblue;
	}
	
	public void Reset()
	{
		callCount = 0;
		redCount = Convert.ToInt32(cf.Others["RedScore"]);
		blueCount = Convert.ToInt32(cf.Others["BlueScore"]);
		globalizeMovingPlatformPosition = Convert.ToBoolean(cf.Others["GalvanPrimeFix"]);
		noSkulls = Convert.ToBoolean(cf.Others["NoSkulls"]);
		
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
		assetGenerators = new Dictionary<string, AssetGenerator>();
		if(cf.Display["Assets"])
		{
			assetGenerators.Add("Platform", GeneratePlatformAction);
			assetGenerators.Add("MovingPlatform", GenerateMovingPlatformAction);
			
			if(cf.Display["ScoreboardDigits"])
			assetGenerators.Add("TeamScoreboard", GenerateScoreboardAction);
		}
		
		if(cf.Display["Background"]) assetGenerators.Add("Background", GenerateBackgroundAction);
		
		generators = new Dictionary<string, Generator>();
		
		if(cf.Display["Bounds"])
		{
			if(cf.Display["CameraBounds"]) generators.Add("CameraBounds", GenerateCameraBoundsAction);
			if(cf.Display["SpawnbotBounds"]) generators.Add("SpawnBotBounds", GenerateSpawnBotBoundsAction);
		}
		
		if(cf.Display["Items"])
		{
			generators.Add("ItemSpawn", GenerateItemSpawnAction);
			generators.Add("ItemInitSpawn", GenerateInitialItemSpawnAction);
			generators.Add("ItemSet", GenerateItemSetAction);
			generators.Add("DynamicItemSpawn", GenerateDynamicItemSpawnAction);
		}
		
		if(cf.Display["Respawns"])
		{
			generators.Add("Respawn", GenerateRespawnAction);
			generators.Add("DynamicRespawn", GenerateDynamicRespawnAction);
		}
		
		if(cf.Display["Collision"])
		{
			generators.Add("HardCollision", GenerateHardCollisionAction);
			generators.Add("SoftCollision", GenerateSoftCollisionAction);
			generators.Add("NoSlideCollision", GenerateNoSlideCollisionAction);
			
			generators.Add("GameModeHardCollision", GenerateGamemodeHardCollisionAction);
			generators.Add("GameModeSoftCollision", GenerateGamemodeSoftCollisionAction);
			generators.Add("GameModeNoSlideCollision", GenerateGamemodeNoSlideCollisionAction);
			
			generators.Add("BouncyHardCollision", GenerateBouncyHardCollisionAction);
			generators.Add("BouncySoftCollision", GenerateBouncySoftCollisionAction);
			generators.Add("BouncyNoSlideCollision", GenerateBouncyNoSlideCollisionAction);
			
			generators.Add("PressurePlateCollision", GeneratePressurePlateCollisionAction);
			generators.Add("SoftPressurePlateCollision", GenerateSoftPressurePlateCollisionAction);
			
			generators.Add("DynamicCollision", GenerateDynamicCollisionAction);
		}
		
		if(cf.Display["MovingPlatformData"])
		generators.Add("MovingPlatform", GenerateSecondaryMovingPlatformAction);
		
		if(cf.Display["Navmesh"])
		{
			generators.Add("NavNode", GenerateNavNodeAction);
			generators.Add("DynamicNavNode", GenerateDynamicNavNodeAction);
		}
		
		if(cf.Display["Goals"]) generators.Add("Goal", GenerateGoalAction);
	}
	
	
	public bool HasGenerator(string s) => generators.ContainsKey(s);
	public bool HasGenerator(XElement e) => HasGenerator(e.Name.LocalName);
	public Generator GetGenerator(string s) => generators[s];
	public Generator GetGenerator(XElement e) => GetGenerator(e.Name.LocalName);
	public DrawAction InvokeGenerator(XElement e) => GetGenerator(e)(e, Vector2.Zero);
	public bool HasAssetGenerator(string s) => assetGenerators.ContainsKey(s);
	public bool HasAssetGenerator(XElement e) => HasAssetGenerator(e.Name.LocalName);
	public AssetGenerator GetAssetGenerator(string s) => assetGenerators[s];
	public AssetGenerator GetAssetGenerator(XElement e) => GetAssetGenerator(e.Name.LocalName);
	public DrawAction InvokeAssetGenerator(XElement e) => GetAssetGenerator(e)(e, Transform2D.Identity);
	
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
			first.Elements()
				.Where(HasAssetGenerator)
				.Select(InvokeAssetGenerator)
				.Combine()(ci);
			
			first.Elements()
				.Where(HasGenerator)
				.Select(InvokeGenerator)
				.Combine()(ci);
			
			if(cf.Display["Navmesh"])
				GenerateNavMeshActionList(first).Combine()(ci);
			
			GenerateBlastzoneBoundsAction(first)(ci);
			
			callCount++;
		};
	}
	
	public void ResetTime()
	{
		movingPlatformsDict = new Dictionary<int, KeyframeStepper>();
		(parsedMapFile.FirstNode as XElement).Elements("MovingPlatform").ForEach(SetupMovingPlatform);
	}
	
	public void AdvanceTime(float time) => movingPlatformsDict.Values.ForEach(s => s.AdvanceTime(time));
	
	public IEnumerable<DrawAction> GenerateNavMeshActionList(XElement element) =>
		element.Elements("DynamicNavNode")
			.Prepend(element)
			.SelectMany(e => e.Elements("NavNode"))
			.Select(GenerateNavLineAction);
	
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
			
			if(cf.Display["Bounds"] && cf.Display["BlastzoneBounds"])
				ci?.DrawRect(rect, cf.Colors["BlastzoneBounds"], false);
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
		return (ci) => ci?.DrawRect(rect, color, false);
	}
	
	public DrawAction GenerateCameraBoundsAction(XElement element, Vector2 offset = default) => GenerateGenericBoundsAction(element, offset, cf.Colors["CameraBounds"]);
	public DrawAction GenerateSpawnBotBoundsAction(XElement element, Vector2 offset = default) => GenerateGenericBoundsAction(element, offset, cf.Colors["SpawnbotBounds"]);
	
	//////////////////////////////////////////
	///////////////Item Spawns////////////////
	//////////////////////////////////////////
	public DrawAction GenerateGenericAreaAction(XElement element, Vector2 offset, Color color)
	{
		var rect = element.GetElementRect();
		rect.Position += offset;
		if(rect.Size.x == 0 && rect.Size.y == 0)
		{
			var rad = cf.Sizes["DefaultAreaRadius"];
			var newrect = new Rect2(rect.Position - rad*Vector2.One, 2f*rad*Vector2.One);
			return (ci) => ci?.DrawRect(newrect, color, false);
		}
		else if(rect.Size.x == 0 || rect.Size.y == 0)
			return (ci) => ci?.DrawLine(rect.Position, rect.End, color);
		else
			return (ci) => ci?.DrawRect(rect, color, true);
	}
	
	public DrawAction GenerateItemSpawnAction(XElement element, Vector2 offset = default) => GenerateGenericAreaAction(element, offset, cf.Colors["ItemSpawn"]);
	public DrawAction GenerateInitialItemSpawnAction(XElement element, Vector2 offset = default) => GenerateGenericAreaAction(element, offset, cf.Colors["InitialItemSpawn"]);
	public DrawAction GenerateItemSetAction(XElement element, Vector2 offset = default) => GenerateGenericAreaAction(element, offset, cf.Colors["ItemSet"]);
	
	//////////////////////////////////////////
	/////////////////Respawns/////////////////
	//////////////////////////////////////////
	public DrawAction GenerateRespawnAction(XElement element, Vector2 offset = default)
	{
		var initial = element.GetBooleanAttribute("Initial");
		var expandedInit = element.GetBooleanAttribute("ExpandedInit");
		
		Color chosenColor;
		if(initial) chosenColor = cf.Colors["InitialRespawn"];
		else if(expandedInit) chosenColor = cf.Colors["ExtendedInitialRespawn"];
		else chosenColor = cf.Colors["Respawn"];
		
		var pos = element.GetElementPosition();
		pos += offset;
		return (ci) => ci?.DrawCircle(pos, cf.Sizes["RespawnRadius"], chosenColor);
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
		
		var normal = clockwise_dir;
		if(element.HasAttribute("NormalX")) normal.x = element.GetFloatAttribute("NormalX");
		if(element.HasAttribute("NormalY")) normal.y = element.GetFloatAttribute("NormalY");
		var normal_start = (@from+to)/2f;
		var normal_end = normal_start + cf.Sizes["NormalLength"] * normal;
		
		DrawAction action = (ci) => ci?.DrawLine(@from, to, color);
		
		if(cf.Display["CollisionNormals"])
			action = action.Chain((ci) => ci?.DrawLine(normal_start, normal_end, cf.Colors["NormalLine"]));
		
		if(cf.Display["TeamCollision"] && element.HasAttribute("Team"))
		{
			var teamoffset = cf.Sizes["TeamLinesOffset"] * clockwise_dir;
			var team = element.GetIntAttribute("Team");
			var teamcolor = cf.Colors[$"TeamColor{team}"];
			action = action.Chain(
				(ci) =>
				{
					ci?.DrawLine(@from+teamoffset, to+teamoffset, teamcolor);
					ci?.DrawLine(@from-teamoffset, to-teamoffset, teamcolor);
				}
			);
		}
		
		if(cf.Display["TauntEvent"] && element.HasAttribute("TauntEvent"))
		{
			var tauntevent = element.GetAttribute("TauntEvent");
			var labelPos = new Vector2(Math.Min(@from.x, to.x), Math.Min(@from.y, to.y));
			action = action.Chain(
				(ci) => ci?.DrawString(font, labelPos + cf.Sizes["CollisionTauntEventOffset"]*Vector2.Up, $"TauntEvent: {tauntevent}")
			);
		}
		
		if(cf.Display["Anchors"] && element.HasAttribute("AnchorX") && element.HasAttribute("AnchorY"))
		{
			var anchor = element.GetElementPosition("Anchor");
			var more_transparent = new Color(color.r, color.g, color.b, 0.3f);
			action = action.Chain(
				(ci) => ci?.DrawCircle(anchor, cf.Sizes["AnchorRadius"], more_transparent)
			);
		}
		
		return action;
	}
	
	public DrawAction GenerateHardCollisionAction(XElement element, Vector2 offset = default) => GenerateGenericCollisionAction(element, offset, cf.Colors["HardCollision"]);
	public DrawAction GenerateSoftCollisionAction(XElement element, Vector2 offset = default) => GenerateGenericCollisionAction(element, offset, cf.Colors["SoftCollision"]);
	public DrawAction GenerateNoSlideCollisionAction(XElement element, Vector2 offset = default) => GenerateGenericCollisionAction(element, offset, cf.Colors["NoSlideCollision"]);
	
	public DrawAction GenerateGenericGamemodeCollisionAction(XElement element, Vector2 offset, Color color) => GenerateGenericCollisionAction(element, offset, color);
	public DrawAction GenerateGamemodeHardCollisionAction(XElement element, Vector2 offset = default) => GenerateGenericGamemodeCollisionAction(element, offset, cf.Colors["GamemodeHardCollision"]);
	public DrawAction GenerateGamemodeSoftCollisionAction(XElement element, Vector2 offset = default) => GenerateGenericGamemodeCollisionAction(element, offset, cf.Colors["GamemodeSoftCollision"]);
	public DrawAction GenerateGamemodeNoSlideCollisionAction(XElement element, Vector2 offset = default) => GenerateGenericGamemodeCollisionAction(element, offset, cf.Colors["GamemodeNoSlideCollision"]);
	
	public DrawAction GenerateGenericBouncyCollisionAction(XElement element, Vector2 offset, Color color) => GenerateGenericCollisionAction(element, offset, color);
	public DrawAction GenerateBouncyHardCollisionAction(XElement element, Vector2 offset = default) => GenerateGenericBouncyCollisionAction(element, offset, cf.Colors["BouncyHardCollision"]);
	public DrawAction GenerateBouncySoftCollisionAction(XElement element, Vector2 offset = default) => GenerateGenericBouncyCollisionAction(element, offset, cf.Colors["BouncySoftCollision"]);
	public DrawAction GenerateBouncyNoSlideCollisionAction(XElement element, Vector2 offset = default) => GenerateGenericBouncyCollisionAction(element, offset, cf.Colors["BouncyNoSlideCollision"]);
	
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
				if(cf.Display["TrapPowers"])
				ci?.DrawString(font, firePos + cf.Sizes["PressurePlatePowerOffset"]*Vector2.Up, $"Powers: {powers}");
				
				if(cf.Display["TrapCooldown"])
				ci?.DrawString(font, labelPos + cf.Sizes["PressurePlateCooldownOffset"]*Vector2.Up, $"Cooldown: {cooldown}f");
				
				if(cf.Display["TrapPowerOffset"])
				{
					ci?.DrawCircle(firePos, cf.Sizes["FireOffsetRadius"], cf.Colors["PressurePlateFireOffset"]);
					ci?.DrawLine(middle, firePos, cf.Colors["PressurePlateFireOffset"]);
				}
			}
		);
		
		if(cf.Display["TrapPowerOffset"])
		{
			var lineend = firePos + dirmult*cf.Sizes["PressurePlateDirLineLength"]*Vector2.Left;
			var offX = cf.Sizes["PressurePlateDirLineOffsetX"];
			var offY = cf.Sizes["PressurePlateDirLineOffsetY"];
			var sideline1 = lineend + dirmult*new Vector2(offX, offY);
			var sideline2 = lineend + dirmult*new Vector2(offX, -offY);
			
			var plateLine = cf.Colors["PressurePlateLine"];
			action = action.Chain(
				(ci) =>
				{
					ci?.DrawLine(firePos, lineend, plateLine);
					ci?.DrawLine(lineend, sideline1, plateLine);
					ci?.DrawLine(lineend, sideline2, plateLine);
				}
			);
		}
		
		return action;
	}
	
	public DrawAction GeneratePressurePlateCollisionAction(XElement element, Vector2 offset = default) => GenerateGenericPressurePlateCollisionAction(element, offset, cf.Colors["PressurePlateCollision"]);
	public DrawAction GenerateSoftPressurePlateCollisionAction(XElement element, Vector2 offset = default) => GenerateGenericPressurePlateCollisionAction(element, offset, cf.Colors["SoftPressurePlateCollision"]);
	
	//////////////////////////////////////////
	///////////////////Misc///////////////////
	//////////////////////////////////////////
	public DrawAction GenerateGoalAction(XElement element, Vector2 offset = default)
	{
		var goal = element.GetIntAttribute("Team", 1);
		return GenerateGenericAreaAction(element, offset, cf.Colors[$"GoalColor{goal}"]);
	}
	
	//////////////////////////////////////////
	//////////////Navigation//////////////////
	//////////////////////////////////////////
	
	public DrawAction GenerateNavNodeAction(XElement element, Vector2 offset = default)
	{
		var pos = element.GetElementPositionOrDefault() + offset;
		var navid = element.GetAttribute("NavID");
		var id = NormalizeNavID(navid);
		var type = GetNavType(navid);
		navnodesPositions.Add(id, pos);
		return (ci) =>
		{
			ci?.DrawCircle(pos, cf.Sizes["NavnodeRadius"], cf.Colors[$"Navnode{type}"]);
			ci?.DrawString(font, pos + cf.Sizes["NavnodeIDOffset"]*Vector2.Up, $"NavID: {navid}");
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
							GD.PushWarning($"NavNode {navid} connects to NavNode {norms}{stypedesc}, but there's no NavNode with ID {norms}");
						}
						return EMPTY_ACTION;
					}
					
					return (ci) => ci?.DrawLine(pos, (pos+navnodesPositions[norms])/2f, cf.Colors[$"Navnode{types}"]);
				}
		).Combine();
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
		
		var hasSkulls = element.GetBooleanAttribute("HasSkulls", false);
		
		return GenerateBaseAssetAction(trans, offset, $"{mapArtPath}/{assetpath}", instanceName, bounds, hasSkulls);
	}
	
	public DrawAction GenerateBaseAssetAction(Transform2D trans, Vector2 offset, string path, string instanceName = "", Vector2 bounds = default, bool hasSkulls = false)
	{
		var texture = Utils.LoadImageFromPath(path, instanceName, bounds);
		
		if(
			texture is null ||
			(!noSkulls && instanceName == "am_NoSkulls") ||
			(noSkulls && hasSkulls) ||
			(instanceName == "am_Holiday")
		) return EMPTY_ACTION;
		
		return (ci) =>
		{
			if(cf.Display["AssetPosition"])
			ci?.DrawCircle(offset, cf.Sizes["AssetPositionRadius"], cf.Colors["Asset"]);
			
			ci?.DrawSetTransformMatrix(trans);
			ci?.DrawTexture(texture, offset);
			ci?.DrawSetTransformMatrix(Transform2D.Identity);
		};
	}
	
	public DrawAction GenerateBackgroundAction(XElement element, Transform2D trans = default) => GenerateGenericAssetAction(element.Parent.Elements("CameraBounds").First(), trans, true, "Backgrounds", "", element.GetAttribute("AssetName"));
	
	public DrawAction GeneratePlatformAction(XElement element, Transform2D trans = default)
	{
		var instanceName = element.GetAttribute("InstanceName");
		
		trans = trans.Translated(element.GetElementPositionOrDefault());
		
		trans.Scale *= element.GetFloatAttribute("Scale", 1);
		trans.Scale *= Vector2.Right * element.GetFloatAttribute("ScaleX", 1) + Vector2.Down;
		trans.Scale *= Vector2.Down * element.GetFloatAttribute("ScaleY", 1) + Vector2.Right;
		
		trans.Rotation += element.GetFloatAttribute("Rotation").ToRad();
		
		if(!element.HasAttribute("AssetName"))
		{
			var assetActions = element.Elements("Asset")
									.Select(e => GenerateGenericAssetAction(e, trans, true, assetDir, instanceName));
			
			var platformActions = element.Elements("Platform")
										.Select(e => GeneratePlatformAction(e, trans));
			
			var actions = assetActions.Concat(platformActions);
			
			if(cf.Display["PlatformLabel"])
			actions = actions.Append((ci) => ci?.DrawString(font, trans.origin, $"InstanceName: {instanceName}"));
			
			return actions.Combine();
		}
		else return GenerateGenericAssetAction(element, trans, false, assetDir, instanceName);
	}
	
	public DrawAction GenerateMovingPlatformAction(XElement element, Transform2D trans = default)
	{
		var platid = element.GetIntAttribute("PlatID");
		var stepper = movingPlatformsDict[platid];
		trans = trans.Translated(stepper.GetCurrent());
		
		if(!globalizeMovingPlatformPosition) trans = trans.Translated(element.GetElementPositionOrDefault());
		
		return element.Elements()
					.Where(HasAssetGenerator)
					.Select(e => GetAssetGenerator(e)(e,trans))
					.Combine();
	}
	
	public DrawAction GenerateSecondaryMovingPlatformAction(XElement element, Vector2 offset = default)
	{
		var platid = element.GetIntAttribute("PlatID");
		var stepper = movingPlatformsDict[platid];
		var pos = stepper.GetCurrent() + offset;
		
		if(!globalizeMovingPlatformPosition) pos += element.GetElementPositionOrDefault();
		
		if(!cf.Display["MovingPlatformsTime"]) return EMPTY_ACTION;
		
		return (ci) =>
		{
			if(cf.Display["MovingPlatformsTime"])
			ci?.DrawString(font, pos + cf.Sizes["MovingPlatformTimeOffset"]*Vector2.Up, $"Time: {stepper.time}");
			
			if(cf.Display["MovingPlatformsID"])
			ci?.DrawString(font, pos + cf.Sizes["MovingPlatformPlatIDOffset"]*Vector2.Up, $"PlatID: {platid}");
			
			if(cf.Display["MovingPlatformsPosition"])
			ci?.DrawCircle(pos, cf.Sizes["MovingPlatformRadius"], cf.Colors["MovingPlatform"]);
		};
	}
	
	public DrawAction GenerateScoreboardAction(XElement element, Transform2D trans = default)
	{
		var redX = element.GetFloatAttribute("RedTeamX");
		var blueX = element.GetFloatAttribute("BlueTeamX");
		var y = element.GetFloatAttribute("Y");
		var oneDigitX = element.GetFloatAttribute("DoubleDigitsOnesX");
		var tenDigitX = element.GetFloatAttribute("DoubleDigitsTensX");
		var digitYDiff = element.GetFloatAttribute("DoubleDigitsY")-y;
		var digitScale = element.GetFloatAttribute("DoubleDigitsScale");
		
		var redOne = (redCount%10)/1;
		var redTen = (redCount%100)/10;
		
		var blueOne = (blueCount%10)/1;
		var blueTen = (blueCount%100)/10;
		
		var redDouble = (redTen != 0);
		var blueDouble = (blueTen != 0);
		
		var redOneDigit = new Vector2(redDouble?oneDigitX:0, redDouble?digitYDiff:0);
		var redTenDigit = new Vector2(tenDigitX, digitYDiff);
		var blueOneDigit = new Vector2(blueDouble?oneDigitX:0, blueDouble?digitYDiff:0);
		var blueTenDigit = new Vector2(tenDigitX, digitYDiff);
		
		var redTrans = trans.Translated(new Vector2(redX, y));
		var blueTrans = trans.Translated(new Vector2(blueX, y));
		
		if(!redDouble) blueTrans.Scale /= digitScale;
		if(!blueDouble) redTrans.Scale /= digitScale;
		
		var redFont = element.GetAttribute("RedDigitFont");
		var blueFont = element.GetAttribute("BlueDigitFont");
		
		var redOneName = $"Digit{redOne}_{redFont}.png";
		var redTenName = $"Digit{redTen}_{redFont}.png";
		
		var blueOneName = $"Digit{blueOne}_{blueFont}.png";
		var blueTenName = $"Digit{blueTen}_{blueFont}.png";
		
		return (ci) =>
		{
			GenerateBaseAssetAction(redTrans, redOneDigit, $"{swfPath}/{redOneName}")(ci);
			
			if(redDouble)
				GenerateBaseAssetAction(redTrans, redTenDigit, $"{swfPath}/{redTenName}")(ci);
			
			GenerateBaseAssetAction(blueTrans, blueOneDigit, $"{swfPath}/{blueOneName}")(ci);
			
			if(blueDouble)
				GenerateBaseAssetAction(blueTrans, blueTenDigit, $"{swfPath}/{blueTenName}")(ci);
		};
	}
	
	//////////////////////////////////////////
	////////////////Dynamic///////////////////
	//////////////////////////////////////////
	
	public DrawAction GenerateGenericDynamicAction(XElement element, Vector2 offset = default)
	{
		var platid = element.GetIntAttribute("PlatID");
		var stepper = movingPlatformsDict[platid];
		var pos = stepper.GetCurrent() + offset;
		
		if(globalizeMovingPlatformPosition)
			return element.Elements()
						.Where(HasGenerator)
						.Select(e => GetGenerator(e)(e,pos))
						.Combine();
		
		pos += element.GetElementPositionOrDefault();
		
		DrawAction dynAct = cf.Display["MovingPlatformData"]?((ci) =>
		{
			if(cf.Display["MovingPlatformsTime"])
			ci?.DrawString(font, pos + cf.Sizes["MovingPlatformTimeOffset"]*Vector2.Up, $"Time: {stepper.time}");
			
			if(cf.Display["MovingPlatformsID"])
			ci?.DrawString(font, pos + cf.Sizes["MovingPlatformPlatIDOffset"]*Vector2.Up, $"PlatID: {platid}");
			
			if(cf.Display["MovingPlatformsPosition"])
			ci?.DrawCircle(pos, cf.Sizes["MovingPlatformRadius"], cf.Colors["MovingPlatform"]);
		}):EMPTY_ACTION;
		
		return element.Elements()
					.Where(HasGenerator)
					.Select(e => GetGenerator(e)(e,pos))
					.Append(dynAct)
					.Combine();
	}
	
	public DrawAction GenerateDynamicCollisionAction(XElement element, Vector2 offset = default) => GenerateGenericDynamicAction(element, offset);
	public DrawAction GenerateDynamicRespawnAction(XElement element, Vector2 offset = default) => GenerateGenericDynamicAction(element, offset);
	public DrawAction GenerateDynamicItemSpawnAction(XElement element, Vector2 offset = default) => GenerateGenericDynamicAction(element, offset);
	public DrawAction GenerateDynamicNavNodeAction(XElement element, Vector2 offset = default) => GenerateGenericDynamicAction(element, offset);
}
