using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using System;

namespace ProceduralMapGeneration.Digger
{
public class DiggerAlgorithm : MonoBehaviour
{
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
			if(config.digChance.useDirectionChance)
			{
				//@ Set the result base on comparing chance of each direction 
				if(r == 0 && config.digChance.up    >= decide) {result[r] = true;}
				if(r == 1 && config.digChance.down  >= decide) {result[r] = true;}
				if(r == 2 && config.digChance.left  >= decide) {result[r] = true;}
				if(r == 3 && config.digChance.right >= decide) {result[r] = true;}
			}
			//If all direction use an same chance
			else
			{
				//This direction result are true if it chance are higher than decide
				if(config.digChance.generalChance >= decide) {result[r] = true;}
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
		if(miner.digCount >= config.miningConstraint.maximum) {return;}

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
		nextPlot = DiggerGeneral.GetDugAtCoordinate(config.dugs, nextCoord);
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
		if(config.miners.Count == 0 || miner.digCount < config.miningConstraint.minimum)
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
			if(!DiggerGeneral.GetDugAtCoordinate(config.dugs, neighbor.coordinate).empty) {neighbor.empty = false;}
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

	Vector2 GetPositionInDirection(DiggerConfig config, DigPlot dug, Vector2 dirVector)
	{
		//Return the dug position that got increase with spaced multiply in direction
		return dug.position + (dirVector * config.spacing);
	}
#endregion

#region Preview

	// public void ClearDraft(bool renew)
	// {
	// 	//Clear draft data list
	// 	//! drafts.Clear(); 
	// 	//Destroy the draft grouper if it already exist
	// 	if(builder.draft.grouper != null) Destroy(builder.draft.grouper);
	// 	//Create an new draft grouper if needed to renew
	// 	if(renew) {builder.draft.grouper = new GameObject(); builder.draft.grouper.name = "Dafts Group";}
	// }

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