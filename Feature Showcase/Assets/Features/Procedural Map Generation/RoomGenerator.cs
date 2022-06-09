///This generation are know as Diffusion Limited Aggregation - DLA
using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using System;

public class RoomGenerator : MonoBehaviour
{
	public int roomAmount;
	[SerializeField]
	Customize customize = new Customize();
	[Tooltip("The minimum and maximum amount of replicate an room need to do")]
	public ReplicateRequirement replicateReq = new ReplicateRequirement();
	[Tooltip("The percent chance of generate")]
	public RateSetting replicateRate = new RateSetting();
	public List<RoomData> currentLeaders = new List<RoomData>();
	public List<RoomData> rooms = new List<RoomData>();
	public List<BridgeData> bridges = new List<BridgeData>();
	public List<WallData> walls = new List<WallData>();
	public bool autoGenerate; public float autoDelay;
	GameObject floorGroup, bridgeGroup, wallGroup;
	[HideInInspector]
	public bool hasGenerate, areGenerating; 
	public event Action completeGeneration, structureBuilded;

#region Classes
	[Serializable] public class ReplicateRequirement 
	{[Range(0,4)] public int min; [Range(1,4)] public int max;}
	[Serializable] public class RateSetting 
	{
		public float general;
		[Tooltip("Each directon will has their own replicate rate")]
		public bool useDirectional; 
		public float up, down, left, right;
	}
	[Serializable] class Customize
	{
		public DebugInfo debug;
		[Serializable] public class DebugInfo {public Color leaderColor, stuckColor, endColor;}
		[Header("Floor")]
		public Color floorColor;
		public GameObject[] floorPrefabs;
		public Vector2 floorScale;
		public float floorSpacing;
		[Header("Bridge")]
		public Color bridgeColor;
		public GameObject bridgePrefab;
		public float bridgeWidth, bridgeLength;
		[Tooltip("Will the bridge connect to every room or connect by how room was replicate")]
		public bool neighbourMode; //? replicate mode
		[Header("Wall")]
		public Color wallColor;
		public GameObject wallPrefab;
		public float wallThick, wallLengthModify;
		public enum WallAlign {center, outside, inside} public WallAlign wallAlign;
		[Tooltip("Will the wall block all side of room or only block the side has nothing")]
		public bool enclosedMode; //? border mode
	}
	[Serializable] public class RoomData
	{
		public int index;
		public Vector2 coordinates, position;
		public int replicateCount;
		[Serializable] public class Replicate {public Vector2 coord, pos;}
		public Replicate[] replicated = new Replicate[] {new Replicate(),new Replicate(),new Replicate(),new Replicate()};
		public int neighboursCount;
		[Serializable] public class Neighbours {public bool filled; public Vector2 coord, pos;}
		public Neighbours[] neighbours = new Neighbours[] {new Neighbours(),new Neighbours(),new Neighbours(),new Neighbours()};
		public Structure structure = new Structure();
		[HideInInspector] public bool stuck, continuation, escape;
		[Serializable] public class Structure 
		{
			public GameObject floor, bridge;
			public SpriteRenderer floorRender, bridgeRender;
			public bool[] hasBridge = new bool[4];
		}
	}
	[Serializable] public class BridgeData
	{
		public int index, direction;
		public Vector2 position;
		public Vector2[] connectPos = new Vector2[2];
	}

	[Serializable] public class WallData
	{
		public Vector2 position;
		public GameObject obj;
		public SpriteRenderer renderer;
		public int direction, indexBelong;
		public bool belongToWall, belongToBridge;
	}
#endregion

	void Update()
	{
		//Bein generate upon press space
		if(Input.GetKeyDown(KeyCode.Space)) {Generate();}
	}

	public void Generate()
	{
		//Only generating when not generate
		if(areGenerating == true) {return;}
		//Preparing to for structure build
		PrepareStructure();
		areGenerating = true;
		hasGenerate = false;
		rooms.Clear(); rooms = new List<RoomData>();
		bridges.Clear(); bridges = new List<BridgeData>();
		walls.Clear(); walls = new List<WallData>();
		//Add the first empty room into list than replicate at it
		rooms.Add(new RoomData()); StartCoroutine(Replicate(rooms[0], rooms[0]));
	}

#region Replicating

	IEnumerator Replicate(RoomData leader, RoomData prev, int forceDirection = -1)
	{
	#region Preparing for replicate cycle
		//This room are now the current leader
		currentLeaders.Add(leader);
		//Build floor at this leader
		BuildFloor(leader, prev);
		//Wait for an frame
		yield return null;
		//Reset all the last leader floor color back to normal
		if(rooms.Count >= roomAmount) {leader.structure.floorRender.color = customize.floorColor;}
	#endregion
	#region Decide direction to replicate
		//The result to see what direction will be replicate
		bool[] result = new bool[4];
		//If this leader has been force to use an direction
		if(forceDirection != -1)
		{
			//Set the result at direction has been force to true
			result[forceDirection] = true;
		}
		//Or else this leader need to randomize it own direction
		else
		{
			//Go through all the result need to randomize
			for (int a = 0; a < result.Length; a++)
			{
				//The randomize chance of this cycle
				float chance = UnityEngine.Random.Range(0f, 100f);
				//Shuffle the direction that will receive result
				int d = UnityEngine.Random.Range(0,4);
				//If using the directional rate
				if(replicateRate.useDirectional)
				{
					//@ Set the result base on rate of each direction compare to chance
					if(d == 0 && replicateRate.up    >= chance) {result[d] = true;}
					if(d == 1 && replicateRate.down  >= chance) {result[d] = true;}
					if(d == 2 && replicateRate.left  >= chance) {result[d] = true;}
					if(d == 3 && replicateRate.right >= chance) {result[d] = true;}
				}
				//Else set the result base on general the rate compare to chance
				else {if(replicateRate.general >= chance) {result[d] = true;}}
			}
		}
		//Go through all 4 direction to check all of them
		for (int d = 0; d < 4; d++) {CheckDirection(leader, IndexToDirection(d), d, result[d]);}
		//This room are no longer the current leader
		currentLeaders.Remove(leader);
	#endregion
	#region If the leader are stuck
		//Stop going if this leader are an continuation room
		if(leader.continuation) {yield break;}
		//Attempt to escape again at this leader if the leader is still stuck
		if(leader.stuck) {StartCoroutine(Replicate(leader, leader)); yield break;}
	#endregion
	#region Finding neighbour
		//Go through all 4 direction to check neighbours 
		for (int d = 0; d < 4; d++)
		{
			//Finding room in this index direction of leader
			RoomData finded = FindRoomAtCoordinates(leader.coordinates + IndexToDirection(d));
			//If has find an room that haven't has neighbours in this direction
			if(finded != null) if(!leader.neighbours[d].filled)
			{
				//Both leader and finded room neighbours increased
				leader.neighboursCount++; finded.neighboursCount++;
				//Get the neighbours at current direction of leader room
				RoomData.Neighbours ld = leader.neighbours[d]; 
				//Get the neighbours at opposite of current direction of finded room
				RoomData.Neighbours fo = finded.neighbours[OppositeIndexDirection(d)];
				//Leader neighbours are filled then set it coordinates and position at FINDED
				ld.filled = true; ld.coord = finded.coordinates; ld.pos = finded.position;
				//Finded neighbours are filled then set it coordinates and position at LEADER
				fo.filled = true; fo.coord = leader.coordinates; fo.pos = leader.position;
			}
		}
		//If this leader neighbours are filled from all 4 side
		if(leader.neighboursCount >= 4)
		{
			//This leader is now STUCK if it is the only leader
			if(currentLeaders.Count <= 1) {leader.stuck = true;}
			// Stop this leader if there still more leader left
			else if(currentLeaders.Count > 1) 
			{
				//Set the leader floor color back to normal
				leader.structure.floorRender.color = customize.floorColor;
				yield break;
			}
		}
	#endregion
	#region This leader cycle complete
		/// If haven't got enough room 
		if(rooms.Count < roomAmount)
		{
			bool retry = false;
			//If there is only 1 leader left or this leader haven't replicate the minimum amount needed
			if(currentLeaders.Count < 1 || leader.replicateCount < replicateReq.min)
			{
				//Replicate again at this leader
				StartCoroutine(Replicate(leader, leader));
				retry = true;
			}
			//If there more leader left while this leader haven't replicate anything
			if(!retry && leader.replicateCount == 0)
			{
				//Change leader floor color to end color
				leader.structure.floorRender.color = customize.debug.endColor;
			}
		}
		/// If there are no more leader then generating are finish
		if(currentLeaders.Count == 0) {GeneratingFinish();}
	#endregion
	}

	void CheckDirection(RoomData leader, Vector2 dir, int index, bool needReplicate)
	{
		//Get the next room at given direction increase with leader coordiantes
		RoomData nextRoom = FindRoomAtCoordinates(leader.coordinates + dir);
		if(!needReplicate) {return;}
		if(leader.stuck)
		{
			leader.structure.floorRender.color = customize.debug.stuckColor;
			if(nextRoom != null)
			{
				//The leader has continue escape attempt at the next room
				leader.continuation = true;
				//The leader transfer it stuck to the next room
				leader.stuck = false; nextRoom.stuck = true; 
				//Try to escape at the next room in the same direction
				StartCoroutine(Replicate(nextRoom, leader, index)); return;
			}
			else
			{
				//The leader are no longer stuck
				leader.stuck = false;
				//The leader has escape using this direction
				leader.escape = true;
			}
		}
		if(!leader.stuck)
		{
			//Bypass the max replicate restraint if this leader is an ESCAPED room
			if(!leader.escape && leader.replicateCount >= replicateReq.max) {return;}
			//Stop if has reach the needed amount of room or next room are not empty
			if(rooms.Count >= roomAmount || nextRoom != null) {return;}
		}
		RoomData newRoom = new RoomData();
		//Set the new room coordiates as the leader coordinates increase with direction
		newRoom.coordinates = leader.coordinates + dir;
		//Set the new room's position
		newRoom.position = new Vector2
		(
			//Multiply the new room X coordinates with floor scale + spacing
			newRoom.coordinates.x * (customize.floorScale.x + customize.floorSpacing),
			//Multiply the new room Y coordinates with floor scale + spacing
			newRoom.coordinates.y * (customize.floorScale.y + customize.floorSpacing)
		);
		//Add the new room into list
		rooms.Add(newRoom);
		//Set the new room's index
		newRoom.index = rooms.Count-1;
		//This leader has replicate an new room if it not an escape replicate
		if(!leader.escape) {leader.replicateCount++;}
		leader.replicated[index].coord = newRoom.coordinates;
		leader.replicated[index].pos = newRoom.position;
		//Begin replicate more at the new room with previous room being leader
		StartCoroutine(Replicate(newRoom, leader));
	}

	void GeneratingFinish()
	{
		//Call the complete generation event
		completeGeneration?.Invoke();
		//Has completed generation
		areGenerating = false; hasGenerate = true;
		//Setting up bridge and wall
		SetupBridge(); SetupWall();
		//Call the structure builded event
		structureBuilded?.Invoke();
		//Reset all the floor color to default
		for (int r = 0; r < rooms.Count; r++) {rooms[r].structure.floorRender.color = customize.floorColor;}
		//Auto generate if wanted when complete the current generate
		if(autoGenerate) {Invoke("Generate", autoDelay);}
	}

#endregion

#region Build
	void PrepareStructure()
	{
		if(floorGroup != null) {Destroy(floorGroup);}
		if(bridgeGroup != null) {Destroy(bridgeGroup);}
		if(wallGroup != null) {Destroy(wallGroup);}
		floorGroup = new GameObject(); floorGroup.name = "Floor Group";
		bridgeGroup = new GameObject(); bridgeGroup.name = "Bridge Group";
		wallGroup = new GameObject(); wallGroup.name = "Wall Group";
	}

	void SetupBridge()
	{
		//Count to assign index for bridge
		int indexCounter = 0;
		//Go through all the room to go through 4 direction of each room
		for (int r = 0; r < rooms.Count; r++) for (int d = 0; d < 4; d++)
		{
			Vector2 roomP = rooms[r].position; Vector2 nextP = Vector2.zero; 
			///Set the next position as NEIGHBOURS position if using neighbours mode
			if(customize.neighbourMode) {nextP = rooms[r].neighbours[d].pos;} 
			///Set the next position as REPLICATED position if using not using neighbours mode
			else {nextP = rooms[r].replicated[d].pos;}
			//Find the room at next position as next room
			RoomData nextRoom = FindRoomAtPosition(nextP);
			///Skip this direction if next positon are zero or opposite of next room already has bridge
			if(nextP == Vector2.zero || nextRoom.structure.hasBridge[OppositeIndexDirection(d)]) {continue;}
			//Set position for the bridge at middle point between the current and next room
			Vector2 pos = new Vector2(roomP.x + (nextP.x - roomP.x)/2, roomP.y + (nextP.y - roomP.y)/2);
			BridgeData newBridge = new BridgeData(); newBridge.position = pos;  newBridge.direction = d;
			//Set the bridge 1st connection as current room position and 2nd as room at next position
			newBridge.connectPos[0] = roomP; newBridge.connectPos[1] = nextP;
			//The current room now has bridge at current direction
			rooms[r].structure.hasBridge[d] = true;
			//The next room now has bridge at opposite of current direction
			nextRoom.structure.hasBridge[OppositeIndexDirection(d)] = true;
			newBridge.index = indexCounter; indexCounter++; bridges.Add(newBridge);
			//Build the bridge of this room at position, rotation and index
			BuildBridge(rooms[r], newBridge.position, IndexToRotation(d), newBridge.index);
		}
	}

	void SetupWall()
	{
		//Go through all the room to go through 4 direction of each room
		for (int r = 0; r < rooms.Count; r++) for (int d = 0; d < 4; d++)
		{
			//Save the current room position
			Vector2 roomPos = rooms[r].position;
			//Get the out position as pushed outward in this direction from room position
			Vector2 outPos = WallOutwardPosition(roomPos, d);
			//The default adjacent are true for up/down and false for left/right if direction are 2/3
			bool adjacent = true; if(d == 2 || d == 3) {adjacent = false;}
			//Get the rotation of current index direction then decrease it by 90
			float rot = IndexToRotation(d) - 90;
			//Get value to align as half of wall thick if direction up/right and negative it if left/down
			float align = customize.wallThick/2; if(d==1||d==2) {align = -align;}
			//The length of wall
			float length = 0;
			//@ Edit length and align base on wall align mode
			switch(customize.wallAlign.ToString())
			{
				case "center": length = 1; align = 0; break;
				case "outside": length = 2; break;
				case "inside": length = 0; align = -align; break;
			}
			//Multiply length with wall thick to know how many thick need to add onto length
			length *= customize.wallThick;
			//If adjacent are UP/DOWN - increase outpos Y with align and length with X floor scale
			if(adjacent) {outPos.y += align; length += customize.floorScale.x;}
			//If adjacent are UP/DOWN - increase outpos X with align and length with Y floor scale
			else {outPos.x += align; length += customize.floorScale.y;}

			/// If using enclosed mode
			if(customize.enclosedMode)
			{
				//Build an wall normally for this room if this direction don't has bridge
				if(!rooms[r].structure.hasBridge[d]) {BuildWall(rooms[r], null, outPos, rot, r, d, length);}
				//Create an gate for this room in this direction if it has bridge
				else {BuildWallGate(r, d, outPos, roomPos, rot, adjacent, length);}
			}
			/// If not using enclosed mode while this direction has NO neighbours
			if(!customize.enclosedMode && !rooms[r].neighbours[d].filled)
			{
				//? This are for cutting part of wall that poke into floor
				float cutoff = customize.wallThick;
				//Get the wall alignment choosed
				switch(customize.wallAlign.ToString())
				{
					//Cutoff 0 if choose center
					case "center": cutoff = 0; break;
					//Cutof to negative if choose inside
					case "inside": cutoff = -cutoff; break;
				}
				//If adjacent are UP/DOWN
				if(adjacent)
				{
					//Cut off length and move to RIGHT if room's LEFT neighbours filled
					if(rooms[r].neighbours[2].filled) {length -= cutoff; outPos.x += cutoff/2;}
					//Cut off length and move to LEFT if room's RIGHT neighbours filled
					if(rooms[r].neighbours[3].filled) {length -= cutoff; outPos.x -= cutoff/2;}
				}
				//If adjacent are LEFT/RIGHT
				else
				{
					//Cut off length and move DOWN if room's UP neighbours filled
					if(rooms[r].neighbours[0].filled) {length -= cutoff; outPos.y -= cutoff/2;}
					//Cut off length and move UP if DOWN room's neighbours filled
					if(rooms[r].neighbours[1].filled) {length -= cutoff; outPos.y += cutoff/2;}
				}
				//Build an wall for current room at out position and rotation
				BuildWall(rooms[r], null, outPos, rot, r, d, length);
			}
		}
		BuildBridgeRailing();
	}

	void BuildWallGate(int r, int d, Vector2 outPos, Vector2 roomPos, float rot, bool adjacent, float length)
	{
		float center = center = roomPos.x; if(!adjacent) {center = roomPos.y;}
		//Get the bridge value by increase half of bridge width from center
		float bridge = center + (customize.bridgeWidth/2);
		//Get the edge value by increase half of length from center
		float edge = center + length/2;
		//The positive side are the middle point between edge and bridge
		float sidePOS = (bridge + (edge - bridge)/2);
		//The negative side get by decrease muitple value from the positive side
		float sideNEG = sidePOS - ((sidePOS - bridge)*2) - customize.bridgeWidth;
		//The current length are now the value between edge and bridge
		length = edge - bridge;
		//If adjacent are UP/DOWN
		if(adjacent)
		{
			//Build wall of room at side positive as X and outward as Y with custom length
			BuildWall(rooms[r], null, new Vector2(sidePOS, outPos.y), rot, r,d, length);
			//Build wall of room at side negative as X and outward as Y with custom length
			BuildWall(rooms[r], null, new Vector2(sideNEG, outPos.y), rot, r,d, length);
		}
		//If adjacent are LEFT/RIGHT
		else 
		{
			//Build wall of room at outward as X and side positive as Y with custom length
			BuildWall(rooms[r], null, new Vector2(outPos.x, sidePOS), rot, r,d, length);
			//Build wall of room at outward as X and side negative as Y with custom length
			BuildWall(rooms[r], null, new Vector2(outPos.x, sideNEG), rot, r,d, length);
		}
	}

	void BuildBridgeRailing()
	{
		//Go through all the bridge
		for (int b = 0; b < bridges.Count; b++)
		{
			//Get the bridge direction
			int dir = bridges[b].direction;
			//The default adjacent are true for up/down and false for left/right if direction are 2/3
			bool adjacent = true; if(dir == 2 || dir == 3) {adjacent = false;}
			//The positive and negative direction for wall
			int dirPos = 0; int dirNeg = 0;
			//Get value to align as half of wall thick
			float align = customize.wallThick/2; float length = 0;
			//@ Edit length and align base on wall align mode
			switch(customize.wallAlign.ToString())
			{
				case "center": length = 1; align = 0; break;
				case "inside": if(customize.enclosedMode) {length = 2;} align = -align; break;
			}
			//Multiply current length with wall thick then increase with spacing for the correct length
			length = (length * customize.wallThick) + customize.floorSpacing;
			//Set the side positive and negative position as bridge position
			Vector2 sidePos = bridges[b].position; Vector2 sideNeg = bridges[b].position;
			//If adjacent are UP/DOWN
			if(adjacent)
			{
				dirPos = 0; dirNeg = 1;
				sidePos.x += (customize.bridgeWidth/2 + align);
				sideNeg.x -= (customize.bridgeWidth/2 + align);
			}
			//If adjacent are LEFT/RIGHT
			else
			{
				dirNeg = 2; dirPos = 3;
				sidePos.y += (customize.bridgeWidth/2 + align);
				sideNeg.y -= (customize.bridgeWidth/2 + align);
			}
			BuildWall(null, bridges[b], sidePos, IndexToRotation(dirPos), b , dirPos, length);
			BuildWall(null, bridges[b], sideNeg, IndexToRotation(dirNeg), b , dirNeg, length);
		}
	}

	void BuildFloor(RoomData room, RoomData prev)
	{
		//Don't build floor if it has already been build
		if(room.structure.floor != null) {return;}
		//Randomly choose which type of floor prefabs gonna spawn
		GameObject prefab = customize.floorPrefabs[UnityEngine.Random.Range(0,customize.floorPrefabs.Length)];
		//Build the floor prefab has get at the room given position with no rotation
		GameObject floor = Instantiate(prefab, room.position, Quaternion.identity);
		//@ Setup the newly floor bridge
		floor.transform.SetParent(floorGroup.transform);
		floor.transform.localScale = customize.floorScale;
		floor.name = (rooms.Count-1) + " - Floor";
		room.structure.floor = floor;
		room.structure.floorRender = floor.GetComponent<SpriteRenderer>();
		//Set this room current color to leader color
		room.structure.floorRender.color = customize.debug.leaderColor;
		//Set the previous room color to default color
		prev.structure.floorRender.color = customize.floorColor;
	}

	void BuildBridge(RoomData room , Vector2 pos, float rot, int index)
	{
		//Create the bridge object at given position and rotation
		GameObject bridge = Instantiate(customize.bridgePrefab, pos, Quaternion.Euler(0,0,rot));
		//@ Setup the newly created bridge
		bridge.transform.SetParent(bridgeGroup.transform);
		bridge.transform.localScale = new Vector2(customize.bridgeWidth, customize.bridgeLength);
		bridge.name = index + " Bridge";
		room.structure.bridge = bridge;
		room.structure.bridgeRender = bridge.GetComponent<SpriteRenderer>();
		//Set the bridge color to default color
		room.structure.bridgeRender.color = customize.bridgeColor;
	}

	void BuildWall(RoomData room,BridgeData bridge,Vector2 pos,float rot,int index,int dir,float length)
	{
		//Create the wall object at given position and rotation
		GameObject wall = Instantiate(customize.wallPrefab, pos, Quaternion.Euler(0,0,rot));
		//@ Setup the newly created wall
		wall.transform.SetParent(wallGroup.transform);
		wall.transform.localScale = new Vector2(customize.wallThick, length + customize.wallLengthModify);
		//@ Create and setup the new wall data
		WallData newWall = new WallData();
		newWall.obj = wall;
		newWall.position = pos;
		newWall.direction = dir;
		newWall.indexBelong = index;
		newWall.renderer = wall.GetComponent<SpriteRenderer>();
		newWall.renderer.color = customize.wallColor;
		//Setup the wall that belong to room
		if(room != null)
		{
			wall.name = "Wall of Room " + index + " in direction " + dir;
			newWall.belongToWall = true;
		}
		//Setup the wall that belong to bridge
		if(bridge != null)
		{
			wall.name = "Wall of Bridge " + index + " in direction " + dir;
			newWall.belongToBridge = true;
		}
		walls.Add(newWall);
	}
#endregion

#region Converter
	Vector2 IndexToDirection(int index)
	{
		//@ Return vector depend on index given from 0-3
		if(index == 0) {return Vector2.up;}
		if(index == 1) {return Vector2.down;}
		if(index == 2) {return Vector2.left;}
		if(index == 3) {return Vector2.right;}
		//Return zero vector if index given are not 0-3
		return Vector2.zero;
	}

	float IndexToRotation(int index)
	{
		//@ Return rotation (360 degree) depend on index given from 0-3
		if(index == 0) {return 0;}
		if(index == 1) {return 180;}
		if(index == 2) {return 90;}
		if(index == 3) {return 270;}
		//Return -1 if index given are not 0-3 and print warning
		Debug.LogWarning("There no rotation for "+index+" index"); return -1;
	}

	int OppositeIndexDirection(int i) 
	{
		//Return the number opposite of index given base direction order
		if(i==0){return 1;} if(i==1){return 0;} if(i==2){return 3;} if(i==3){return 2;}
		//Call an warning an return -1 if index given are not 0-3
		Debug.LogWarning("There no opposite direction for index " + i); return -1;
	}
	
	Vector2 WallOutwardPosition(Vector2 pos, int direction)
	{
		//Try the direction given
		switch(direction)
		{
			//For UP - only Y will INCREASE with half of floor size
			case 0: return new Vector2(pos.x, pos.y + (customize.floorScale.y/2));
			//For DOWN - only Y will DECREASE with half of floor size
			case 1: return new Vector2(pos.x, pos.y - (customize.floorScale.y/2)); 
			//For LEFT - only X will DECREASE with half of floor size
			case 2: return new Vector2(pos.x - (customize.floorScale.x/2), pos.y); 
			//For RIGHT - only X will INCREASE with half of floor size
			case 3: return new Vector2(pos.x + (customize.floorScale.x/2), pos.y);
		}
		//Return zero if the direction are not 0-3
		return Vector2.zero;
	}
#endregion

#region Finder
	public int GetIndexOfRoom(RoomData room)
	{
		//Return the index of given room by go through all the rooms
		for (int n = 0; n < rooms.Count; n++) {if(rooms[n] == room) {return n;}}
		//Return -1 if there is no index at given room
		return -1;
	}

	public RoomData FindRoomAtIndex(int index)
	{
		//Return the room data of room that has the same position as given by go through all the rooms
		for (int n = 0; n < rooms.Count; n++) {if(n == index) {return rooms[n];}}
		//Return null if there is no data for room at position given
		return null;
	}

	public RoomData FindRoomAtCoordinates(Vector2 coordinates)
	{
		//Return the room data of room that has the same coordinates as given by go through all the rooms
		for (int n = 0; n < rooms.Count; n++) {if(rooms[n].coordinates == coordinates) {return rooms[n];}}
		//Return null if there is no data for room at coordinates given
		return null;
	}

	public RoomData FindRoomAtPosition(Vector2 position)
	{
		//Return the room data of room that has the same position as given by go through all the rooms
		for (int n = 0; n < rooms.Count; n++) {if(rooms[n].position == position) {return rooms[n];}}
		//Return null if there is no data for room at position given
		return null;
	}
#endregion
}