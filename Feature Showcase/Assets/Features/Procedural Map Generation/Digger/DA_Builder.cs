using ProceduralMapGeneration.Digger;
using System.Collections.Generic;
using System.Collections;
using UnityEngine;

namespace ProceduralMapGeneration.Digger
{
public class DA_Builder : MonoBehaviour
{
	public DiggerConfig diggerConfig;
	

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
}
} //? Close namespace
