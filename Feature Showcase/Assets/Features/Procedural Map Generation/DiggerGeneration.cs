using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using System;
#pragma warning disable ///! FOR NOW

public class DiggerGeneration : MonoBehaviour
{
	public bool autoGenerate; public float autoDelay;
	[Header("> Setting")]
	[Tooltip("The amount of plot will need to dig")]
	public int amount;
	[Tooltip("The position to begin dig")]
	public Vector2 startPosition;
	[Tooltip("The chance for miner to dig an new plot from 0 to 100")][Range(0,100)]
	public float digChance;
	[Tooltip(" Allow to has specific chance for each direction")]
	public DirectionalChance directionalChance;
	[Tooltip("The constraint for the miner when dig")]
	public DigRequirement digRequirement;
	public Builder builder;
	[Header("> Data List")]
	public List<PlotData> plots = new List<PlotData>();
	public List<PlotData> miners = new List<PlotData>();
	public List<DraftData> drafts = new List<DraftData>();
	public List<FloorData> floors = new List<FloorData>();
	public List<BridgeData> bridges = new List<BridgeData>();
	public bool hasGenerated, areGenerating; 
	public event Action generationCompleted, structureBuilded;
	
#region Classes
	[Serializable] public class DirectionalChance 
	{
		public bool use; [Range(0,100)] 
		public float up,down,left,right;
	}
	[Serializable] public class DigRequirement
	{
		[Range(1,4)] [Tooltip("The maximum amount of plot the miner allow to dig")]
		public int maximum = 1;
		[Range(0,4)][Tooltip("The minimum amount of plot the miner need to dig")]
		public int minimum;
	}
	[Serializable] public class Builder
	{
		public float spacing;
		[Tooltip("All structure size will multiply with this scale")] 
		public float masterScale;
		[Tooltip("Bridge and Wall length will auto scale with spacing and floor size \n (Length value will now use to modfy it)")]
		public bool autoScale;
		public Draft draft; [Serializable] public class Draft
		{
			public GameObject grouper;
			public GameObject Prefab;
			public float Size;
			public Color digged, miner, stuck, over;
		}
		public Floor floor; [Serializable] public class Floor
		{
			public GameObject grouper;
			public GameObject Prefab;
			public float Size;
			public Color Color;
		}
		public Bridge bridge; [Serializable] public class Bridge
		{
			public GameObject grouper;
			public GameObject Prefab;
			public float Width, Length;
			public Color Color;
		}
		public Wall wall; [Serializable] public class Wall
		{
			public GameObject grouper;
			public GameObject Prefab;
			public float Thick, Length;
			public Color Color;
		}
	}
	[Serializable] public class PlotData
	{
		public int index;
		public Vector2 coordinate, position;
		public NeighborData[] neighbors = new NeighborData[4];
	}
	[Serializable] public class NeighborData
	{
		public Vector2 coordinate, position;
		public bool hasDig;
		[Tooltip("Is this neighbor got dig by this plot?")]
		public bool digByThis;
	}
	[Serializable] public class DraftData
	{
		public int plotIndex;
		public GameObject obj;
		public SpriteRenderer renderer;
	}
	[Serializable] public class FloorData
	{
		public int plotIndex;
		public GameObject obj;
		public List<GameObject> walls = new List<GameObject>();
	}
	[Serializable] public class BridgeData
	{
		public int[] connection = new int[2];
		public int direction;
		public GameObject obj;
		public List<GameObject> walls = new List<GameObject>();
	}
#endregion

	//Begin dig when pressed space
	void Update() {if(Input.GetKeyDown(KeyCode.Space)) Dig();}

	public void Dig()
	{
		//Begin renew structure to dig
		RenewStructure();
		//Clear all the plots
		plots.Clear();
		//Only dig when not generate
		if(areGenerating) {return;}
		//Are now generating and haven't generated
		areGenerating = true; hasGenerated = false;
		//Create an plot at 0,0 then set it position at start position
		CreatePlot(new Vector2(0,0), startPosition);
		//Begin digging at the first plot
		StartCoroutine(Digging(plots[0], plots[0]));
	}

	IEnumerator Digging(PlotData miner, PlotData digged, int forceDirection = -1)
	{
		//This room are now the current leader
		miners.Add(miner);
		//Draft miner and digged
		Drafting(miner, digged);
		//Wait for an frame
		yield return null;
		//Set the result of each direction
		bool[] result = DirectionResult(forceDirection);
		//List of available direction
		List<int> aDir = new List<int>{0,1,2,3};
		//Go through all 4 direction
		for (int d = 0; d < 4; d++)
		{
			//Randomly get the available direction gonna use
			int use = UnityEngine.Random.Range(0, aDir.Count);
			//Dig at that available direction with it result
			DigAtDirection(miner, aDir[use], result[use]);
			//This used direction are no longer available
			aDir.RemoveAt(use);
		}
		//Remove this miner after finish dig
		miners.Remove(miner);
		//Dig are complete when there no miner left
		if(miners.Count <= 0) {CompleteDig();}
	}

	void DigAtDirection(PlotData miner, int dir, bool allow)
	{
		//Don't dig if has reach max plot amount or not allow
		if(plots.Count >= amount || !allow) return;
		//Get the vector of this current direction
		Vector2 dirVector = IndexToDirection(dir);
		//Get the coordinate in this direction
		Vector2 dirCoord = miner.coordinate + dirVector;
		//Find the next plot at direction coordinate
		PlotData nextPlot = FindPlotAtCoordinate(dirCoord);
		//If there is next plot
		if(nextPlot != null)
		{
			//Don't dig
			return;
		}
		//Create an new digging plot
		PlotData dig = new PlotData();
		//The dig index are current room count
		dig.index = plots.Count;
		//Set dig coordinate at direction coordinates
		dig.coordinate = dirCoord;
		//Set dig position at miner position increase with itself at direction
		dig.position = dig.coordinate * builder.spacing;
		//Add the digged plot to list
		plots.Add(dig);
		//Begin dig again at this plot
		StartCoroutine(Digging(dig, miner));
	}

	bool[] DirectionResult(int forceDirection = -1)
	{
		//The result for each direction
		bool[] result = new bool[4];
		//Set the result of force direction to true if has any then return immediately
		if(forceDirection != -1) {result[forceDirection] = true; return result;}
		//Go through all the result need to randomize
		for (int r = 0; r < result.Length; r++)
		{
			//Randomize the chance to decide for this direction
			float decide = UnityEngine.Random.Range(0f,100f);
			//If each directional has it own change
			if(directionalChance.use)
			{
				//@ Set the result base on comparing chance of each direction 
				if(r == 0 && directionalChance.up    >= decide) {result[r] = true;}
				if(r == 1 && directionalChance.down  >= decide) {result[r] = true;}
				if(r == 2 && directionalChance.left  >= decide) {result[r] = true;}
				if(r == 3 && directionalChance.right >= decide) {result[r] = true;}
			}
			//If all direction use an same chance
			else
			{
				//This direction result are true if it chance are higher than decide
				if(digChance >= decide) {result[r] = true;}
			}
		}
		return result;
	}

	PlotData CreatePlot(Vector2 coord, Vector2 pos)
	{
		//Create an new empty plot data
		PlotData newPlot = new PlotData();
		//Assign coordinate and position given
		newPlot.coordinate = coord; newPlot.position = pos;
		//Add the new plot to list and return it
		plots.Add(newPlot); return newPlot;
	}

	void CompleteDig()
	{
		//Has generated and no longer generating
		hasGenerated = true; areGenerating = false;
		//Call complete generation event
		generationCompleted?.Invoke();
	}
	
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
#endregion

#region Finder
	public PlotData FindPlotAtIndex(int index)
	{
		//Go through all the plot to return the plot at the same index given
		for (int p = 0; p < plots.Count; p++) {if(p == index) return plots[p];}
		return null;
	}

	public PlotData FindPlotAtCoordinate(Vector2 coord)
	{
		//Go through all the plot to return the plot at the same index given
		for (int p = 0; p < plots.Count; p++) {if(plots[p].coordinate == coord) return plots[p];}
		return null;
	}
	public PlotData FindPlotAtPosition(Vector2 pos)
	{
		//Go through all the plot to return the plot has the same position given
		for (int p = 0; p < plots.Count; p++) {if(plots[p].position == pos) return plots[p];}
		return null;
	}
#endregion

#region Builder
	void RenewStructure()
	{
		//@ Destroy all the structure grouper
		if(builder.draft.grouper != null) Destroy(builder.draft.grouper);
		if(builder.floor.grouper != null) Destroy(builder.floor.grouper);
		if(builder.bridge.grouper != null) Destroy(builder.bridge.grouper);
		if(builder.wall.grouper != null) Destroy(builder.wall.grouper);
		//@ Create new grouper and name them
		builder.draft.grouper = new GameObject(); builder.draft.grouper.name = "Dafts Group";
		builder.floor.grouper = new GameObject(); builder.floor.grouper.name = "Floor Group";
		builder.bridge.grouper = new GameObject(); builder.bridge.grouper.name = "Bridge Group";
		builder.wall.grouper = new GameObject(); builder.wall.grouper.name = "Wall Group";
		//@ Clear all structure list
		drafts.Clear(); floors.Clear(); bridges.Clear();
	}

	void Drafting(PlotData miner, PlotData digged)
	{
		//Setup an empty new draft
		DraftData newDraft = new DraftData();
		//Save the miner index to draft's plot index
		newDraft.plotIndex = miner.index;
		//Instantiate an draft prefab at miner position then save it to data
		newDraft.obj = Instantiate(builder.draft.Prefab, miner.position, Quaternion.identity);
		//Save the new draft object's sprite renderer to data
		newDraft.renderer = newDraft.obj.GetComponent<SpriteRenderer>();
		//Multiply draft local scale with master scale
		newDraft.obj.transform.localScale *= builder.masterScale;
		//Group the draft parent
		newDraft.obj.transform.SetParent(builder.draft.grouper.transform);
		//Add new draft data to list
		drafts.Add(newDraft);
	}
#endregion
}