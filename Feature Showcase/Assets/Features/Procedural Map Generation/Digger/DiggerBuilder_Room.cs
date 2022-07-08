using ProceduralMapGeneration.Digger;
using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using System;

namespace ProceduralMapGeneration.Digger
{
public class DiggerBuilder_Room : MonoBehaviour
{
	public DiggerConfig diggerConfig;
	public Builder builder; [Serializable] public class Builder
	{
		[Tooltip("All structure size will multiply with this scale")] 
		public float masterScale;
		[Tooltip("The following will be change:\n- Bridge length will be scale with spacing\n- Wall will be scale wih bridge and wall size\n- Value that got auto scale will now use to modify")]
		public bool autoScale;
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
			public bool enableBarrier, enableCorner, enableRailing;
			[Tooltip("Create an barrier or gate when there is neighbor")]
			public bool barricadeNeighbor;
			public GameObject prefab;
			public float thick, length;
			public Color color;
			[HideInInspector] public GameObject grouper;
		}
	}

	List<FloorData> floors = new List<FloorData>();
	List<BridgeData> bridges = new List<BridgeData>();
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

	public void BeginDig() {ClearStructure(false); DiggerAlgorithm.i.RecursiveDig(diggerConfig);}

	void OnEnable(){diggerConfig.digCompleted += AssembleStructure;}
	void OnDisable() {diggerConfig.digCompleted -= AssembleStructure;}

	//? Assemble Structure -> Build Bridge -> Decide direction to bridge -> Format Bridge -> Format Railing ->
	//? -> Build all floor -> Build wall for each floor -> Format Barrier -> Format Corner -> Format Gate -> Completed

	void AssembleStructure()
	{
		//Clear all the structure before assemble new one
		ClearStructure(true);
		//Begin build bridge for all the plot if enable
		if(builder.bridge.enable) {for (int p = 0; p < diggerConfig.dugs.Count; p++) {ConnectBridge(diggerConfig.dugs[p]);}}
		//Begin build floor for all the plot if enable
		if(builder.floor.enable) {for (int p = 0; p < diggerConfig.dugs.Count; p++) {BuildFloor(diggerConfig.dugs[p]);}}
	}

#region Bridge
	void ConnectBridge(DigPlot plot)
	{
		//Decide which neighbor will be connect to build bridge on them
		DigPlot.Neighbor[] connects = DecideNeighborToConnect(plot);
		//Go through all 4 connection has get
		for (int c = 0; c < 4; c++)
		{
			//Skip if this current connection don't exist
			if(connects[c] == null) continue;
			//Build an bridge from plot to connection
			GameObject bridge = BuildBridge(plot, connects[c], c);
			//Get the connection from plot to current connect index
			int[] connection = new int[]{plot.index, connects[c].index};
			//List bridge with connection, bridge object then build it railing
			ListingBridge(connection,c, bridge, BuildRailing(bridge, c, connection));
		}
	}

	DigPlot.Neighbor[] DecideNeighborToConnect(DigPlot plot)
	{
		//Create 4 empty connection
		DigPlot.Neighbor[] connects = new DigPlot.Neighbor[4];
		//Go through all 4 of neighbor to connect
		for (int n = 0; n < 4; n++)
		{
			//Save the current neighbor
			DigPlot.Neighbor neighbor = plot.neighbors[n];
			//If this neighbor dig by given plot 
			if(neighbor.digbyThis) 
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
			//If chance allow to dig when the neighbor not empty and not already connect
			if(builder.bridge.bridgeChance >= chance && !neighbor.empty)
			{
				//This connect will be given plot neighbor if not already connect
				if(!alreadyConnect) connects[n] = neighbor;
				//The neighbor of given plot has been bridge in this connection
				plot.neighbors[n].hasBridge = true;
			}
		}
		return connects;
	}

	GameObject BuildBridge(DigPlot plot, DigPlot.Neighbor connect, int dir)
	{
		//Get this plot and current connect position
		Vector2 plotP = plot.position; Vector2 conP = connect.position;
		//Get position at the middle point between the plot and it connection
		Vector2 pos = new Vector2(plotP.x + (conP.x - plotP.x)/2, plotP.y + (conP.y - plotP.y)/2);
		//Get the bridge length and increase with spacing if auto scale enable
		float length = builder.bridge.length; if(builder.autoScale) {length += MasterScaling("spacing");}
		//Bridge scaling X as length and Y as width
		Vector2 scale = new Vector2(length, MasterScaling("bridge"));
		//Rotation default are 0 for horizontal and 90 if vertical
		float rot = 0; if(dir <= 1) rot = 90;
		//Set name for this bridge (X > Y Bridge)
		string name = plot.index + " > " + connect.index + " Bridge";
		//Return the newly create an bridge structure using all the data above
		return CreateStructure(name, "bridge", pos, scale, rot);
	}
#endregion

#region Floor
	void BuildFloor(DigPlot plot)
	{
		//Get the master scaling of floor
		Vector2 scale = new Vector2(MasterScaling("floor"), MasterScaling("floor"));
		//Set the name for floor (X Floor)
		string naming = plot.index + " Floor";
		//Createan floor with name, scale has got and at given plot position
		GameObject floor = CreateStructure(naming, "floor", plot.position, scale);
		//List the newly create floor with given plot index then formating it wall
		ListingFloor(plot.index, floor, FormatWalls(plot));
	}

	List<GameObject> FormatWalls(DigPlot plot)
	{
		//Return null if dont need to build barrier
		if(!builder.wall.enableBarrier) {return null;}
		//Empty list of gameobject for wall
		List<GameObject> walls = new List<GameObject>();
		//Build corner for this plot first if needed
		if(builder.wall.enableCorner) walls = BuildCorner(plot);
		//Go through all the neighbor of given plot
		for (int n = 0; n < 4; n++)
		{
			//Get this plot neighbor
			DigPlot.Neighbor neighbor = plot.neighbors[n];
			//If neighbor dont has bridge
			if(!neighbor.hasBridge)
			{
				//Build an barrier for given plot in this neighbor then list them
				GameObject barrier = BuildBarrier(plot, n);
				//Add barrier to it list data if it exist
				if(barrier != null) walls.Add(barrier);
			}
			//If neighbor has bridge
			else
			{
				//Skip if not need to barricade gate in this neighbor
				if(!builder.wall.barricadeNeighbor) continue;
				//Get gate length by half deceased floor scale and bridge that got decrease with wall thick
				float gateLength = ((MasterScaling("floor")-MasterScaling("bridge"))/2) - MasterScaling("wall");
				//For each side of this neighbor
				for (int s = 0; s < 2; s++)
				{
					//Build an gate for this plot in this neighbor and on this side
					walls.Add(BuildGate(BuildBarrier(plot, n, gateLength), plot, n, s));
				}
			}
		}
		//Return all the barrier has created
		return walls;
	}

	GameObject BuildBarrier(DigPlot plot, int direction, float gateLength = -1)
	{
		//Don't barricade neighbor in this direction if it has dig when no need to barricade
		if(!plot.neighbors[direction].empty && !builder.wall.barricadeNeighbor) return null;
		Vector2 position = plot.position;
		Vector2 scale = new Vector2();
		float barrierLength = 0;
		float wallThick = MasterScaling("wall");
		float floorScale = MasterScaling("floor");
		// Barrier length as floor scale if use auto scale
		if(builder.autoScale) barrierLength = floorScale;
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

	List<GameObject> BuildCorner(DigPlot plot)
	{
		float floorScale = MasterScaling("floor"); 
		float wallThick = MasterScaling("wall");
		//Get the scale as square that use wall thick amount
		Vector2 scale = new Vector2(wallThick, wallThick);
		//List of corner will be create
		List<GameObject> corners = new List<GameObject>();
		//? Create corner for BARRIER
		for (int b = 0; b < 4; b++)
		{
			//If does not barricade neighbor then checking them in 2 both direction
			if(!builder.wall.barricadeNeighbor) switch(b)
			{
				//Skip if any neighbor in [0 = <-↑] [1 = ↑->] [2 = <-↓] [3 = ↓->] are not empty
				case 0: if(!plot.neighbors[0].empty || !plot.neighbors[2].empty) continue; break;
				case 1: if(!plot.neighbors[0].empty || !plot.neighbors[3].empty) continue; break;
				case 2: if(!plot.neighbors[1].empty || !plot.neighbors[2].empty) continue; break;
				case 3: if(!plot.neighbors[1].empty || !plot.neighbors[3].empty) continue; break;
			}
			//The vector that will use to align corner away from original position
			Vector2 align = new Vector2(0,0);
			//Corner naming (X Corner UL|UR|DL|DR)
			string naming = "";
			//Each of 4 corner
			switch(b)
			{
				//<-↑ UP LEFT
				case 0: align = new Vector2(-1,01); naming = plot.index + " Barrier Corner [UL]"; break;
				//↑-> UP RIGHT
				case 1: align = new Vector2(01,01); naming = plot.index + " Barrier Corner [UR]"; break;
				//<-↓ DOWN LEFT
				case 2: align = new Vector2(-1,-1); naming = plot.index + " Barrier Corner [DL]"; break;
				//↓-> DOWN RIGHT
				case 3: align = new Vector2(01,-1); naming = plot.index + " Barrier Corner [DR]"; break;
			}
			//Get position by aligned given plot position with half of floor scale + wall thick 
			Vector2 position = plot.position + (align * ((floorScale + wallThick)/2));
			//Create an barrier with naming, position and scale has get
			corners.Add(CreateStructure(naming, "wall", position, scale));
		}
		//Dont create corner for gate if does not barricade neighbor and enable railing
		if(!builder.wall.barricadeNeighbor && !builder.wall.enableRailing) return corners;
		//? Create corner for GATE
		for (int dir = 0; dir < 4; dir++)
		{
			//Skip if there no neighbor in this direction
			if(plot.neighbors[dir].empty) continue;
			//Align use half of total floor scale with wall thick
			float align = ((floorScale + wallThick)/2);
			//Get the amount that 2 corner will space from each other
			float spacing = (MasterScaling("bridge") + wallThick)/2;
			//Corner naming (X Gate Corner [D,S])
			string naming = "";
			//Save given plot position
			Vector2 pos = plot.position;
			//@ Create 2 corner for each gate at each direction
			switch(dir)
			{
				//@ Set position of corner by modify it axis with align and spacing base on this direction
				case 0:
					naming = plot.index + " Gate Corner " + "[UP]";
					corners.Add(CreateStructure(naming, "wall", pos + new Vector2(+spacing, +align), scale));
					corners.Add(CreateStructure(naming, "wall", pos + new Vector2(-spacing, +align), scale));
				break; 
				case 1:
					naming = plot.index + " Gate Corner " + "[DOWN]";
					corners.Add(CreateStructure(naming, "wall", pos + new Vector2(+spacing, -align), scale));
					corners.Add(CreateStructure(naming, "wall", pos + new Vector2(-spacing, -align), scale));
				break;
				case 2:
					naming = plot.index + " Gate Corner " + "[LEFT]";
					corners.Add(CreateStructure(naming, "wall", pos + new Vector2(-align, +spacing), scale));
					corners.Add(CreateStructure(naming, "wall", pos + new Vector2(-align, -spacing), scale));
				break;
				case 3:
					naming = plot.index + " Gate Corner " + "[RIGHT]";
					corners.Add(CreateStructure(naming, "wall", pos + new Vector2(+align, +spacing), scale));
					corners.Add(CreateStructure(naming, "wall", pos + new Vector2(+align, -spacing), scale));						
				break;
			}
		}
		return corners;
	}

	GameObject BuildGate(GameObject gate, DigPlot plot, int direction, int side)
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
		//Increase railing with spacing the got decrease double wall thick if enable auto scale
		if(builder.autoScale) {railingLength += MasterScaling("spacing") - wallThick*2;}
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

	public void ClearStructure(bool renew)
	{
		//Dont clear if still generating
		if(diggerConfig.isDigging) return;
		//Clear all structure data list
		floors.Clear(); bridges.Clear();
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

#region Converter
	float MasterScaling(string structure)
	{
		//Save the master scale
		float ms = builder.masterScale;
		//@ Return the master scaled value of needed structure scaling
		switch(structure)
		{
			case "spacing": return diggerConfig.spacing - (builder.floor.size * ms);
			case "floor"  : return builder.floor.size   * ms;
			case "bridge" : return builder.bridge.width * ms;
			case "wall"   : return builder.wall.thick   * ms;
		}
		//Send an error if there no structure that needed
		Debug.LogError("There no '"+structure+"' structure"); return -1;
	}
#endregion
}
} //? Close namespace
