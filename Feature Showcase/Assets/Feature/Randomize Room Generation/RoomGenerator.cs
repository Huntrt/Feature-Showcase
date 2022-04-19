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
	[HideInInspector]
	public bool completeGenerate, areGenerating; 
	public event Action onGenerated;
	public List<RoomData> currentLeaders = new List<RoomData>();
	public List<RoomData> rooms = new List<RoomData>();
	public List<BridgeData> bridges = new List<BridgeData>();
	public bool autoGenerate; public float autoDelay;
	GameObject floorGroup, bridgeGroup, wallGroup;

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
		public GameObject floorPrefab;
		public Vector2 floorScale, floorSpacing;
		[Header("Bridge")]
		public Color bridgeColor;
		public GameObject bridgePrefab;
		public float bridgeWidth, bridgeLength;
		[Tooltip("Will the bridge connect to every room or connect by how room was replicate")]
		public bool neighbourMode;
		[Header("Wall")]
		public Color wallColor;
		public GameObject wallPrefab;
		public float wallThick, wallLengthModify;
		public enum WallAlign {center, outside, inside} public WallAlign wallAlign;
		[Tooltip("Will the wall block all side of room or only block the side has nothing")]
		public bool enclosedMode;
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
			public GameObject floor, bridge, wall;
			public SpriteRenderer floorRender, bridgeRender, wallRender;
			public bool[] hasBridge = new bool[4];
		} 
	}
	[Serializable] public class BridgeData
	{
		public int index;
		public Vector2[] connectPosition = new Vector2[2]; 
		public Vector2 position;
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
		//Preparering to for structure build
		PrepareStructure();
		//Are now generating
		areGenerating = true;
		//Generate are no longer complete
		completeGenerate = false;
		//Renew the room list
		rooms.Clear(); rooms = new List<RoomData>();
		//Renew the bridges list
		bridges.Clear(); bridges = new List<BridgeData>();
		//Add the first empty room than replicate at it
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
				//Return the result base on general the rate compare to chance
				else {if(replicateRate.general >= chance) {result[d] = true;}}
			}
		}
		//Go through all 4 direction to check all of them for potential replicate
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
				//Get the neighbours at opposite direction of finded room
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

	void CheckDirection(RoomData leader, Vector2 vector, int index, bool needReplicate)
	{
		//Get the next room at given direction
		RoomData nextRoom = FindRoomAtCoordinates(leader.coordinates + vector);
		//Stop if this direction don't need replication
		if(!needReplicate) {return;}
		//! If the leader are stuck
		if(leader.stuck)
		{
			leader.structure.floorRender.color = customize.debug.stuckColor;
			//If there already an room ar the next room
			if(nextRoom != null)
			{
				//The leader has continue escape attempt at the next room
				leader.continuation = true;
				//The leader transfer it stuck to the next room
				leader.stuck = false; nextRoom.stuck = true; 
				//Try to escape at the next room with the same direction
				StartCoroutine(Replicate(nextRoom, leader, index)); return;
			}
			//If there are no room at the next room
			else
			{
				//The leader are no longer stuck
				leader.stuck = false;
				//The leader has escape using this direction
				leader.escape = true;
			}
		}
		//If the leader are not stuck
		if(!leader.stuck)
		{
			//Ignore the max replicate restrainy if this leader is an ESCAPE room
			if(!leader.escape && leader.replicateCount >= replicateReq.max) {return;}
			/// Stop if has reach the needed amount of room or next room are not empty
			if(rooms.Count >= roomAmount || nextRoom != null) {return;}
		}
		//Create an new temp room
		RoomData newRoom = new RoomData();
		//Set the new room coordiates as the leader coordinates increase with this direction
		newRoom.coordinates = leader.coordinates + vector;
		//Set the new room's position
		newRoom.position = new Vector2
		(
			//Multiple the new room X coordinates with size + spacing
			newRoom.coordinates.x * (customize.floorScale.x + customize.floorSpacing.x),
			//Multiple the new room Y coordinates with size + spacing
			newRoom.coordinates.y * (customize.floorScale.y + customize.floorSpacing.y)
		);
		//Add the new room into list
		rooms.Add(newRoom);
		//Update the new room's index
		newRoom.index = rooms.Count-1;
		//This leader has replicate an new room if it not an escape replicate
		if(!leader.escape) {leader.replicateCount++;}
		//Save the coordinates of the new room leader has replicate
		leader.replicated[index].coord = newRoom.coordinates;
		//Save the position of the new room leader has replicate
		leader.replicated[index].pos = newRoom.position;
		//Begin replicate more at the new room with previous room being leader
		StartCoroutine(Replicate(newRoom, leader));
	}

	void GeneratingFinish()
	{
		//Call the on generated event
		onGenerated?.Invoke();
		//Has completed generation
		areGenerating = false; completeGenerate = true;
		//Setting up bridge and wall
		SetupBridge(); SetupWall();
		//Reset all the floor color to default
		for (int r = 0; r < rooms.Count; r++) {rooms[r].structure.floorRender.color = customize.floorColor;}
		//Auto generate if wanted when complete the current generate
		if(autoGenerate) {Invoke("Generate", autoDelay);}
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

#region Build
	void PrepareStructure()
	{
		//Destroy the old floor group if it exist
		if(floorGroup != null) {Destroy(floorGroup);}
		//Create an new group object for floor then edit it name
		floorGroup = new GameObject(); floorGroup.name = "Floor Group";
		//Destroy the old bridge group if it exist
		if(bridgeGroup != null) {Destroy(bridgeGroup);}
		//Create an new group object for bridge then edit it name
		bridgeGroup = new GameObject(); bridgeGroup.name = "Bridge Group";
		//Destroy the old wall group if it exist
		if(wallGroup != null) {Destroy(wallGroup);}
		//Create an new group object for wall then edit it name
		wallGroup = new GameObject(); wallGroup.name = "Wall Group";
	}

	void SetupBridge()
	{
		//Count to assign index for bridge
		int indexCounter = 0;
		//Go through all the room to go through 4 direction of each room
		for (int r = 0; r < rooms.Count; r++) for (int d = 0; d < 4; d++)
		{
			//Get room position and empty next position in this direction
			Vector2 roomP = rooms[r].position; Vector2 nextP = Vector2.zero; 
			///Set the next position as NEIGHBOURS position if not using connect mode
			if(customize.neighbourMode) {nextP = rooms[r].neighbours[d].pos;} 
			///Set the next position as REPLICATED position if using connect mode
			else {nextP = rooms[r].replicated[d].pos;}
			//Stop if the next positon are zero
			if(nextP == Vector2.zero) {continue;}
			//Set position for the bridge at middle point between current and next room
			Vector2 pos = new Vector2(roomP.x + (nextP.x - roomP.x)/2, roomP.y + (nextP.y - roomP.y)/2);
			//Create an new bridge data
			BridgeData newBridge = new BridgeData();
			//Set the new bridge's index and position
			newBridge.index = indexCounter; newBridge.position = pos;
			//Set the bridge 1st connection as current room position and 2nd as room at next position
			newBridge.connectPosition[0] = roomP; newBridge.connectPosition[1] = nextP;
			//If the new bridge is not an duplicate
			if(!bridges.Contains(newBridge))
			{
				//The current room now has bridge at current direction
				rooms[r].structure.hasBridge[d] = true;
				//The next room new has bridge at opposite direction
				FindRoomAtPosition(nextP).structure.hasBridge[OppositeIndexDirection(d)] = true;
				//Increase the bridge index counter
				indexCounter++;
				//Add the newly bridge into list
				bridges.Add(newBridge);
				//Build the bridge of this room at position and rotation of index direction
				BuildBridge(rooms[r], pos, IndexToRotation(d), newBridge.index);
			}
		}
	}

	void SetupWall()
	{
		//Go through all the room to go through 4 direction of each room
		for (int r = 0; r < rooms.Count; r++) for (int d = 0; d < 4; d++)
		{
			//Save the current room position
			Vector2 roomPos = rooms[r].position;
			//Get the position that has pushed outward in this direction from room
			Vector2 outPos = WallOutwardPosition(roomPos, d);
			//Get the current adjacent by checking direction Up/Down = 0 and Left/Right = 1
			int adjacent = 0; if(d==2 || d==3) {adjacent = 1;}
			//Get the rotation of current index then decrease it by -90
			float rot = IndexToRotation(d) - 90;
			//Get the half of the wall thick value as positive if up/right and negative if left/down
			float thick = customize.wallThick/2; if(d==1||d==2) {thick = -thick;}
			//The length of wall
			float length = 0;
			//Get the wall alignment choosed
			switch(customize.wallAlign.ToString())
			{
				//Length to 1 and thick to 0 if choose center mode
				case "center": length = 1; thick = 0; break;
				//Length to 2 and not chaging thick if choose outside mode 
				case "outside": length = 2; break;
				//Length to 0 and thick to negative if choose inside mode 
				case "inside": length = 0; thick = -thick; break;
			}
			//Multiply length with wall thick to know how many thick need to add onto length
			length *= customize.wallThick;
			//If adjacent are UP/DOWN
			if(adjacent == 0)
			{
				//Increase Y axis of out position with thick
				outPos.y += thick;
				//Increase floor scale X axis with length
				length += customize.floorScale.x;
			}
			//If adjacent are LEFT/RIGHT
			else
			{
				//Increase X axis of out position with thick
				outPos.x += thick;
				//Increase floor scale Y axis with length
				length += customize.floorScale.y;
			}
			/// If using Enclosed mode
			if(customize.enclosedMode)
			{
				//Build an wall normally for this room if this direction don't has bridge
				if(!rooms[r].structure.hasBridge[d]) {BuildWall(rooms[r], outPos, rot, r, d, length);}
				//Create an gate for this room if this direction if it has bridge
				else {Wall_BuildGate(r, d, outPos, roomPos, rot, adjacent, length);}
			}
			/// Else now in connect mode instead with direction has NO neighbours
			if(!customize.enclosedMode && !rooms[r].neighbours[d].filled)
			{
				//? This are for cutting part of wall that poke into floor
				//The value to cut off this wall's length are wall thick
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
				if(adjacent == 0)
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
				BuildWall(rooms[r], outPos, rot, r, d, length);
			}
		}
	}

	void Wall_BuildGate(int r, int d, Vector2 outPos, Vector2 roomPos, float rot, int adjacent, float length)
	{
		//Center are room position X axis if adjacent up/down or Y if adjacent left/right
		float center = 0; if(adjacent == 0) {center = roomPos.x;} else {center = roomPos.y;}
		//Get the bridge value by increase half of bridge width from center
		float bridge = center + (customize.bridgeWidth/2);
		//Get the edge value by increase half of length from center
		float edge = center + length/2;
		//The positive side are the middle point between edge and bridge
		float sidePOS = (bridge + (edge - bridge)/2);
		//The negative side get by decrease muitple value from the positive side
		float sideNEG = sidePOS - ((sidePOS - bridge)*2) - customize.bridgeWidth;
		//The current are now the value between edge and bridge
		length = edge - bridge;
		//If adjacent are UP/DOWN
		if(adjacent == 0)
		{
			//Build wall of room at side positive as X and outward as Y with custom length
			BuildWall(rooms[r], new Vector2(sidePOS, outPos.y), rot, r,d, length);
			//Build wall of room at side negative as X and outward as Y with custom length
			BuildWall(rooms[r], new Vector2(sideNEG, outPos.y), rot, r,d, length);
		}
		//If adjacent are LEFT/RIGHT
		else 
		{
			//Build wall of room at outward as X and side positive as Y with custom length
			BuildWall(rooms[r], new Vector2(outPos.x, sidePOS), rot, r,d, length);
			//Build wall of room at outward as X and side negative as Y with custom length
			BuildWall(rooms[r], new Vector2(outPos.x, sideNEG), rot, r,d, length);
		}
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

	void BuildFloor(RoomData room, RoomData prev)
	{
		//Don't build floor if it has already been build
		if(room.structure.floor != null) {return;}
		//Build the floor at the room given position with no rotation
		GameObject floor = Instantiate(customize.floorPrefab, room.position, Quaternion.identity);
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

	void BuildWall(RoomData room , Vector2 pos, float rot, int belong, int direction, float length)
	{
		//Create the wall object at given position and rotation
		GameObject wall = Instantiate(customize.wallPrefab, pos, Quaternion.Euler(0,0,rot));
		//@ Setup the newly created wall
		wall.transform.SetParent(wallGroup.transform);
		wall.transform.localScale = new Vector2(customize.wallThick, length + customize.wallLengthModify);
		wall.name = "Wall of Room " + belong + " as index " + direction;
		room.structure.wall = wall;
		room.structure.wallRender = wall.GetComponent<SpriteRenderer>();
		//Set the wall color to default color
		room.structure.wallRender.color = customize.wallColor;
	}
#endregion
}