using System.Collections.Generic;
using ProceduralMapGeneration;
using System.Collections;
using UnityEngine;
using System;

public class DiggerGeneration : MonoBehaviour
{
	[Serializable] public class DiggerConfig
	{
		[Tooltip("The amount of plot will need to dig")]
		public int amount;
		[Tooltip("The position to begin dig")]
		public Vector2 startPosition;
		[Tooltip("The chance for miner to dig an new plot from 0 to 100")][Range(0,100)]
		public float digChance;
		[Tooltip(" Allow to has specific chance for each direction")]
		public DirectionalChance directionalChance; [Serializable] public class DirectionalChance 
		{
			public bool use; 
			[Range(0,100)] public float up,down,left,right;
		}

		[Tooltip("The constraint for the miner when dig")]
		public MiningRequirement digRequirement; [Serializable] public class MiningRequirement
		{
			[Range(1,4)] [Tooltip("The maximum amount of plot the miner allow to dig")]
			public int maximum = 1;
			[Range(0,4)][Tooltip("The minimum amount of plot the miner need to dig")]
			public int minimum;
		}
	}
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
	[Serializable] public class Builder
	{
		public float spacing; 
		[Tooltip("All structure size will multiply with this scale")] 
		public float masterScale;
		[Tooltip("The following will be change:\n- Bridge length will be scale with spacing\n- Wall will be scale wih bridge and wall size\n- Value that got auto scale will now use to modify")]
		public bool autoScale;
		public Draft draft; [Serializable] public class Draft
		{
			public bool enable, clearAfterDig;
			public GameObject prefab;
			public float size;
			public Color digged, miner, stuck, bypassed, over;
			public enum Colors {digged, miner, stuck, bypassed, over};
			[HideInInspector] public GameObject grouper;
		}
		public Floor floor; [Serializable] public class Floor
		{
			public bool enable;
			public GameObject prefab;
			public float size;
			public Color color;
			[HideInInspector] public GameObject grouper;
		}
		public Bridge bridge; [Serializable] public class Bridge
		{
			public bool enable;
			[Tooltip("The chance for plot to bridge more than 1 neighbor")][Range(0,100)]
			public float bridgeChance;
			public GameObject prefab;
			public float width, length;
			public Color color;
			[HideInInspector] public GameObject grouper;
		}
		public Wall wall; [Serializable] public class Wall
		{
			public bool enableBarrier;
			public bool enableRailing;
			[Tooltip("Will floor create an barrier or gate when there neighbor next to it?")]
			public bool barricadeNeighbor;
			public GameObject prefab;
			public float thick, length;
			public Color color;
			[HideInInspector] public GameObject grouper;
		}
	}
	
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

	#region Datas
	[Serializable] public class PlotData
	{
		public int index, digCount;
		public Vector2 coordinate, position;
		public NeighborData[] neighbors = new NeighborData[4];
		[HideInInspector] public int bypassDirection = -1;
		[HideInInspector] public List<int> availableDirection = new List<int>{0,1,2,3};
	}
	[Serializable] public class NeighborData
	{
		public int index = -1;
		public Vector2 coordinate, position;
		public bool digged;
		[Tooltip("Is this neighbor got dig by this plot?")]
		public bool digByThis; public bool hasBridge;
	}
	[Serializable] public class DraftData
	{
		public int plotIndex;
		public GameObject obj; public SpriteRenderer renderer;
	}
	[Serializable] public class FloorData
	{
		public int plotIndex;
		public GameObject obj; public SpriteRenderer renderer;
		public List<GameObject> walls = new List<GameObject>();
	}
	[Serializable] public class BridgeData
	{
		public int[] connectionIndex = new int[2];
		public int direction;
		public GameObject obj; public SpriteRenderer renderer;
		public GameObject[] walls = new GameObject[2];
	}
	#endregion
#endregion

#region Generation
	public void Dig(bool overwrite = false)
	{
		//Don't dig if currently generating when no need to overwriten
		if(generating) {if(!overwrite) {return;}}
		//Clearing all generation to dig
		ClearGeneration(true, true);
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
		DirectionalDigging(miner);
		//Remove this miner after try to dig
		miners.Remove(miner);
		//Begin check the digging progress if haven't dig enough plot
		if(plots.Count < amount) {CheckingDigProgress(miner);}
		//Dig are complete when there no miner left
		if(miners.Count <= 0) {CompleteDig();}
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

	void DirectionalDigging(PlotData miner)
	{
		//Get the result of each direction to dig
		bool[] result = RandomizingDigDirection();
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
		//If there are no available direction and this is the only miner left
		else if(miners.Count <= 1)
		{
			//Will continuous run until miner has direction to bypass
			while (miner.bypassDirection == -1)
			{
				//Refresh dig direction result for bypass
				result = RandomizingDigDirection();
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
	
	bool[] RandomizingDigDirection()
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

	void SetDirectionUnavailable(int dir, PlotData miner) 
	{
		//Remove the requested direction from availability of miner if haven't
		if(miner.availableDirection.Contains(dir)) miner.availableDirection.Remove(dir);
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
	
	void GetNextPlot(PlotData plot,int dir,out Vector2 dirVector,out Vector2 nextCoord,out PlotData nextPlot)
	{
		//Get the vector of this current direction
		dirVector = DirectionIndexToVector(dir);
		//Get the next coordinate at given plot coord coordinate increase with direction vector
		nextCoord = plot.coordinate + dirVector;
		//Find the next plot at next coordinate
		nextPlot = FindPlotAtCoordinate(nextCoord);
	}

	void DigPlot(PlotData miner, int dir, Vector2 dirVector, Vector2 nextCoord)
	{
		//If this direction haven't got empty neighbor then create one
		if(miner.neighbors[dir] == null) miner.neighbors[dir] = new NeighborData();
		//Get the next position by using miner with direction vector
		Vector2 nextPos = GetPositionInDirectionVector(miner, dirVector);
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

	void SetAllPlotsNeighbors()
	{
		//Go through all the plot to go through all 4 of it neighbor
		for (int p = 0; p < plots.Count; p++) for (int d = 0; d < 4; d++)
		{
			//Get the vector in this direction
			Vector2 dirVector = DirectionIndexToVector(d);
			//If this direction haven't got empty neighbor then create one
			if(plots[p].neighbors[d] == null) plots[p].neighbors[d] = new NeighborData();
			//Get the neighbor in this direction of this plot
			NeighborData neighbor = plots[p].neighbors[d];
			//Get coordinate of this neighbor by increase this plot coordinate with direction vector
			neighbor.coordinate = plots[p].coordinate + dirVector;
			//Get position of this neighbot by using this plot with direction vector
			neighbor.position = GetPositionInDirectionVector(plots[p], dirVector);
			//Find the plot at this neighbor coordinate
			PlotData finded = FindPlotAtCoordinate(neighbor.coordinate);
			//If the finded plot exist then set this neighbor as finded index and mark as digged
			if(finded != null) {neighbor.index = finded.index; neighbor.digged = true;}
		}
	}

	void CompleteDig()
	{
		//Set all 4 neighbors of all plot
		SetAllPlotsNeighbors();
		//No longer generating
		generating = false;
		//Call complete generation event
		generationCompleted?.Invoke();
		//Begin build structure after generated
		AssembleStructure();
		//Only clear draft after generated when needed
		if(builder.draft.clearAfterDig) ClearDraft(false);
	}

	public void ClearGeneration(bool renew, bool clearStructure)
	{
		//Dont clear if still generating
		if(generating) return;
		//Clear all the plots
		plots.Clear();
		//Clear all structure data list
		floors.Clear(); bridges.Clear();
		//Clear draft and renew them if needed
		ClearDraft(renew);
		//@ Destroy all the structure grouper if it already exist
		if(builder.floor.grouper != null) Destroy(builder.floor.grouper);
		if(builder.bridge.grouper != null) Destroy(builder.bridge.grouper);
		if(builder.wall.grouper != null) Destroy(builder.wall.grouper);
		//@ Create new grouper and name them if needed to refreash
		if(!renew) return;
		builder.floor.grouper = new GameObject(); builder.floor.grouper.name = "Floor Group";
		builder.bridge.grouper = new GameObject(); builder.bridge.grouper.name = "Bridge Group";
		builder.wall.grouper = new GameObject(); builder.wall.grouper.name = "Wall Group";
	}

	public void ClearDraft(bool renew)
	{
		//Clear draft data list
		drafts.Clear(); 
		//Destroy the draft grouper if it already exist
		if(builder.draft.grouper != null) Destroy(builder.draft.grouper);
		//Create an new draft grouper if needed to renew
		if(renew) {builder.draft.grouper = new GameObject(); builder.draft.grouper.name = "Dafts Group";}
	}
#endregion
	
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
			case "draft" : return builder.draft.size   * ms;
			case "floor" : return builder.floor.size   * ms;
			case "bridge": return builder.bridge.width * ms;
			case "wall"  : return builder.wall.thick   * ms;
		}
		//Send an error if there no structure that needed
		Debug.LogError("There no '"+structure+"' structure"); return -1;
	}

	Vector2 GetPositionInDirectionVector(PlotData plot, Vector2 dirVector)
	{
		//Save the spacing and increase the spacing with floor size if using auto scale
		float spaced = builder.spacing; if(builder.autoScale) {spaced += MasterScaling("floor");}
		//Return the plot position that got increase with direction vector multiply with spaced
		return plot.position + (dirVector * spaced);
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

	//? Assemble Structure -> Build X -> Format X -> Create Structure -> Listing X

	void AssembleStructure()
	{
		//Begin build bridge for all the plot if enable
		if(builder.bridge.enable) {for (int p = 0; p < plots.Count; p++) {BuildBridge(plots[p]);}}
		//Begin build floor for all the plot if enable
		if(builder.floor.enable) {for (int p = 0; p < plots.Count; p++) {BuildFloor(plots[p]);}}
	}

	#region Bridge
	void BuildBridge(PlotData plot)
	{
		//Get connection to use for bridging of plot given
		NeighborData[] connects = GetConnectionForBridge(plot);
		//Go through all 4 connection of plot given
		for (int c = 0; c < 4; c++)
		{
			//Skip if this current connection don't exist
			if(connects[c] == null) continue;
			//Format an bridge from plot to connection
			GameObject bridge = FormatBridge(plot, connects[c], c);
			//Get the connection from plot to current connect index
			int[] connection = new int[]{plot.index, connects[c].index};
			//List bridge with connection, bridge object then build it railing
			ListingBridge(connection,c, bridge, BuildRailing(bridge, c, connection));
		}
	}

	NeighborData[] GetConnectionForBridge(PlotData plot)
	{
		//Create 4 empty connection
		NeighborData[] connects = new NeighborData[4];
		//Go through all 4 of neighbor to connect
		for (int n = 0; n < 4; n++)
		{
			//Save the current neighbor
			NeighborData neighbor = plot.neighbors[n];
			//If this neighbor dig by given plot 
			if(neighbor.digByThis) 
			{
				//This connect will be given plot neighbor 
				connects[n] = neighbor; 
				//The neighbor of given plot has been bridge in this connection
				plot.neighbors[n].hasBridge = true;
				//Skip conection randomzie
				continue;
			}
			//? Randomize Bridging
			//See if the plot neighbor and itself are already connect
			bool alreadyConnect = false;
			//Go through all the bridge
			for (int b = 0; b < bridges.Count; b++)
			{
				//Get the connected index of current bridge
				int[] connected = bridges[b].connectionIndex;
				//How many connection are the same as connected
				int hasConnected = 0;
				//@ Count how many connection of current bridge match the index of neighbor or plot
				if(neighbor.index == connected[0] || plot.index == connected[0]) hasConnected++;
				if(neighbor.index == connected[1] || plot.index == connected[1]) hasConnected++;
				// Already connect when both connected match
				if(hasConnected == 2) alreadyConnect = true;
			}
			//The randomize result to bridge 
			float chance = UnityEngine.Random.Range(0f,100f);
			//If chance allow to dig when the neighbor has digged and not already connect
			if(builder.bridge.bridgeChance >= chance && neighbor.digged)
			{
				//This connect will be given plot neighbor if not already connect
				if(!alreadyConnect) connects[n] = neighbor;
				//The neighbor of given plot has been bridge in this connection
				plot.neighbors[n].hasBridge = true;
			}
		}
		//Return all connection has made
		return connects;
	}

	GameObject FormatBridge(PlotData plot, NeighborData connect, int dir)
	{
		//Get this plot and current connect position
		Vector2 plotP = plot.position; Vector2 conP = connect.position;
		//Get position at the middle point between the plot and it connection
		Vector2 pos = new Vector2(plotP.x + (conP.x - plotP.x)/2, plotP.y + (conP.y - plotP.y)/2);
		//Get the bridge length and increase with spacing if auto scale enable
		float length = builder.bridge.length; if(builder.autoScale) {length += builder.spacing;}
		//Bridge scaling X as length and Y as width
		Vector2 scale = new Vector2(length, MasterScaling("bridge"));
		//Rotation default are 0 for horizontal and 90 if vertical
		float rot = 0; if(dir <= 1) rot = 90;
		//Set name for this bridge (X > Y Bridge)
		string name = plot.index + " > " + connect.index + " Bridge";
		//Return the newly create an bridge structure using all the data above
		return CreateStructure(name, "bridge", pos, scale, rot);
	}

	GameObject[] BuildRailing(GameObject bridge, int direction, int[] index)
	{
		//Return null if dont need to build railing
		if(!builder.wall.enableRailing) {return null;}
		//Create 2 null gameobject to store railing
		GameObject[] walls = new GameObject[2]{null, null};
		//Get the bridge position
		Vector2 bridgePos = bridge.transform.position;
		//Create 2 vector for wall position that using bridge position
		Vector2[] wallsPos = new Vector2[2]{bridgePos, bridgePos}; 
		//Create 2 empty vector for wall scale
		Vector2[] wallScale = new Vector2[2];
		//Get the thick of wall by master scaling
		float wallThick = MasterScaling("wall");
		//Get how far to place wall by increase half of bridge width with wall thick
		float railingSpaced = (MasterScaling("bridge")/2) + (wallThick/2);
		//Get railing as length as custom wall length
		float railingLength = builder.wall.length;
		//Increase railing with spacing if enable auto scale
		if(builder.autoScale) {railingLength += builder.spacing;}
		//@ Set the wall position and scale in certain axis base on it horizontal or vertical
		//The bridge are vertical
		if(direction <= 1)
		{
			wallsPos[0].x += railingSpaced;
			wallsPos[1].x -= railingSpaced;
			wallScale[0] = new Vector3(wallThick, railingLength);
			wallScale[1] = new Vector3(wallThick, railingLength);
		}
		//The bridge are horizontal
		else
		{
			wallsPos[0].y += railingSpaced;
			wallsPos[1].y -= railingSpaced;
			wallScale[0] = new Vector3(railingLength, wallThick);
			wallScale[1] = new Vector3(railingLength, wallThick);
		}
		//Go through 2 railing
		for (int r = 0; r < 2; r++)
		{
			//Set the name for railing (X > Y Raling [R])
			string naming = index[0] + " > " + index[1] + " Railing ["+r+"]";
			//Create an wall at position and scale has get
			walls[r] = CreateStructure(naming, "wall", wallsPos[r], wallScale[r]);
		}
		//Return railing has build
		return walls;
	}
	#endregion

	#region Floor
	void BuildFloor(PlotData plot)
	{
		//Get the master scaling of floor
		Vector2 scale = new Vector2(MasterScaling("floor"), MasterScaling("floor"));
		//Set the name for floor (X Floor)
		string naming = plot.index + " Floor";
		//Createan floor with name, scale has got and at given plot position
		GameObject floor = CreateStructure(naming, "floor", plot.position, scale);
		//List the newly create floor with given plot index then build barrier at it
		ListingFloor(plot.index, floor, BuildBarrier(plot));
	}

	List<GameObject> BuildBarrier(PlotData plot)
	{
		//Return null if dont need to build barrier
		if(!builder.wall.enableBarrier) {return null;}
		//Empty list of gameobject for barrier
		List<GameObject> barriers = new List<GameObject>();
		//Go through all the neighbor of given plot
		for (int n = 0; n < 4; n++)
		{
			//Get this plot neighbor
			NeighborData neighbor = plot.neighbors[n];
			//If neighbor dont has bridge
			if(!neighbor.hasBridge)
			{
				//Format an barrier for given plot in this neighbor then list them
				GameObject barrier = FormatBarrier(plot, n);
				//Add barrier to it list data if it exist
				if(barrier != null) barriers.Add(barrier);
			}
			//If neighbor has bridge
			else
			{
				//Skip if not need to barricade gate in this neighbor
				if(!builder.wall.barricadeNeighbor) continue;
				//Get gate length by half deceased floor scale and bridge that got increase with wall thick
				float gateLength = ((MasterScaling("floor")-MasterScaling("bridge"))/2) + MasterScaling("wall");
				//For each side of this neighbor
				for (int s = 0; s < 2; s++)
				{
					//Format an barrier for given plot in this direction with gate length
					GameObject barrier = FormatBarrier(plot, n, gateLength);
					//Format the created barrier for given plot in this neighbor and side to be an gate
					barriers.Add(FormatGate(barrier, plot, n, s));
				}
			}
		}
		//Return all the barrier has created
		return barriers;
	}

	GameObject FormatBarrier(PlotData plot, int direction, float gateLength = -1)
	{
		//Don't barricade neighbor in this direction if it has dig when no need to barricade
		if(plot.neighbors[direction].digged && !builder.wall.barricadeNeighbor) return null;
		Vector2 position = plot.position;
		Vector2 scale = new Vector2();
		float barrierLength = 0;
		float wallThick = MasterScaling("wall");
		float floorScale = MasterScaling("floor");
		//If enable auto scale
		if(builder.autoScale)
		{
			//Set length for barrier by incease floor scale with double wall thick 
			barrierLength = floorScale + (wallThick*2);
			//? Fixing wall poking into floor when not barricade neighbour
			if(!builder.wall.barricadeNeighbor) for (int n = 0; n < 4; n++)
			{
				//Skip neighbor in this direction if currently build barrier for it or has dig
				if(n == direction || !plot.neighbors[n].digged) continue;
				//If this barrier direction are VERTICAL
				if(direction <= 1)
				{
					//If direction are LEFT cut off barrier length then move RIGHT
					if(n == 2) {position.x += wallThick/2; barrierLength -= wallThick;}
					//If direction are RIGHT cut off barrier length then moveLEFT
					if(n == 3) {position.x -= wallThick/2; barrierLength -= wallThick;}
				}
				//If this barrier direction are HORIZONTAL
				if(direction >= 2)
				{
					//If direction are UP cut off barrier length then move DOWN
					if(n == 0) {position.y -= wallThick/2; barrierLength -= wallThick;}
					//If direction are DOWN cut off barrier length then movE UP
					if(n == 1) {position.y += wallThick/2; barrierLength -= wallThick;}
				}
			}
		}
		//Overwrite barrier length to given gate length if given not -1
		if(gateLength >= 0) {barrierLength = gateLength;}
		//Modify barrier length will custom wall length
		barrierLength += builder.wall.length;
		//Get barrier spacing by use half of combined floor scale and wall thick
		float barrierSpaced = (floorScale + wallThick)/2;
		//What is given direction
		switch(direction)
		{
			//@ Set the axis to spacing and scale depend on given direction
			case 0: position.y += barrierSpaced; scale = new Vector2(barrierLength, wallThick); break;
			case 1: position.y -= barrierSpaced; scale = new Vector2(barrierLength, wallThick); break;
			case 2: position.x -= barrierSpaced; scale = new Vector2(wallThick, barrierLength); break;
			case 3: position.x += barrierSpaced; scale = new Vector2(wallThick, barrierLength); break;
		}
		//Set the name for barrier (X Barrier [D])
		string naming = plot.index + " Barrier " + "["+direction+"]";
		//Return the created wall that use getted name, position and scale as barrier
		return CreateStructure(naming, "wall", position, scale);
	}

	GameObject FormatGate(GameObject gate, PlotData plot, int direction, int side)
	{
		float floorScale = MasterScaling("floor"); 
		float wallThick = MasterScaling("wall"); 
		float bridgeWidth = MasterScaling("bridge");
		//Overwrite the name or gate formated (X Gate [D] [S])
		gate.name = plot.index + " Gate " + "["+direction+"] " +"["+side+"]";
		//Align by quater of combined floor scale with bridge width then increase with half of wall thick
		float align = (floorScale + bridgeWidth)/4 + wallThick/2;
		//Create an empty vector for align position
		Vector3 alignPosition = Vector3.zero;
		//If direction in vertical then align the X axis base on it given side
		if(direction <= 1) {if(side == 0) {alignPosition.x = align;} if(side == 1) {alignPosition.x = -align;}}
		//If direction in horizonal then align the Y axis base on it given side
		if(direction >= 2) {if(side == 0) {alignPosition.y = align;} if(side == 1) {alignPosition.y = -align;}}
		//Align gate position
		gate.transform.position += alignPosition;
		//Return the gate has align
		return gate;
	}
	#endregion


	GameObject CreateStructure(string naming,string structure,Vector2 position,Vector2 scale,float rotation=0)
	{
		//An empty prefab and grouper to use
		GameObject prefab = null; GameObject grouper = null; 
		//@ Set the structure prefab and grouper as the name given
		switch(structure)
		{
			case "floor" : prefab = builder.floor.prefab ; grouper = builder.floor.grouper ; break;
			case "bridge": prefab = builder.bridge.prefab; grouper = builder.bridge.grouper; break;
			case "wall"  : prefab = builder.wall.prefab  ; grouper = builder.wall.grouper  ; break;
		}
		//Send an error if there no structure that needed
		if(prefab == null) Debug.LogError("There no '"+structure+"' structure to create");
		//Instantiate an structure prefab in given position and with given rotation
		GameObject builded = Instantiate(prefab, position, Quaternion.Euler(0,0,rotation));
		//Set the builded structure scale as given
		builded.transform.localScale = scale;
		//Group the builded structure
		builded.transform.SetParent(grouper.transform);
		//Add set structure name
		builded.name = naming;
		//Return the builded structure
		return builded;
	}

	void ListingFloor(int index, GameObject obj, List<GameObject> wall)
	{
		//@ Create empty floor data then assign given data to it
		FloorData data = new FloorData();
		data.plotIndex = index; 
		data.obj = obj;
		data.renderer = obj.GetComponent<SpriteRenderer>(); 
		data.walls = wall;

		//@ Color the floor and it walls
		data.renderer.color = builder.floor.color;
		if(wall != null) for (int w = 0; w < wall.Count; w++)
		{wall[w].GetComponent<SpriteRenderer>().color = builder.wall.color;}
		//Add floor data to list
		floors.Add(data);
	}

	void ListingBridge(int[] connection, int direction, GameObject obj, GameObject[] wall)
	{
		//@ Create empty bridge data then assign given data to it
		BridgeData data = new BridgeData();
		data.connectionIndex = connection; 
		data.direction = direction; 
		data.obj = obj;
		data.renderer = obj.GetComponent<SpriteRenderer>(); data.walls = wall;

		//@ Color the bridge and it walls
		data.renderer.color = builder.bridge.color;
		if(wall != null) for (int w = 0; w < wall.Length; w++)
		{wall[w].GetComponent<SpriteRenderer>().color = builder.wall.color;}
		//Add bridge data to list
		bridges.Add(data);
	}

	void Drafting(PlotData miner, PlotData digged)
	{
		//Only need to create new draft if plots haven't get enough draft and only when need to draft
		if(drafts.Count >= plots.Count || !builder.draft.enable) return;
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
		//Only change draft color if drafting enable
		if(!builder.draft.enable) return;
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