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
	public bool autoGenerate; public float autoDelay;
	GameObject floorGroup, bridgeGroup, wallGroup;

#region Classes
	[Serializable] public class ReplicateRequirement {[Range(1,4)] public int min, max;}
	[Serializable] public class RateSetting 
	{
		public float general;
		[Tooltip("Each directon will has their own replicate rate")]
		public bool useDirectional; 
		public float up, down, left, right;
	}
	[Serializable] class Customize
	{
		[Header("Floor")]
		public GameObject floorPrefab;
		public Vector2 floorScale, floorSpacing;
		public Color floorColor, leaderColor, stuckColor;
		[Header("Bridge")]
		public GameObject bridgePrefab;
		public Vector2 bridgeScale;
		public Color bridgeColor;
		public bool connectMode = true;
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
		public Building build = new Building();
		[HideInInspector] public bool stuck, continuation, escape;
		[Serializable] public class Building 
		{
			public GameObject floor, bridge, wall;
			public SpriteRenderer floorRender, bridgeRender;
			public Vector2[] bridgePos = new Vector2[4];
		} 
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
		//Setting build
		SetupBuild();
		//Are now generating
		areGenerating = true;
		//Generate are no longer complete
		completeGenerate = false;
		//Renew the room list
		rooms.Clear(); rooms = new List<RoomData>();
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
		if(rooms.Count >= roomAmount) {leader.build.floorRender.color = customize.floorColor;}
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
		for (int d = 0; d < 4; d++) {CheckDirection(leader, DirectionVector(d), d, result[d]);}
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
			//Finding room in this direction of leader
			RoomData finded = FindRoomAtCoordinates(leader.coordinates + DirectionVector(d));
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
				leader.build.floorRender.color = customize.floorColor;
				yield break;
			}
		}
	#endregion
	#region This leader cycle complete
		/// If haven't got enough room and this leader don't meet the minimum amount of replicate need
		if(rooms.Count < roomAmount && leader.replicateCount < replicateReq.min)
		{
			//Replicate again at this leader 
			StartCoroutine(Replicate(leader, leader));
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
			leader.build.floorRender.color = customize.stuckColor;
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
		//Setting up bridge
		SetupBridge();
		//Auto generate if wanted when complete the current generate
		if(autoGenerate) {Invoke("Generate", autoDelay);}
	}

#endregion

#region Converter
	Vector2 DirectionVector(int index)
	{
		//@ Return vector depend on index given from 0-3
		if(index == 0) {return Vector2.up;}
		if(index == 1) {return Vector2.down;}
		if(index == 2) {return Vector2.left;}
		if(index == 3) {return Vector2.right;}
		//Return zero vector if index given are not 0-3
		return Vector2.zero;
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
	void SetupBuild()
	{
		//Destroy the old floor group if it exist
		if(floorGroup != null) {Destroy(floorGroup);}
		//Create an new group object for floor then edit it name
		floorGroup = new GameObject(); floorGroup.name = "Floor Group";
		//Destroy the old bridge group if it exist
		if(bridgeGroup != null) {Destroy(bridgeGroup);}
		//Create an new group object for bridge then edit it name
		bridgeGroup = new GameObject(); bridgeGroup.name = "Bridge Group";
	}

	void BuildFloor(RoomData room, RoomData prev)
	{
		//Don't build floor if it has already been build
		if(room.build.floor != null) {return;}
		//Build the floor at the room given position
		GameObject floor = Instantiate(customize.floorPrefab, room.position, Quaternion.identity);
		//@ Setup the newly floor bridge
		floor.transform.SetParent(floorGroup.transform);
		floor.transform.localScale = customize.floorScale;
		floor.name = (rooms.Count-1) + " - Floor";
		room.build.floor = floor;
		room.build.floorRender = floor.GetComponent<SpriteRenderer>();
		//Set this room current color to leader color
		room.build.floorRender.color = customize.leaderColor;
		//Set the previous room color to default color
		prev.build.floorRender.color = customize.floorColor;
	}

	void SetupBridge()
	{
		//List of position bridge as builded
		List<Vector2> buildedPos = new List<Vector2>();
		//Go through all the room to go through 4 direction of each room
		for (int r = 0; r < rooms.Count; r++) for (int d = 0; d < 4; d++)
		{
			//Get room position and empty next position in this direction
			Vector2 room = rooms[r].position; Vector2 next = Vector2.zero; 
			///Set the next position as REPLICATED position if using connect mode
			if(customize.connectMode) {next = rooms[r].replicated[d].pos;} 
			///Set the next position as NEIGHBOURS position if not using connect mode
			else {next = rooms[r].neighbours[d].pos;}
			//Stop if the next positon are zero
			if(next == Vector2.zero) {continue;}
			//Decide the rotation of this bridge
			float rot = 0; switch(d)
			{
				//@ Set rotation base on direction index given
				case 0: rot = 0  ; break; 
				case 1: rot = 180; break; 
				case 2: rot = 90 ; break;
				case 3: rot = 270; break;
			}
			//Set position for the bridge at middle point between current and next room
			Vector2 pos = new Vector2(room.x + (next.x - room.x)/2, room.y + (next.y - room.y)/2);
			//Stopped bridge from building at the same position when not using connect mode
			if(!customize.connectMode) {if(buildedPos.Contains(pos)) {continue;} buildedPos.Add(pos);}
			//Save the bridge position in current room
			rooms[r].build.bridgePos[d] = pos;
			//Build the bridge of this room at position and rotation has get
			BuildBridge(rooms[r], pos, rot);
		}
	}

	void BuildBridge(RoomData room , Vector2 pos, float rot)
	{
		//Create the bridge object at position with the current index direction as rotation
		GameObject bridge = Instantiate(customize.bridgePrefab, pos, Quaternion.Euler(0,0,rot));
		//@ Setup the newly created bridge
		bridge.transform.SetParent(bridgeGroup.transform);
		bridge.transform.localScale = customize.bridgeScale;
		bridge.name = (room.index + 1) + "'s Bridge";
		room.build.bridge = bridge;
		room.build.bridgeRender = bridge.GetComponent<SpriteRenderer>();
		//Set the bridge color to default
		room.build.bridgeRender.color = customize.bridgeColor;
	}
#endregion
}