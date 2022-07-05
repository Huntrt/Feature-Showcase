using System.Collections.Generic;
using ProceduralMapGeneration;
using System.Collections;
using UnityEngine;
using System.Linq;
using System;

namespace ProceduralMapGeneration.Digger
{
public class DiggerAlgorithm : MonoBehaviour
{

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

	/// Begin recursive dig with config class given
	public void RecursiveDig(DiggerConfig config, bool overwrite = false)
	{
		//Stop if already dig except when need to overwriten
		if(config.isDigging) {if(!overwrite) {return;}}
		//Clear all the dig in config given
		config.dugs.Clear();
		//Are now digging
		config.isDigging = true;
		//Dig at 0,0 then set it position at start position
		DigAtLocation(config, new Vector2(0,0), config.startPosition);
		//Begin digging at that first plot of given config
		StartCoroutine(Digging(config, config.dugs[0], config.dugs[0]));
	}

	IEnumerator Digging(DiggerConfig config, DigPlot miner, DigPlot digged)
	{
		//This room are now the current leader
		config.miners.Add(miner);
		//Draft miner and digged
		//! Drafting(miner, digged);
		//Wait for an frame
		yield return null;
		//Begin decide direction to dig at this miner
		DirectionalDigging(config, miner);
		//Remove this miner after dig
		config.miners.Remove(miner);
		//Begin check the digging progress if haven't dig enough plot
		if(config.dugs.Count < config.amount) {CheckingDigProgress(config, miner);}
		//Dig are complete when there no miner left
		if(config.miners.Count <= 0) {CompleteDig(config);}
	}

	//? Try to bypassed of dig in 4 direction
	void DirectionalDigging(DiggerConfig config, DigPlot miner)
	{
		/// DIG
		//Go through all 4 direction when there still available direction
		if(miner.availableDirection.Count > 0) for (int d = 0; d < 4; d++)
		{
			//Create an temporary replicate of miner's available direction
			List<int> aDir = new List<int>(miner.availableDirection);
			//Exit loop out of temporary direction
			if(aDir.Count == 0) {break;}
			//Get the result of each direction to dig
			bool[] result = RandomizingDigDirection(config);
			//Randomly get the available direction gonna use
			int use = UnityEngine.Random.Range(0, aDir.Count);
			//Try to dig at that available direction with it result
			TryToDig(config, miner, aDir[use], result[use]);
			//Remove this direction from temporary
			aDir.Remove(use);
		}
		/// BYPASS
		//If there are no available direction and this is the only miner left
		else if(config.miners.Count <= 1)
		{
			//Will continuous run until miner has direction to bypass
			while (miner.bypassedDirection == -1)
			{
				//Get the result of each direction to bypassed
				bool[] result = RandomizingDigDirection(config);
				//Randomly get an choosed an direction to bypass
				int choosed = UnityEngine.Random.Range(0,4);
				//If the result in choosed direction are true then miner start bypass in that direction
				if(result[choosed] == true) miner.bypassedDirection = choosed;
			}
			//Change the draft color at miner index to stuck
			//! ChangeDraftColor(miner.index, Builder.Draft.Colors.stuck);
			//Try to bypass at miner in bypass direction has get
			TryToBypass(config, miner, miner.bypassedDirection);
		}
	}
	
	//? Randomly allow any direction to dig or bypass
	bool[] RandomizingDigDirection(DiggerConfig config)
	{
		//The result for each direction
		bool[] result = new bool[4];
		//Go through all the result need to randomize
		for (int r = 0; r < result.Length; r++)
		{
			//Randomize the chance to decide for this direction
			float decide = UnityEngine.Random.Range(0f,100f);
			//If each directional has it own change
			if(config.directionalChance.use)
			{
				//@ Set the result base on comparing chance of each direction 
				if(r == 0 && config.directionalChance.up    >= decide) {result[r] = true;}
				if(r == 1 && config.directionalChance.down  >= decide) {result[r] = true;}
				if(r == 2 && config.directionalChance.left  >= decide) {result[r] = true;}
				if(r == 3 && config.directionalChance.right >= decide) {result[r] = true;}
			}
			//If all direction use an same chance
			else
			{
				//This direction result are true if it chance are higher than decide
				if(config.digChance >= decide) {result[r] = true;}
			}
		}
		return result;
	}

	void TryToDig(DiggerConfig config, DigPlot miner, int dir, bool allow)
	{
		//? Get the next dug of miner at given direction
		GetNextDug(config, miner, dir, out Vector2 dirVector, out Vector2 nextCoord, out DigPlot nextDig);

		//? Are able to dig in this direction
		//STOP dig and this direction are no longer available when next dug have exist
		if(nextDig != null) {SetDirectionUnavailable(dir, miner); return;}
		//STOP dig if has reach max dug amount or not allow
		if(config.dugs.Count >= config.amount || !allow) {return;}
		//STOP dig if miner has dig over the maximum allow
		if(miner.digCount >= config.digRequirement.maximum) {return;}

		//? Dig for miner at direction in direction vector with next corrdinate
		DigPlot(config, miner, dir, dirVector, nextCoord);
	}

	void TryToBypass(DiggerConfig config, DigPlot bypasser, int dir)
	{
		//Get the next dug of bypasser in given direction to attempt that dug
		GetNextDug(config, bypasser, dir, out Vector2 dirVector, out Vector2 nextCoord, out DigPlot attempt);
		//If there still dug at the attempt
		if(attempt != null) 
		{
			//Try to bypass at that attempt in the same direction
			TryToBypass(config, attempt, dir);
			//Change the draft color at attempt index to bypassed
			//! ChangeDraftColor(attempt.index, Builder.Draft.Colors.bypassed);
		}
		//If there no longer dug at attempt
		else
		{
			//Dig an new dug at the same direction of that empty attempt
			DigPlot(config, bypasser, dir, dirVector, nextCoord);
		}
	}
	
	//? Get the info of the dug at given direction of given dug
	void GetNextDug(DiggerConfig config,DigPlot dug,int dir,out Vector2 dirV,out Vector2 nextCoord,out DigPlot nextPlot)
	{
		//Get the vector of this current direction
		dirV = DirectionIndexToVector(dir);
		//Get the next coordinate at given dug coord coordinate increase with direction vector
		nextCoord = dug.coordinate + dirV;
		//Find the next dug at next coordinate
		nextPlot = GetDugAtCoordinate(config.dugs, nextCoord);
	}

	void DigPlot(DiggerConfig config, DigPlot miner, int dir, Vector2 dirVector, Vector2 nextCoord)
	{
		//If this direction haven't got empty neighbor then create one
		if(miner.neighbors[dir] == null) miner.neighbors[dir] = new DigPlot.Neighbor();
		//Get the next position by using miner with direction vector
		Vector2 nextPos = GetPositionInDirection(config, miner, dirVector);
		//Create an new digged dug with index of dug count at direction coordinate and next position
		DigPlot newDig = DigAtLocation(config, nextCoord, nextPos);
		//Counting this dig of miner
		miner.digCount++;
		//The neighbor in this direction of miner got dig by it
		miner.neighbors[dir].digbyThis = true;
		//This direction of miner are no longer available
		SetDirectionUnavailable(dir, miner);
		///Begin dig again at that newly digged dug
		StartCoroutine(Digging(config, newDig, miner));
	}	

	void CheckingDigProgress(DiggerConfig config, DigPlot miner)
	{
		//Has this miner retry
		bool retry = false;
		//If there no miner left or this miner haven't dig the bare minimum 
		if(config.miners.Count == 0 || miner.digCount < config.digRequirement.minimum)
		{
			//Retry again at this miner
			StartCoroutine(Digging(config, miner, miner)); retry = true;
		}
		//If this miner are not bypasser, haven't dig anything and is not retrying
		if(!retry && miner.digCount == 0 && miner.bypassedDirection == -1)
		{
			//Change the draft color at miner index to over
			//! ChangeDraftColor(miner.index, Builder.Draft.Colors.over);
		}
	}

	//? Dig an new plot with given position and coordinates
	DigPlot DigAtLocation(DiggerConfig config, Vector2 coord, Vector2 pos)
	{
		//Create an new empty dig
		DigPlot newDig = new DigPlot();
		//@ Assign the new dig coordinate and position as given
		newDig.coordinate = coord; 
		newDig.position = pos;
		//Add the new dig to config list then return it
		config.dugs.Add(newDig); return newDig;
	}

	void SetDirectionUnavailable(int dir, DigPlot miner) 
	{
		//Remove the requested direction from availability of miner if haven't
		if(miner.availableDirection.Contains(dir)) miner.availableDirection.Remove(dir);
	}

	void SetAllDugNeighbors(DiggerConfig config)
	{
		//Go through all the dug to go through all 4 of it neighbor
		for (int d = 0; d < config.dugs.Count; d++) for (int n = 0; n < 4; n++)
		{
			//Get the vector in this direction
			Vector2 dirVector = DirectionIndexToVector(n);
			//If this direction haven't got empty neighbor then create one
			if(config.dugs[d].neighbors[n] == null) config.dugs[d].neighbors[n] = new DigPlot.Neighbor();
			//Get the neighbor in this direction of this dug
			DigPlot.Neighbor neighbor = config.dugs[d].neighbors[n];
			//Get coordinate of this neighbor by increase this dug coordinate with direction vector
			neighbor.coordinate = config.dugs[d].coordinate + dirVector;
			//Get position of this neighbor by apply this dug with direction vector
			neighbor.position = GetPositionInDirection(config, config.dugs[d], dirVector);
			//If the finded dug not empty then this neighbor no longer empty
			if(!GetDugAtCoordinate(config.dugs, neighbor.coordinate).empty) {neighbor.empty = false;}
		}
	}

	void CompleteDig(DiggerConfig config)
	{
		//Set all 4 neighbors of all dug
		SetAllDugNeighbors(config);
		//No longer digging
		config.isDigging = false;
		//Call complete dig
		config.digCompleted.Invoke();
		//Begin build structure after generated
		//! AssembleStructure();
		//Only clear draft after generated when needed
		//! if(builder.draft.clearAfterDig) ClearDraft(false);
	}

	// public void ClearGeneration(bool renew, bool clearStructure)
	// {
	// 	//Dont clear if still generating
	// 	if(generating) return;
	// 	//Clear all the plots
	// 	plots.Clear();
	// 	//Clear all structure data list
	// 	floors.Clear(); bridges.Clear();
	// 	//Clear draft and renew them if needed
	// 	ClearDraft(renew);
	// 	//@ Destroy all the structure grouper if it already exist
	// 	if(builder.floor.grouper != null) Destroy(builder.floor.grouper);
	// 	if(builder.bridge.grouper != null) Destroy(builder.bridge.grouper);
	// 	if(builder.wall.grouper != null) Destroy(builder.wall.grouper);
	// 	//@ Create new grouper and name them if needed to refreash
	// 	if(!renew) return;
	// 	builder.floor.grouper = new GameObject(); builder.floor.grouper.name = "Floor Group";
	// 	builder.bridge.grouper = new GameObject(); builder.bridge.grouper.name = "Bridge Group";
	// 	builder.wall.grouper = new GameObject(); builder.wall.grouper.name = "Wall Group";
	// }

	// public void ClearDraft(bool renew)
	// {
	// 	//Clear draft data list
	// 	//! drafts.Clear(); 
	// 	//Destroy the draft grouper if it already exist
	// 	if(builder.draft.grouper != null) Destroy(builder.draft.grouper);
	// 	//Create an new draft grouper if needed to renew
	// 	if(renew) {builder.draft.grouper = new GameObject(); builder.draft.grouper.name = "Dafts Group";}
	// }
#endregion

#region Finder //? Static?
	public DigPlot GetDugAtCoordinate(List<DigPlot> digList, Vector2 coordinate)
	{
		//Go through all the dig in given list
		for (int d = 0; d < digList.Count; d++)
		{
			//Return the dug that has the same coordinate as given coordinate
			if(digList[d].coordinate == coordinate) return digList[d];
		}
		return null;
	}
	public DigPlot GetDugAtPosition(List<DigPlot> digList, Vector2 position)
	{
		//Go through all the dig in given list
		for (int d = 0; d < digList.Count; d++)
		{
			//Return the dug that has the same position as given position
			if(digList[d].position == position) return digList[d];
		}
		return null;
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

	// float MasterScaling(string structure)
	// {
	// 	//Save the master scale
	// 	float ms = builder.masterScale;
	// 	//@ Return the master scaled value of needed structure scaling
	// 	switch(structure)
	// 	{
	// 		case "draft" : return builder.draft.size   * ms;
	// 		case "floor" : return builder.floor.size   * ms;
	// 		case "bridge": return builder.bridge.width * ms;
	// 		case "wall"  : return builder.wall.thick   * ms;
	// 	}
	// 	//Send an error if there no structure that needed
	// 	Debug.LogError("There no '"+structure+"' structure"); return -1;
	// }

	Vector2 GetPositionInDirection(DiggerConfig config, DigPlot dug, Vector2 dirVector)
	{
		//Return the dug position that got increase with spaced multiply in direction
		return dug.position + (dirVector * config.spacing);
	}
#endregion

#region Builder //! PAUSED

// 	//? Assemble Structure -> Build X -> Format X -> Create Structure -> Listing X

// 	void AssembleStructure()
// 	{
// 		//Begin build bridge for all the plot if enable
// 		if(builder.bridge.enable) {for (int p = 0; p < plots.Count; p++) {BuildBridge(plots[p]);}}
// 		//Begin build floor for all the plot if enable
// 		if(builder.floor.enable) {for (int p = 0; p < plots.Count; p++) {BuildFloor(plots[p]);}}
// 	}

// 	#region Bridge
// 	void BuildBridge(DigPlot plot)
// 	{
// 		//Get connection to use for bridging of plot given
// 		NeighborData[] connects = GetConnectionForBridge(plot);
// 		//Go through all 4 connection of plot given
// 		for (int c = 0; c < 4; c++)
// 		{
// 			//Skip if this current connection don't exist
// 			if(connects[c] == null) continue;
// 			//Format an bridge from plot to connection
// 			GameObject bridge = FormatBridge(plot, connects[c], c);
// 			//Get the connection from plot to current connect index
// 			int[] connection = new int[]{plot.index, connects[c].index};
// 			//List bridge with connection, bridge object then build it railing
// 			ListingBridge(connection,c, bridge, BuildRailing(bridge, c, connection));
// 		}
// 	}

// 	NeighborData[] GetConnectionForBridge(DigPlot plot)
// 	{
// 		//Create 4 empty connection
// 		NeighborData[] connects = new NeighborData[4];
// 		//Go through all 4 of neighbor to connect
// 		for (int n = 0; n < 4; n++)
// 		{
// 			//Save the current neighbor
// 			NeighborData neighbor = plot.neighbors[n];
// 			//If this neighbor dig by given plot 
// 			if(neighbor.digByThis) 
// 			{
// 				//This connect will be given plot neighbor 
// 				connects[n] = neighbor; 
// 				//The neighbor of given plot has been bridge in this connection
// 				plot.neighbors[n].hasBridge = true;
// 				//Skip conection randomzie
// 				continue;
// 			}
// 			//? Randomize Bridging
// 			//See if the plot neighbor and itself are already connect
// 			bool alreadyConnect = false;
// 			//Go through all the bridge
// 			for (int b = 0; b < bridges.Count; b++)
// 			{
// 				//Get the connected index of current bridge
// 				int[] connected = bridges[b].connectionIndex;
// 				//How many connection are the same as connected
// 				int hasConnected = 0;
// 				//@ Count how many connection of current bridge match the index of neighbor or plot
// 				if(neighbor.index == connected[0] || plot.index == connected[0]) hasConnected++;
// 				if(neighbor.index == connected[1] || plot.index == connected[1]) hasConnected++;
// 				// Already connect when both connected match
// 				if(hasConnected == 2) alreadyConnect = true;
// 			}
// 			//The randomize result to bridge 
// 			float chance = UnityEngine.Random.Range(0f,100f);
// 			//If chance allow to dig when the neighbor has digged and not already connect
// 			if(builder.bridge.bridgeChance >= chance && neighbor.digged)
// 			{
// 				//This connect will be given plot neighbor if not already connect
// 				if(!alreadyConnect) connects[n] = neighbor;
// 				//The neighbor of given plot has been bridge in this connection
// 				plot.neighbors[n].hasBridge = true;
// 			}
// 		}
// 		//Return all connection has made
// 		return connects;
// 	}

// 	GameObject FormatBridge(DigPlot plot, NeighborData connect, int dir)
// 	{
// 		//Get this plot and current connect position
// 		Vector2 plotP = plot.position; Vector2 conP = connect.position;
// 		//Get position at the middle point between the plot and it connection
// 		Vector2 pos = new Vector2(plotP.x + (conP.x - plotP.x)/2, plotP.y + (conP.y - plotP.y)/2);
// 		//Get the bridge length and increase with spacing if auto scale enable
// 		float length = builder.bridge.length; if(builder.autoScale) {length += builder.spacing;}
// 		//Bridge scaling X as length and Y as width
// 		Vector2 scale = new Vector2(length, MasterScaling("bridge"));
// 		//Rotation default are 0 for horizontal and 90 if vertical
// 		float rot = 0; if(dir <= 1) rot = 90;
// 		//Set name for this bridge (X > Y Bridge)
// 		string name = plot.index + " > " + connect.index + " Bridge";
// 		//Return the newly create an bridge structure using all the data above
// 		return CreateStructure(name, "bridge", pos, scale, rot);
// 	}

// 	GameObject[] BuildRailing(GameObject bridge, int direction, int[] index)
// 	{
// 		//Return null if dont need to build railing
// 		if(!builder.wall.enableRailing) {return null;}
// 		//Create 2 null gameobject to store railing
// 		GameObject[] walls = new GameObject[2]{null, null};
// 		//Get the bridge position
// 		Vector2 bridgePos = bridge.transform.position;
// 		//Create 2 vector for wall position that using bridge position
// 		Vector2[] wallsPos = new Vector2[2]{bridgePos, bridgePos}; 
// 		//Create 2 empty vector for wall scale
// 		Vector2[] wallScale = new Vector2[2];
// 		//Get the thick of wall by master scaling
// 		float wallThick = MasterScaling("wall");
// 		//Get how far to place wall by increase half of bridge width with wall thick
// 		float railingSpaced = (MasterScaling("bridge")/2) + (wallThick/2);
// 		//Get railing as length as custom wall length
// 		float railingLength = builder.wall.length;
// 		//Increase railing with spacing if enable auto scale
// 		if(builder.autoScale) {railingLength += builder.spacing;}
// 		//@ Set the wall position and scale in certain axis base on it horizontal or vertical
// 		//The bridge are vertical
// 		if(direction <= 1)
// 		{
// 			wallsPos[0].x += railingSpaced;
// 			wallsPos[1].x -= railingSpaced;
// 			wallScale[0] = new Vector3(wallThick, railingLength);
// 			wallScale[1] = new Vector3(wallThick, railingLength);
// 		}
// 		//The bridge are horizontal
// 		else
// 		{
// 			wallsPos[0].y += railingSpaced;
// 			wallsPos[1].y -= railingSpaced;
// 			wallScale[0] = new Vector3(railingLength, wallThick);
// 			wallScale[1] = new Vector3(railingLength, wallThick);
// 		}
// 		//Go through 2 railing
// 		for (int r = 0; r < 2; r++)
// 		{
// 			//Set the name for railing (X > Y Raling [R])
// 			string naming = index[0] + " > " + index[1] + " Railing ["+r+"]";
// 			//Create an wall at position and scale has get
// 			walls[r] = CreateStructure(naming, "wall", wallsPos[r], wallScale[r]);
// 		}
// 		//Return railing has build
// 		return walls;
// 	}
// 	#endregion

// 	#region Floor
// 	void BuildFloor(DigPlot plot)
// 	{
// 		//Get the master scaling of floor
// 		Vector2 scale = new Vector2(MasterScaling("floor"), MasterScaling("floor"));
// 		//Set the name for floor (X Floor)
// 		string naming = plot.index + " Floor";
// 		//Createan floor with name, scale has got and at given plot position
// 		GameObject floor = CreateStructure(naming, "floor", plot.position, scale);
// 		//List the newly create floor with given plot index then build barrier at it
// 		ListingFloor(plot.index, floor, BuildBarrier(plot));
// 	}

// 	List<GameObject> BuildBarrier(DigPlot plot)
// 	{
// 		//Return null if dont need to build barrier
// 		if(!builder.wall.enableBarrier) {return null;}
// 		//Empty list of gameobject for barrier
// 		List<GameObject> barriers = new List<GameObject>();
// 		//Go through all the neighbor of given plot
// 		for (int n = 0; n < 4; n++)
// 		{
// 			//Get this plot neighbor
// 			NeighborData neighbor = plot.neighbors[n];
// 			//If neighbor dont has bridge
// 			if(!neighbor.hasBridge)
// 			{
// 				//Format an barrier for given plot in this neighbor then list them
// 				GameObject barrier = FormatBarrier(plot, n);
// 				//Add barrier to it list data if it exist
// 				if(barrier != null) barriers.Add(barrier);
// 			}
// 			//If neighbor has bridge
// 			else
// 			{
// 				//Skip if not need to barricade gate in this neighbor
// 				if(!builder.wall.barricadeNeighbor) continue;
// 				//Get gate length by half deceased floor scale and bridge that got increase with wall thick
// 				float gateLength = ((MasterScaling("floor")-MasterScaling("bridge"))/2) + MasterScaling("wall");
// 				//For each side of this neighbor
// 				for (int s = 0; s < 2; s++)
// 				{
// 					//Format an barrier for given plot in this direction with gate length
// 					GameObject barrier = FormatBarrier(plot, n, gateLength);
// 					//Format the created barrier for given plot in this neighbor and side to be an gate
// 					barriers.Add(FormatGate(barrier, plot, n, s));
// 				}
// 			}
// 		}
// 		//Return all the barrier has created
// 		return barriers;
// 	}

// 	GameObject FormatBarrier(DigPlot plot, int direction, float gateLength = -1)
// 	{
// 		//Don't barricade neighbor in this direction if it has dig when no need to barricade
// 		if(plot.neighbors[direction].digged && !builder.wall.barricadeNeighbor) return null;
// 		Vector2 position = plot.position;
// 		Vector2 scale = new Vector2();
// 		float barrierLength = 0;
// 		float wallThick = MasterScaling("wall");
// 		float floorScale = MasterScaling("floor");
// 		//If enable auto scale
// 		if(builder.autoScale)
// 		{
// 			//Set length for barrier by incease floor scale with double wall thick 
// 			barrierLength = floorScale + (wallThick*2);
// 			//? Fixing wall poking into floor when not barricade neighbour
// 			if(!builder.wall.barricadeNeighbor) for (int n = 0; n < 4; n++)
// 			{
// 				//Skip neighbor in this direction if currently build barrier for it or has dig
// 				if(n == direction || !plot.neighbors[n].digged) continue;
// 				//If this barrier direction are VERTICAL
// 				if(direction <= 1)
// 				{
// 					//If direction are LEFT cut off barrier length then move RIGHT
// 					if(n == 2) {position.x += wallThick/2; barrierLength -= wallThick;}
// 					//If direction are RIGHT cut off barrier length then moveLEFT
// 					if(n == 3) {position.x -= wallThick/2; barrierLength -= wallThick;}
// 				}
// 				//If this barrier direction are HORIZONTAL
// 				if(direction >= 2)
// 				{
// 					//If direction are UP cut off barrier length then move DOWN
// 					if(n == 0) {position.y -= wallThick/2; barrierLength -= wallThick;}
// 					//If direction are DOWN cut off barrier length then movE UP
// 					if(n == 1) {position.y += wallThick/2; barrierLength -= wallThick;}
// 				}
// 			}
// 		}
// 		//Overwrite barrier length to given gate length if given not -1
// 		if(gateLength >= 0) {barrierLength = gateLength;}
// 		//Modify barrier length will custom wall length
// 		barrierLength += builder.wall.length;
// 		//Get barrier spacing by use half of combined floor scale and wall thick
// 		float barrierSpaced = (floorScale + wallThick)/2;
// 		//What is given direction
// 		switch(direction)
// 		{
// 			//@ Set the axis to spacing and scale depend on given direction
// 			case 0: position.y += barrierSpaced; scale = new Vector2(barrierLength, wallThick); break;
// 			case 1: position.y -= barrierSpaced; scale = new Vector2(barrierLength, wallThick); break;
// 			case 2: position.x -= barrierSpaced; scale = new Vector2(wallThick, barrierLength); break;
// 			case 3: position.x += barrierSpaced; scale = new Vector2(wallThick, barrierLength); break;
// 		}
// 		//Set the name for barrier (X Barrier [D])
// 		string naming = plot.index + " Barrier " + "["+direction+"]";
// 		//Return the created wall that use getted name, position and scale as barrier
// 		return CreateStructure(naming, "wall", position, scale);
// 	}

// 	GameObject FormatGate(GameObject gate, DigPlot plot, int direction, int side)
// 	{
// 		float floorScale = MasterScaling("floor"); 
// 		float wallThick = MasterScaling("wall"); 
// 		float bridgeWidth = MasterScaling("bridge");
// 		//Overwrite the name or gate formated (X Gate [D] [S])
// 		gate.name = plot.index + " Gate " + "["+direction+"] " +"["+side+"]";
// 		//Align by quater of combined floor scale with bridge width then increase with half of wall thick
// 		float align = (floorScale + bridgeWidth)/4 + wallThick/2;
// 		//Create an empty vector for align position
// 		Vector3 alignPosition = Vector3.zero;
// 		//If direction in vertical then align the X axis base on it given side
// 		if(direction <= 1) {if(side == 0) {alignPosition.x = align;} if(side == 1) {alignPosition.x = -align;}}
// 		//If direction in horizonal then align the Y axis base on it given side
// 		if(direction >= 2) {if(side == 0) {alignPosition.y = align;} if(side == 1) {alignPosition.y = -align;}}
// 		//Align gate position
// 		gate.transform.position += alignPosition;
// 		//Return the gate has align
// 		return gate;
// 	}
// 	#endregion


// 	GameObject CreateStructure(string naming,string structure,Vector2 position,Vector2 scale,float rotation=0)
// 	{
// 		//An empty prefab and grouper to use
// 		GameObject prefab = null; GameObject grouper = null; 
// 		//@ Set the structure prefab and grouper as the name given
// 		switch(structure)
// 		{
// 			case "floor" : prefab = builder.floor.prefab ; grouper = builder.floor.grouper ; break;
// 			case "bridge": prefab = builder.bridge.prefab; grouper = builder.bridge.grouper; break;
// 			case "wall"  : prefab = builder.wall.prefab  ; grouper = builder.wall.grouper  ; break;
// 		}
// 		//Send an error if there no structure that needed
// 		if(prefab == null) Debug.LogError("There no '"+structure+"' structure to create");
// 		//Instantiate an structure prefab in given position and with given rotation
// 		GameObject builded = Instantiate(prefab, position, Quaternion.Euler(0,0,rotation));
// 		//Set the builded structure scale as given
// 		builded.transform.localScale = scale;
// 		//Group the builded structure
// 		builded.transform.SetParent(grouper.transform);
// 		//Add set structure name
// 		builded.name = naming;
// 		//Return the builded structure
// 		return builded;
// 	}

// 	void ListingFloor(int index, GameObject obj, List<GameObject> wall)
// 	{
// 		//@ Create empty floor data then assign given data to it
// 		FloorData data = new FloorData();
// 		data.plotIndex = index; 
// 		data.obj = obj;
// 		data.renderer = obj.GetComponent<SpriteRenderer>(); 
// 		data.walls = wall;

// 		//@ Color the floor and it walls
// 		data.renderer.color = builder.floor.color;
// 		if(wall != null) for (int w = 0; w < wall.Count; w++)
// 		{wall[w].GetComponent<SpriteRenderer>().color = builder.wall.color;}
// 		//Add floor data to list
// 		floors.Add(data);
// 	}

// 	void ListingBridge(int[] connection, int direction, GameObject obj, GameObject[] wall)
// 	{
// 		//@ Create empty bridge data then assign given data to it
// 		BridgeData data = new BridgeData();
// 		data.connectionIndex = connection; 
// 		data.direction = direction; 
// 		data.obj = obj;
// 		data.renderer = obj.GetComponent<SpriteRenderer>(); data.walls = wall;

// 		//@ Color the bridge and it walls
// 		data.renderer.color = builder.bridge.color;
// 		if(wall != null) for (int w = 0; w < wall.Length; w++)
// 		{wall[w].GetComponent<SpriteRenderer>().color = builder.wall.color;}
// 		//Add bridge data to list
// 		bridges.Add(data);
// 	}

// 	void Drafting(DigPlot miner, DigPlot digged)
// 	{
// 		//Only need to create new draft if plots haven't get enough draft and only when need to draft
// 		if(drafts.Count >= plots.Count || !builder.draft.enable) return;
// 		//Setup an empty new draft
// 		DraftData newDraft = new DraftData();
// 		//Save the miner index to draft's plot index
// 		newDraft.plotIndex = miner.index;
// 		//Instantiate an draft prefab at miner position then save it to data
// 		newDraft.obj = Instantiate(builder.draft.prefab, miner.position, Quaternion.identity);
// 		//Set the draft object scale as the master scaled of the draft scale
// 		newDraft.obj.transform.localScale = new Vector2(MasterScaling("draft"), MasterScaling("draft"));
// 		//Group the draft parent
// 		newDraft.obj.transform.SetParent(builder.draft.grouper.transform);
// 		//Add plot index to the draft object name
// 		newDraft.obj.name = "Draft of " + newDraft.plotIndex;
// 		//Save the new draft object's sprite renderer to data
// 		newDraft.renderer = newDraft.obj.GetComponent<SpriteRenderer>();
// 		//Add new draft data to list
// 		drafts.Add(newDraft);
// 		//Change the draft color at miner index to miner color
// 		ChangeDraftColor(miner.index, Builder.Draft.Colors.miner);
// 		//Change the draft color at digged index to digged color
// 		ChangeDraftColor(digged.index, Builder.Draft.Colors.digged);
// 	}

// 	void ChangeDraftColor(int index, Builder.Draft.Colors color)
// 	{
// 		//Only change draft color if drafting enable
// 		if(!builder.draft.enable) return;
// 		//Get the sprite render of draft at given index
// 		SpriteRenderer renderer = drafts[index].renderer;
// 		//@ Set that draft color color according to given string
// 		if(color == Builder.Draft.Colors.digged)   {renderer.color = builder.draft.digged;   return;}
// 		if(color == Builder.Draft.Colors.miner)    {renderer.color = builder.draft.miner;    return;}
// 		if(color == Builder.Draft.Colors.stuck)    {renderer.color = builder.draft.stuck;    return;}
// 		if(color == Builder.Draft.Colors.bypassed) {renderer.color = builder.draft.bypassed; return;}
// 		if(color == Builder.Draft.Colors.over)     {renderer.color = builder.draft.over;     return;}
// 	}
#endregion
}
} //? namespace close