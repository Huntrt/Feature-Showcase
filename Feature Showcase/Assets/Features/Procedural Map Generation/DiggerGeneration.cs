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
		[Tooltip("All structure size will multiply with this scale")] 
		public float masterScale;
		[Tooltip("Bridge and Wall length will auto scale with spacing and floor size \n (Length value will now use to modfy it)")]
		public bool autoScale;
		public Draft draft; [Serializable] public class Draft
		{
			public GameObject Prefab;
			public float Spacing, Size;
			public Color digged, miner, stuck, over;
		}
		public Floor floor; [Serializable] public class Floor
		{
			public GameObject Prefab;
			public float Spacing, Size;
			public Color Color;
		}
		public Bridge bridge; [Serializable] public class Bridge
		{
			public GameObject Prefab;
			public float Width, Length;
			public Color Color;
		}
		public Wall wall; [Serializable] public class Wall
		{
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
	void Update() {if(Input.GetKeyDown(KeyCode.Space)) {DirectionResult();}}

	public void Dig()
	{
		//Only dig when not generate
		if(areGenerating) {return;}
		//Are now generating and haven't generated
		areGenerating = true; hasGenerated = false;
		//Dig an plot at 0,0 then set it position as start position
		DigPlot(new Vector2(0,0), startPosition);
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
		bool[] result = new bool[4];
	}

	void DirectionResult(int forceDirection = -1)
	{
		//The result to decide which direction will be dig
		bool[] result = new bool[4];
		//Set the result of force direction to true if has any
		if(forceDirection != -1) {result[forceDirection] = true;}
		//Begin randomize result of other direction if has no force
		else
		{
			//List of available direction
			List<int> aDir = new List<int>{0,1,2,3};
			//Go through all the result need to randomize
			for (int r = 0; r < result.Length; r++)
			{
				//Randomize the chance for this direction
				float chance = UnityEngine.Random.Range(0f,100f);
				//Randomly get the available direction gonna use
				int use = UnityEngine.Random.Range(0, aDir.Count);
				//Use the direction then make it unavailable
				int d = aDir[use]; aDir.RemoveAt(use);
			}
		}
	}

	PlotData DigPlot(Vector2 coord, Vector2 pos)
	{
		//Create an new empty plot data
		PlotData newPlot = new PlotData();
		//Assign coordinate and position given
		newPlot.coordinate = coord; newPlot.position = pos;
		//Add the new plot to list and return it
		plots.Add(newPlot); return newPlot;
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
		//Add new draft data to list
		drafts.Add(newDraft);
	}
}