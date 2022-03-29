using System.Collections.Generic;
using UnityEngine;
using System;

[Serializable] public class NodeData
{
	public Vector2 coordinates, position;
	public int replicated;
	public Vector2[] neighbours = new Vector2[4];
}

public class NodeGenerator : MonoBehaviour
{
	public GameObject plainPrefab; GameObject group;
	[Tooltip("How many node will be generate")]
	public int amount;
	[Serializable] class Rate {public float up, down, left, right;}
	[Tooltip("The percent chance of generating another node at any direction")]
	[SerializeField] Rate rate = new Rate();
	[Tooltip("The minimum amount of direction an node need to generate")]
    public int directionRequired;
	[Tooltip("How far away node is from each other")]
	public float spacing;
	public bool hasGenerated; public event Action completeGenerate;
	public List<NodeData> nodes = new List<NodeData>();
	[Header("Testing")] [Tooltip("How fast it will automatic generate (0 are disable)")]
	public float automaticSpeed;

	void Update()
	{
		//Bein generate upon press space
		if(Input.GetKeyDown(KeyCode.Space)) {Generate();}
	}

	public void Generate()
	{
		//Destroy the node group if it exist
		if(group != null) {Destroy(group);}
		//No longer has geneated
		hasGenerated = false;
		//Renew the node list
		nodes.Clear(); nodes = new List<NodeData>();
		//Add the first empty node than replicate at it
		nodes.Add(new NodeData()); Replicate(nodes[0]);
	}

	void Replicate(NodeData point)
	{
		//If this node point are the final node
		if(nodes.Count >= amount)
		{
			//Call the complete generate event
			completeGenerate?.Invoke(); 
			//Has generated to true
			hasGenerated = true;
			//Placing plain
			PlacePlain();
			//Auto generate again if needed
			if(automaticSpeed > 0) {Invoke("Generate", automaticSpeed);}
		}
		//The result too see what direction will be Replicate
		bool[] result = new bool[4];
		//Go through all the result need to calculated
		for (int c = 0; c < result.Length; c++)
		{
			//The chance of this cycle
			float chance = UnityEngine.Random.Range(0f, 100f);
			//@ Return the result base on rate of each direction
			if(c == 0 && rate.up    >= chance) {result[c] = true;}
			if(c == 1 && rate.down  >= chance) {result[c] = true;}
			if(c == 2 && rate.left  >= chance) {result[c] = true;}
			if(c == 3 && rate.right >= chance) {result[c] = true;}
		}
		///Replicate at the 0 [UP] direction if result are true
		if(result[0]) {NewDirection(point, new Vector2(00,+1), 0);}
		///Replicate at the 1 [DOWN] direction if result are true
		if(result[1]) {NewDirection(point, new Vector2(00,-1), 1);}
		///Replicate at the 2 [LEFT] direction if result are true
		if(result[2]) {NewDirection(point, new Vector2(-1,00), 2);}
		///Replicate at the 3 [RIGHT] direction if result are true
		if(result[3]) {NewDirection(point, new Vector2(+1,00), 3);}
	}

	void NewDirection(NodeData point, Vector2 directionVector, int directionIndex)
	{
		//Stop create more direction if has enough
		if(nodes.Count >= amount) {return;}
		//Create an new temp node
		NodeData newNode = new NodeData();
		//Set the temp's node coordiates with point coord increase with direction
		newNode.coordinates = point.coordinates + directionVector;
		//Stop if the temp node coordinates are already taken
		if(!IsEmpty(newNode.coordinates)) {return;}
		//Set the temp node's position by multiple coord with spacing
		newNode.position = new Vector2(newNode.coordinates.x * spacing, newNode.coordinates.y * spacing);
		//Add the temp node into list
		nodes.Add(newNode);
		//This point has replicate an node
		point.replicated++;
		//Save the coordinates of direction this point has replicate
		point.neighbours[directionIndex] = newNode.position;
		//Begin replicate more
		Replicate(newNode);
	}

	bool IsEmpty(Vector2 coord)
	{
		//Send false if when go through all the nodes there already an coodrinated sam as given
		for (int n = 0; n < nodes.Count; n++) {if(nodes[n].coordinates == coord) {return false;}}
		//There is empty node if no node has the same coordinates given
		return true;
	}

	int NodeIndex(NodeData need) 
	{
		//Go through all the list to return the index of need node data in list
		for (int n = 0; n < nodes.Count; n++) {if(nodes[n] == need) {return n+1;}}
		//Return -1 if don't found any
		return -1;
	}

	void PlacePlain()
	{
		//Create an new group object then edit it name
		group = new GameObject(); group.name = "Plain Group";
		//Go through all the node as create
		for (int n = 0; n < nodes.Count; n++)
		{
			//Create the plain at those node position
			GameObject nObj = Instantiate(plainPrefab, nodes[n].position, Quaternion.identity);
			//Add the node object as children of group
			nObj.transform.parent = group.transform;
		}
	}
}
