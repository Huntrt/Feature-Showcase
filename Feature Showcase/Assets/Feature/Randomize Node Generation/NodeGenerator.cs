using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using System;

public class NodeGenerator : MonoBehaviour
{
	[Serializable] class Testing 
	{
		public bool autoGenerate; 
		[Min(0.1f)]public float autoSpeed;
		public Color floorColor, leaderColor;
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
	public List<NodeData> nodes = new List<NodeData>();

	#region Classes
	[Serializable] public class ReplicateAmount {[Range(1,4)] public int min, max;}
	[Serializable] public class NodeSetting {public Vector2 scale, spacing;}
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

	IEnumerator Replicate(NodeData leader, NodeData prev)
	{
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
		//? Randomly replicate
		//The result too see what direction will be replicate
		bool[] result = new bool[4];
		//Go through all the result need to calculated
		for (int c = 0; c < result.Length; c++)
		{
			//The chance of this cycle
			float chance = UnityEngine.Random.Range(0f, 100f);
			//If using the directional rate
			if(replicateRate.useDirectional)
			{
				//@ Return the result base on rate of each direction
				if(c == 0 && replicateRate.up    >= chance) {result[c] = true;}
				if(c == 1 && replicateRate.down  >= chance) {result[c] = true;}
				if(c == 2 && replicateRate.left  >= chance) {result[c] = true;}
				if(c == 3 && replicateRate.right >= chance) {result[c] = true;}
			}
			//Return the result base on the rate if not use directional rate
			else {if(replicateRate.general >= chance) {result[c] = true;}}
		}
		//Go through all 4 direction to check each direction at this leader node
		for (int d = 0; d < 4; d++) {CheckDirection(leader, DirectionVector(d), d, result[d]);}
		//If this leader haven't replcate the minimum amount of node needed
		if(nodes.Count < amount && leader.replicateCount < replicateAmount.min)
		{
			//Replicate again at this leader
			StartCoroutine(Replicate(leader, leader));
		}
		//? Check neighbour
		//Go through all 4 direction of this leader
		for (int d = 0; d < 4; d++)
		{
			//Search the node at 4 direction at this leader
			NodeData searched = SearchNodeCoordinates(leader.coordinates + DirectionVector(d));
			//Save the opposite direction of current direction
			int o = OppositeDirection(d);
			//Only countine node at this direction if it has been search and it neighbours are empty
			if(searched == null) {continue;} if(leader.neighbours[d].filled) {continue;}
			//Increase the neighbours count of both leader and search
			leader.neighboursCount++; searched.neighboursCount++;
			//Set neighbour of leader filled at current direction and searched node at opposite
			leader.neighbours[d].filled = true;searched.neighbours[o].filled = true;
		}
	}

	void CheckDirection(NodeData leader, Vector2 vector, int index, bool needReplicate)
	{
		//Getting the coordinates of direction by increase it given vector with leader
		Vector2 directionCoord = leader.coordinates + vector;
		//Set the coordinates of this leader neighbours at index direction
		leader.neighbours[index].coord = directionCoord;
		///Only replicate when needed or still need more node or has reach the max amount of replicating
		if(!needReplicate || nodes.Count >= amount || leader.replicateCount > replicateAmount.max) {return;}
		//Create an new temp node
		NodeData newNode = new NodeData();
		//Set the new node coordiates as the direction coordinates
		newNode.coordinates = directionCoord;
		//Stop if the new node coordinates are already taken
		if(!IsEmpty(newNode.coordinates)) {return;}
		//Set the new node's position
		newNode.position = new Vector2
		(
			//Multiple the leader X coordinates with size + spacing
			directionCoord.x * (nodeSetting.scale.x + nodeSetting.spacing.x),
			//Multiple the leader Y coordinates with size + spacing
			directionCoord.y * (nodeSetting.scale.y + nodeSetting.spacing.y)
		);
		//Add the new node into list
		nodes.Add(newNode);
		//This leader has replicate an node
		leader.replicateCount++;
		//Save the coordinates of this node leader has replicate
		leader.replicates[index] = newNode.coordinates;
		//Begin replicate more
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
		//Call an warning an return vector zero if index given are not 0-3
		Debug.LogWarning("There no direction for index " + index); return Vector2.zero;
	}

	int OppositeDirection(int i) 
	{
		//Return the number opposite of index given base direction order
		if(i==0){return 1;} if(i==1){return 0;} if(i==2){return 3;} if(i==3){return 2;}
		//Call an warning an return -1 if index given are not 0-3
		Debug.LogWarning("There no opposite direction for index " + i); return -1;
	}
#endregion

#region Searcher
	public int GetIndexOfNode(NodeData node)
	{
		//Return the index of given node by go through all the nodes
		for (int n = 0; n < nodes.Count; n++) {if(nodes[n] == node) {return n;}}
		//Return -1 if there is no index at given node
		return -1;
	}

	public NodeData SearchNodeIndex(int index)
	{
		//Return the node data of node that has the same position as given by go through all the nodes
		for (int n = 0; n < nodes.Count; n++) {if(n == index) {return nodes[n];}}
		//Return null if there is no data for node at position given
		return null;
	}

	public NodeData SearchNodeCoordinates(Vector2 coordinates)
	{
		//Return the node data of node that has the same coordinates as given by go through all the nodes
		for (int n = 0; n < nodes.Count; n++) {if(nodes[n].coordinates == coordinates) {return nodes[n];}}
		//Return null if there is no data for node at coordinates given
		return null;
	}

	public NodeData SearchNodePosition(Vector2 position)
	{
		//Return the node data of node that has the same position as given by go through all the nodes
		for (int n = 0; n < nodes.Count; n++) {if(nodes[n].position == position) {return nodes[n];}}
		//Return null if there is no data for node at position given
		return null;
	}

	public bool IsEmpty(Vector2 coordinates)
	{
		//Return false if when go through all the nodes and there already an coodrinated same as given
		for (int n = 0; n < nodes.Count; n++) {if(nodes[n].coordinates == coordinates) {return false;}}
		//There is empty node at coordinates cause no node has the same coordinates given
		return true;
	}
#endregion

	void BuildFloor(NodeData node, NodeData prev)
	{
		//Don't build floor if it has already been build
		if(node.build.floor != null) {return;}
		//Build the floor at the node given position
		GameObject floor = Instantiate(floorPrefab, node.position, Quaternion.identity);
		//Group the floor
		floor.transform.SetParent(floorGroup.transform);
		//Set the floor size as node size
		floor.transform.localScale = nodeSetting.scale;
		//Set the floor name with index
		floor.name = (nodes.Count-1) + " - Floor";
		//Save the floor object inside the node building it
		node.build.floor = floor;
		//Save the floot render inside the node building it
		node.build.floorRender = floor.GetComponent<SpriteRenderer>();
		//Set the floor color to it the replicator color
		node.build.floorRender.color = testing.leaderColor;
		//Revert the previous node floor color back to normal
		prev.build.floorRender.color = testing.floorColor;
	}
}