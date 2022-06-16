using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using System;
#pragma warning disable ///! FOR NOW

public class DiggerGeneration : MonoBehaviour
{
	public bool autoGenerate; public float autoDelay;
	[Header("- Settings -")]
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
	[Header("- Data List -")]
	public List<PlotData> plots = new List<PlotData>();
	public List<PlotData> miners = new List<PlotData>();
	public List<DraftData> drafts = new List<DraftData>();
	public List<FloorData> floors = new List<FloorData>();
	public List<BridgeData> bridges = new List<BridgeData>();
	public bool generating; 
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
		[Tooltip("The following will be change:\n- Spacing will take into account of floor size\n- Bridge length will be scale with spacing\n- Wall will be scale wih bridge and wall size\n- Value that got auto scale will now use to modify")]
		public bool autoScale;
		public Draft draft; [Serializable] public class Draft
		{
			public GameObject prefab;
			public float size;
			public Color digged, miner, stuck, bypassed, over;
			public enum Colors {digged, miner, stuck, bypassed, over};
			[HideInInspector] public GameObject grouper;
		}
		public Floor floor; [Serializable] public class Floor
		{
			public GameObject prefab;
			public float size;
			public Color color;
			[HideInInspector] public GameObject grouper;
		}
		public Bridge bridge; [Serializable] public class Bridge
		{
			public GameObject prefab;
			public float width, length;
			public Color color;
			[HideInInspector] public GameObject grouper;
		}
		public Wall wall; [Serializable] public class Wall
		{
			public GameObject prefab;
			public float thick, length;
			public Color color;
			[HideInInspector] public GameObject grouper;
		}
	}
	[Serializable] public class PlotData
	{
		public int index, digCount;
		public Vector2 coordinate, position;
		public List<int> availableDirection = new List<int>{0,1,2,3};
		public NeighborData[] neighbors = new NeighborData[4];
		[HideInInspector] public int bypassDirection = -1;
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

	void Update() 
	{
		//% DIG when pressed SPACE
		if(Input.GetKey(KeyCode.Space)) Dig();
		//% LOAD THIS SCENE when pressed R
		if(Input.GetKeyDown(KeyCode.R)) UnityEngine.SceneManagement.SceneManager.LoadScene(3, UnityEngine.SceneManagement.LoadSceneMode.Single);
	}

	public void Dig(bool overwrite = false)
	{
		//Don't dig if currently generating when no need to overwriten
		if(generating) {if(!overwrite) {return;}}
		//Renewing structure to dig
		RenewStructure();
		//Clear all the plots
		plots.Clear();
		//Are now generating
		generating = true;
		//Create an plot at 0,0 then set it position at start position
		CreatePlot(0, new Vector2(0,0), startPosition);
		//Begin digging at that first plot
		StartCoroutine(Digging(plots[0], plots[0]));
	}

	IEnumerator Digging(PlotData miner, PlotData digged)
	{
		//This room are now the current leader
		miners.Add(miner);
		//Draft miner and digged
		Drafting(miner, digged);
		//Wait for an frame
		yield return null;
		//Begin decide direction to dig at this miner
		DecideDirectionToDig(miner);
		//Remove this miner after try to dig
		miners.Remove(miner);
		//Begin check the digging progress if haven't dig enough plot
		if(plots.Count < amount) {CheckingDigProgress(miner);}
		//Dig are complete when there no miner left
		if(miners.Count <= 0) {CompleteDig();}
	}

	void TryToDig(PlotData miner, int dir, bool allow)
	{
		//? Get the next plot of miner at given direction
		GetNextPlot(miner, dir, out Vector2 dirVector, out Vector2 nextCoord, out PlotData nextPlot);

		//? Are able to dig in this direction
		//STOP dig and this direction are no longer available when next plot have exist
		if(nextPlot != null) {SetDirectionUnavailable(dir, miner); return;}
		//STOP dig if has reach max plot amount or not allow
		if(plots.Count >= amount || !allow) {return;}
		//STOP dig if miner has dig over the maximum allow
		if(miner.digCount >= digRequirement.maximum) {return;}

		//? Dig an new plot for miner at direction in direction vector with next corrdinate
		DigPlot(miner, dir, dirVector, nextCoord);
	}

	void DecideDirectionToDig(PlotData miner)
	{
		//Set the result of each direction
		bool[] result = RandomizingDirectionResult();
		//Create an temporary the list of available direction
		List<int> aDir = new List<int>(miner.availableDirection);
		/// DIG
		//Go through all 4 direction when there still available direction
		if(miner.availableDirection.Count > 0) for (int d = 0; d < 4; d++)
		{
			//Exit if out of temporary direction
			if(aDir.Count == 0) {break;}
			//Randomly get the available direction gonna use
			int use = UnityEngine.Random.Range(0, aDir.Count);
			//Try to dig at that available direction with it result
			TryToDig(miner, aDir[use], result[use]);
			//Remove this direction from temporary
			aDir.Remove(use);
		}
		/// BYPASS
		//If there are no available direction and this is the only miner
		else if(miners.Count <= 1)
		{
			//Will continuous run until miner has direction to bypass
			while (miner.bypassDirection == -1)
			{
				//Refresh direction result for bypass
				result = RandomizingDirectionResult();
				//Randomly get an choose an direction index
				int choosed = UnityEngine.Random.Range(0,4);
				//If the result in choosed direction are true then miner bypass in that direction
				if(result[choosed] == true) miner.bypassDirection = choosed;
			}
			//Change the draft color at miner index to stuck
			ChangeDraftColor(miner.index, Builder.Draft.Colors.stuck);
			//Try to bypass at miner in bypass direction has get
			TryToBypass(miner, miner.bypassDirection);
		}
	}

	void TryToBypass(PlotData bypasser, int direction)
	{
		//Get the next plot of bypasser in given direction to attempt that plot
		GetNextPlot(bypasser, direction, out Vector2 dirVector, out Vector2 nextCoord, out PlotData attempt);
		//If there still plot at the attempt
		if(attempt != null) 
		{
			//Try to bypass that attempt in the same direction
			TryToBypass(attempt, direction);
			//Change the draft color at attempt index to bypassed
			ChangeDraftColor(attempt.index, Builder.Draft.Colors.bypassed);
		}
		//If there no longer plot at attempt
		else
		{
			//Dig an new plot at the same direction in direction vector with next corrdinate
			DigPlot(bypasser, direction, dirVector, nextCoord);
		}
	}

	bool[] RandomizingDirectionResult()
	{
		//The result for each direction
		bool[] result = new bool[4];
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

	void GetNextPlot(PlotData plot,int dir,out Vector2 dirVector,out Vector2 nextCoord,out PlotData nextPlot)
	{
		//Get the vector of this current direction
		dirVector = DirectionIndexToVector(dir);
		//Get the next coordinate at given plot coord coordinate increase with direction vector
		nextCoord = plot.coordinate + dirVector;
		//Find the next plot at next coordinate
		nextPlot = FindPlotAtCoordinate(nextCoord);
	}

	void CheckingDigProgress(PlotData miner)
	{
		//Has this miner retry
		bool retry = false;
		//If there no miner left or this miner haven't dig the bare minimum 
		if(miners.Count == 0 || miner.digCount < digRequirement.minimum)
		{
			//Retry again at this miner
			StartCoroutine(Digging(miner, miner)); retry = true;
		}
		//If this miner are not bypasser, haven't dig anything and is not retrying
		if(!retry && miner.digCount == 0 && miner.bypassDirection == -1)
		{
			//Change the draft color at miner index to over
			ChangeDraftColor(miner.index, Builder.Draft.Colors.over);
		}
	}

	void SetDirectionUnavailable(int dir, PlotData miner) 
	{
		//Remove the requested direction from availability of miner if haven't
		if(miner.availableDirection.Contains(dir)) miner.availableDirection.Remove(dir);
	}

	void DigPlot(PlotData miner, int dir, Vector2 dirVector, Vector2 nextCoord)
	{
		//If this direction haven't got empty neighbor then create one
		if(miner.neighbors[dir] == null) miner.neighbors[dir] = new NeighborData();
		//Save the spacing and increase the spacing with floor size if using auto scale
		float spaced = builder.spacing; if(builder.autoScale) {spaced += MasterScaling("floor");}
		//Get the next position by increase miner position with direction got multiply with spaced
		Vector2 nextPos = miner.position + (dirVector * spaced);
		//Create an new digged plot with index of plot count at direction coordinate and next position
		PlotData newDig = CreatePlot(plots.Count, nextCoord, nextPos);
		//Counting this dig of miner
		miner.digCount++;
		//The neighbor in this direction of miner got dig by it
		miner.neighbors[dir].digByThis = true;
		//This direction of miner are no longer available
		SetDirectionUnavailable(dir, miner);
		///Begin dig again at that newly digged plot
		StartCoroutine(Digging(newDig, miner));
	}

	PlotData CreatePlot(int index, Vector2 coord, Vector2 pos)
	{
		//Create an new empty plot data
		PlotData newPlot = new PlotData();
		//@ Assign the new plot info as given
		newPlot.index = index; 
		newPlot.coordinate = coord; 
		newPlot.position = pos;
		//Add the new plot to list and return it
		plots.Add(newPlot); return newPlot;
	}

	void CompleteDig()
	{
		//No longer generating
		generating = false;
		//Call complete generation event
		generationCompleted?.Invoke();
	}
	
#region Converter
	Vector2 DirectionIndexToVector(int index)
	{
		//@ Return vector depend on index given from 0-3
		if(index == 0) {return Vector2.up;}
		if(index == 1) {return Vector2.down;}
		if(index == 2) {return Vector2.left;}
		if(index == 3) {return Vector2.right;}
		//Return zero vector if index given are not 0-3
		return Vector2.zero;
	}

	float MasterScaling(string structure)
	{
		//Save the master scale
		float ms = builder.masterScale;
		//@ Return the master scaled value of needed structure scaling
		switch(structure)
		{
			case "draft" : return builder.draft.size   * ms; break;
			case "floor" : return builder.floor.size   * ms; break;
			case "bridge": return builder.bridge.width * ms; break;
			case "wall"  : return builder.wall.thick   * ms; break;
		}
		//Send an error if there no structure that needed
		Debug.LogError("There no '"+structure+"' structure"); return -1;
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
		//Only need to create new draft if some plots haven't get draft
		if(drafts.Count >= plots.Count) {return;}
		//Setup an empty new draft
		DraftData newDraft = new DraftData();
		//Save the miner index to draft's plot index
		newDraft.plotIndex = miner.index;
		//Instantiate an draft prefab at miner position then save it to data
		newDraft.obj = Instantiate(builder.draft.prefab, miner.position, Quaternion.identity);
		//Set the draft object scale as the master scaled of the draft scale
		newDraft.obj.transform.localScale = new Vector2(MasterScaling("draft"), MasterScaling("draft"));
		//Group the draft parent
		newDraft.obj.transform.SetParent(builder.draft.grouper.transform);
		//Add plot index to the draft object name
		newDraft.obj.name = "Draft of " + newDraft.plotIndex;
		//Save the new draft object's sprite renderer to data
		newDraft.renderer = newDraft.obj.GetComponent<SpriteRenderer>();
		//Add new draft data to list
		drafts.Add(newDraft);
		//Change the draft color at miner index to miner color
		ChangeDraftColor(miner.index, Builder.Draft.Colors.miner);
		//Change the draft color at digged index to digged color
		ChangeDraftColor(digged.index, Builder.Draft.Colors.digged);
	}

	void ChangeDraftColor(int index, Builder.Draft.Colors color)
	{
		//Get the sprite render of draft at given index
		SpriteRenderer renderer = drafts[index].renderer;
		//@ Set that draft color color according to given string
		if(color == Builder.Draft.Colors.digged)   {renderer.color = builder.draft.digged;   return;}
		if(color == Builder.Draft.Colors.miner)    {renderer.color = builder.draft.miner;    return;}
		if(color == Builder.Draft.Colors.stuck)    {renderer.color = builder.draft.stuck;    return;}
		if(color == Builder.Draft.Colors.bypassed) {renderer.color = builder.draft.bypassed; return;}
		if(color == Builder.Draft.Colors.over)     {renderer.color = builder.draft.over;     return;}
	}
#endregion
}