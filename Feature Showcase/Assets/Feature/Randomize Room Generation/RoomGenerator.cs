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
	public bool autoGenerate; [Min(0.1f)] public float autoSpeed;
	GameObject floorGroup;

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
		public Vector2 scale, spacing;
		public GameObject floorPrefab;
		public Color floorColor, leaderColor, stuckColor;
	}
	[Serializable] public class RoomData
	{
		public Vector2 coordinates, position;
		public int replicateCount;
		public Vector2[] replicates = new Vector2[4];
		public int neighboursCount;
		[Serializable] public class Neighbours {public bool filled; public Vector2 coord;}
		public Neighbours[] neighbours = new Neighbours[] {new Neighbours(),new Neighbours(),new Neighbours(),new Neighbours()};
		[Serializable] public class Building 
		{
			public GameObject floor, bridge, wall;
			public SpriteRenderer floorRender; 
		} 
		public Building build = new Building();
		[HideInInspector] public bool stuck, continuation, escape;
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
		//Are now generating
		areGenerating = true;
		//Destroy the old room group if it exist
		if(floorGroup != null) {Destroy(floorGroup);}
		//Create an new group object for floor then edit it name
		floorGroup = new GameObject(); floorGroup.name = "Floor Group";
		//No longer has generated
		completeGenerate = false;
		//Renew the room list
		rooms.Clear(); rooms = new List<RoomData>();
		//Add the first empty room than replicate at it
		rooms.Add(new RoomData()); StartCoroutine(Replicate(rooms[0], rooms[0]));
	}

	IEnumerator Replicate(RoomData leader, RoomData prev, int forceDirection = -1)
	{
		//This room are now the current leader
		currentLeaders.Add(leader);
		//Build floor at this leader
		BuildFloor(leader, prev);
		//! If THIS leader room are the final room (Improving later by only when truly complete)
		if(rooms.Count >= roomAmount)
		{
			//Call the on generated event
			onGenerated?.Invoke();
			//Has completed generation
			areGenerating = false; completeGenerate = true;
			//Auto generate if wanted when complete the current generate
			if(autoGenerate) {Invoke("Generate", autoSpeed);}
		}
		//Wait for an frame
		yield return null;
		//If THOSE leader room are the final room
		if(rooms.Count >= roomAmount)
		{
			//Revert those room room floor color back to normal
			leader.build.floorRender.color = customize.floorColor;
		}
	#region Random Replicate
		//The result to see what direction will be replicate
		bool[] result = new bool[4];
		/// If this leader has been force to use an direction
		if(forceDirection != -1)
		{
			//Set the result at direction has been force to true
			result[forceDirection] = true;
		}
		/// Or else this leader need to randomize it own direction
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
	#endregion
		//Go through all 4 direction to check all of them
		for (int d = 0; d < 4; d++) {CheckDirection(leader, DirectionVector(d), d, result[d]);}
		//This room are no longer the current leader
		currentLeaders.Remove(leader);
		//Stop going further if this leader are an continuation room
		if(leader.continuation) {yield break;}
		//Attemp to escape again at this leader if the leader is still stuck
		if(leader.stuck) {StartCoroutine(Replicate(leader, leader)); yield break;}
	#region Check neighbour
		//Go through all 4 direction of this leader when there sill neighbours to check
		for (int d = 0; d < 4; d++)
		{
			//Findind room at four direction in leader
			RoomData finded = FindroomAtCoordinates(leader.coordinates + DirectionVector(d));
			//Save the opposite direction of current direction
			int o = OppositeDirection(d);
			//Only countine at this direction if it has room and that room neighbours is empty
			if(finded == null) {continue;} if(leader.neighbours[d].filled) {continue;}
			//Increase the neighbours count of both leader and finded
			leader.neighboursCount++; finded.neighboursCount++;
			//Set neighbour of this leader filled at current direction and finded room at opposite
			leader.neighbours[d].filled = true; finded.neighbours[o].filled = true;
		}
		//When the neighbours are filled from all 4 side
		if(leader.neighboursCount >= 4) 
		{
			//This leader is now stuck if it is the only leader
			if(currentLeaders.Count <= 1) {leader.stuck = true;}
			//? Don't attempt to replicate again if there is still leader (and reset color to default)
			else if(currentLeaders.Count > 1) {leader.build.floorRender.color = customize.floorColor; yield break;}
		}
	#endregion
		/// If haven't got enough room and this leader don't meet the minimum amount of replicate need
		if(rooms.Count < roomAmount && leader.replicateCount < replicateReq.min)
		{
			//Replicate again at this leader 
			StartCoroutine(Replicate(leader, leader));
		}
	}

	void CheckDirection(RoomData leader, Vector2 vector, int index, bool needReplicate)
	{
		//Get the next room at given direction's vector
		RoomData nextRoom = FindroomAtCoordinates(leader.coordinates + vector);
		//Set the leader neighbours coordinates as next room coordinates if it exist
		if(nextRoom != null) {leader.neighbours[index].coord = nextRoom.coordinates;}
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
			newRoom.coordinates.x * (customize.scale.x + customize.spacing.x),
			//Multiple the new room Y coordinates with size + spacing
			newRoom.coordinates.y * (customize.scale.y + customize.spacing.y)
		);
		//Add the new room into list
		rooms.Add(newRoom);
		//This leader has replicate an new room if it not an escape replicate
		if(!leader.escape) {leader.replicateCount++;}
		//Save the coordinates of thie new room leader has replicate
		leader.replicates[index] = newRoom.coordinates;
		//Begin replicate more at the new room with previous room being leader
		StartCoroutine(Replicate(newRoom, leader));
	}

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

	int OppositeDirection(int i) 
	{
		//Return the number opposite of index given base direction order
		if(i==0){return 1;} if(i==1){return 0;} if(i==2){return 3;} if(i==3){return 2;}
		//Call an warning an return -1 if index given are not 0-3
		Debug.LogWarning("There no opposite direction for index " + i); return -1;
	}
#endregion

#region Finder
	public int GetIndexOfroom(RoomData room)
	{
		//Return the index of given room by go through all the rooms
		for (int n = 0; n < rooms.Count; n++) {if(rooms[n] == room) {return n;}}
		//Return -1 if there is no index at given room
		return -1;
	}

	public RoomData FindroomAtIndex(int index)
	{
		//Return the room data of room that has the same position as given by go through all the rooms
		for (int n = 0; n < rooms.Count; n++) {if(n == index) {return rooms[n];}}
		//Return null if there is no data for room at position given
		return null;
	}

	public RoomData FindroomAtCoordinates(Vector2 coordinates)
	{
		//Return the room data of room that has the same coordinates as given by go through all the rooms
		for (int n = 0; n < rooms.Count; n++) {if(rooms[n].coordinates == coordinates) {return rooms[n];}}
		//Return null if there is no data for room at coordinates given
		return null;
	}

	public RoomData FindroomAtPosition(Vector2 position)
	{
		//Return the room data of room that has the same position as given by go through all the rooms
		for (int n = 0; n < rooms.Count; n++) {if(rooms[n].position == position) {return rooms[n];}}
		//Return null if there is no data for room at position given
		return null;
	}
#endregion

	void BuildFloor(RoomData room, RoomData prev)
	{
		//Don't build floor if it has already been build
		if(room.build.floor != null) {return;}
		//Build the floor at the room given position
		GameObject floor = Instantiate(customize.floorPrefab, room.position, Quaternion.identity);
		floor.transform.SetParent(floorGroup.transform);
		floor.transform.localScale = customize.scale;
		floor.name = (rooms.Count-1) + " - Floor";
		room.build.floor = floor;
		room.build.floorRender = floor.GetComponent<SpriteRenderer>();
		//Set this room color to leader color
		room.build.floorRender.color = customize.leaderColor;
		//Set the previous room color to default color
		prev.build.floorRender.color = customize.floorColor;
	}
}