using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using System;

public class NodeGenerator : MonoBehaviour
{
	[Serializable] class Testing 
	{
		public bool autoGenerate; 
		[Min(0.1f)] public float autoSpeed;
		public Color floorColor, leaderColor, stuckColor;
	}
	[SerializeField] 
	Testing testing = new Testing();
	public GameObject floorPrefab; GameObject floorGroup;
	[Tooltip("How many node will be generate")]
	public int amount;
	[Tooltip("The minimum and maximum amount of replicate an node need to do")]
	public ReplicateAmount replicateAmount = new ReplicateAmount();
	[Tooltip("The percent chance of generate")]
	public RateSetting replicateRate = new RateSetting();
	[HideInInspector] public bool completeGenerate, areGenerating; public event Action onGenerated;
	[Tooltip("Setting for node")]
	public NodeSetting nodeSetting;
	public List<NodeData> currentLeaders = new List<NodeData>();
	public List<NodeData> nodes = new List<NodeData>();

#region Classes
	[Serializable] public class ReplicateAmount 
	{[Range(1,4)] public int min, max;}
	[Serializable] public class NodeSetting 
	{public Vector2 scale, spacing;}
	[Serializable] public class RateSetting 
	{public float general; public bool useDirectional; public float up, down, left, right;}
	[Serializable] public class NodeData
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
		//Destroy the old node group if it exist
		if(floorGroup != null) {Destroy(floorGroup);}
		//Create an new group object for floor then edit it name
		floorGroup = new GameObject(); floorGroup.name = "Floor Group";
		//No longer has generated
		completeGenerate = false;
		//Renew the node list
		nodes.Clear(); nodes = new List<NodeData>();
		//Add the first empty node than replicate at it
		nodes.Add(new NodeData()); StartCoroutine(Replicate(nodes[0], nodes[0]));
	}

	IEnumerator Replicate(NodeData leader, NodeData prev, int forceDirection = -1)
	{
		//This node are now the current leader
		currentLeaders.Add(leader);
		//Build floor at this leader
		BuildFloor(leader, prev);
		//! If THIS leader node are the final node (Improving later by only when truly complete)
		if(nodes.Count >= amount)
		{
			//Call the on generated event
			onGenerated?.Invoke();
			//Has completed generation
			areGenerating = false; completeGenerate = true;
			//Auto generate if wanted when complete the current generate
			if(testing.autoGenerate) {Invoke("Generate", testing.autoSpeed);}
		}
		//Wait for an frame
		yield return null;
		//If THOSE leader node are the final node
		if(nodes.Count >= amount)
		{
			//Revert those node node floor color back to normal
			leader.build.floorRender.color = testing.floorColor;
		}
	#region Random Replicate
		//The result to see what direction will be replicate
		bool[] result = new bool[4];
		//If this leader has been force to use an direction
		if(forceDirection != -1)
		{
			//Set the result at direction has been force to true
			result[forceDirection] = true;
		}
		//If this leader need to randomize it own direction
		else
		{
			//Go through all the result need to randomize
			for (int c = 0; c < result.Length; c++)
			{
				//The randomize chance of this cycle
				float chance = UnityEngine.Random.Range(0f, 100f);
				//If using the directional rate
				if(replicateRate.useDirectional)
				{
					//@ Set the result base on rate of each direction compare to chance
					if(c == 0 && replicateRate.up    >= chance) {result[c] = true;}
					if(c == 1 && replicateRate.down  >= chance) {result[c] = true;}
					if(c == 2 && replicateRate.left  >= chance) {result[c] = true;}
					if(c == 3 && replicateRate.right >= chance) {result[c] = true;}
				}
				//Return the result base on general the rate compare to chance
				else {if(replicateRate.general >= chance) {result[c] = true;}}
			}
		}
	#endregion
		//! Shuffle drection
		//Go through all 4 direction to check each of them
		for (int d = 0; d < 4; d++) {CheckDirection(leader, DirectionVector(d), d, result[d]);}
		//This node are no longer the current leader
		currentLeaders.Remove(leader);
		//Stop going further if this leader are an continuation node
		if(leader.continuation) {yield break;}
		//Attemp to escape again at this leader if the leader is still stuck
		if(leader.stuck) {StartCoroutine(Replicate(leader, leader)); yield break;}
	#region Check neighbour
		//Go through all 4 direction of this leader when there sill neighbours to check
		for (int d = 0; d < 4; d++)
		{
			//Findind node at four direction in leader
			NodeData finded = FindNodeAtCoordinates(leader.coordinates + DirectionVector(d));
			//Save the opposite direction of current direction
			int o = OppositeDirection(d);
			//Only countine at this direction if it has node and that node neighbours is empty
			if(finded == null) {continue;} if(leader.neighbours[d].filled) {continue;}
			//Increase the neighbours count of both leader and finded
			leader.neighboursCount++; finded.neighboursCount++;
			//Set neighbour of this leader filled at current direction and finded node at opposite
			leader.neighbours[d].filled = true; finded.neighbours[o].filled = true;
		}
		//When the neighbours are filled from all 4 side
		if(leader.neighboursCount >= 4) 
		{
			//This leader is now stuck if it is the only leader
			if(currentLeaders.Count <= 1) {leader.stuck = true;}
			//? Don't attempt to replicate again if there is still leader (and reset color to default)
			else if(currentLeaders.Count > 1) {leader.build.floorRender.color = testing.floorColor; yield break;}
		}
	#endregion
		/// If haven't got enough node and this leader don't meet the minimum amount of replicate need
		if(nodes.Count < amount && leader.replicateCount < replicateAmount.min)
		{
			//Replicate again at this leader 
			StartCoroutine(Replicate(leader, leader));
		}
	}

	void CheckDirection(NodeData leader, Vector2 vector, int index, bool needReplicate)
	{
		//Get the next node to replicate that are at this leader direction
		NodeData nextNode = FindNodeAtCoordinates(leader.coordinates + vector);
		//Set the leader neighbours coordinates as next node coordinates if it exist
		if(nextNode != null) {leader.neighbours[index].coord = nextNode.coordinates;}
		//Stop if this direction don't need replication
		if(!needReplicate) {return;}
		//! If the leader are stuck
		if(leader.stuck)
		{
			leader.build.floorRender.color = testing.stuckColor;
			//If there already an node ar the next node
			if(nextNode != null)
			{
				//The leader has continue escape attempt at the next node
				leader.continuation = true;
				//The leader transfer it stuck to the next node
				leader.stuck = false; nextNode.stuck = true; 
				//Try to escape at the next node with the same direction
				StartCoroutine(Replicate(nextNode, leader, index)); return;
			}
			//If there are no node at the next node
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
			//Ignore the max replicate if this leader is an escape node
			if(!leader.escape && leader.replicateCount >= replicateAmount.max) {return;}
			//Stop if the has reach the needed node amount of next node are not empty
			if(nodes.Count >= amount || nextNode != null) {return;}
		}
		//Create an new temp node
		NodeData newNode = new NodeData();
		//Set the new node coordiates as the leader coordinates increase with this direction
		newNode.coordinates = leader.coordinates + vector;
		//Set the new node's position
		newNode.position = new Vector2
		(
			//Multiple the new node X coordinates with size + spacing
			newNode.coordinates.x * (nodeSetting.scale.x + nodeSetting.spacing.x),
			//Multiple the new node Y coordinates with size + spacing
			newNode.coordinates.y * (nodeSetting.scale.y + nodeSetting.spacing.y)
		);
		//Add the new node into list
		nodes.Add(newNode);
		//This leader has replicate an new node if it not an escape replicate
		if(!leader.escape) {leader.replicateCount++;}
		//Save the coordinates of thie new node leader has replicate
		leader.replicates[index] = newNode.coordinates;
		//Begin replicate more at the new node with previous node being leader
		StartCoroutine(Replicate(newNode, leader));
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
	public int GetIndexOfNode(NodeData node)
	{
		//Return the index of given node by go through all the nodes
		for (int n = 0; n < nodes.Count; n++) {if(nodes[n] == node) {return n;}}
		//Return -1 if there is no index at given node
		return -1;
	}

	public NodeData FindNodeAtIndex(int index)
	{
		//Return the node data of node that has the same position as given by go through all the nodes
		for (int n = 0; n < nodes.Count; n++) {if(n == index) {return nodes[n];}}
		//Return null if there is no data for node at position given
		return null;
	}

	public NodeData FindNodeAtCoordinates(Vector2 coordinates)
	{
		//Return the node data of node that has the same coordinates as given by go through all the nodes
		for (int n = 0; n < nodes.Count; n++) {if(nodes[n].coordinates == coordinates) {return nodes[n];}}
		//Return null if there is no data for node at coordinates given
		return null;
	}

	public NodeData FindNodeAtPosition(Vector2 position)
	{
		//Return the node data of node that has the same position as given by go through all the nodes
		for (int n = 0; n < nodes.Count; n++) {if(nodes[n].position == position) {return nodes[n];}}
		//Return null if there is no data for node at position given
		return null;
	}
#endregion

	void BuildFloor(NodeData node, NodeData prev)
	{
		//Don't build floor if it has already been build
		if(node.build.floor != null) {return;}
		//Build the floor at the node given position
		GameObject floor = Instantiate(floorPrefab, node.position, Quaternion.identity);
		floor.transform.SetParent(floorGroup.transform);
		floor.transform.localScale = nodeSetting.scale;
		floor.name = (nodes.Count-1) + " - Floor";
		node.build.floor = floor;
		node.build.floorRender = floor.GetComponent<SpriteRenderer>();
		//Set this node color to leader color
		node.build.floorRender.color = testing.leaderColor;
		//Set the previous node color to default color
		prev.build.floorRender.color = testing.floorColor;
	}
}