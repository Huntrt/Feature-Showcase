using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using System;

namespace ProceduralMapGeneration.Digger
{
public class DiggerAlgorithm : MonoBehaviour
{
	//Set this class to singleton
	public static DiggerAlgorithm i {get{if(_i==null){_i = GameObject.FindObjectOfType<DiggerAlgorithm>();}return _i;}} static DiggerAlgorithm _i;

#region Generation
	/// Begin recursive dig with config class given
	public void RecursiveDig(DiggerConfig config, bool overwrite = false)
	{
		//Stop if already dig except when need to overwriten
		if(config.isDigging) {if(!overwrite) {return;}}
		//Renew the preview grouper
		ClearPreview(config, true);
		//Clear all the dig in config given
		config.dugs.Clear();
		//Are now digging
		config.isDigging = true;
		//Dig at 0,0 then set it position at start position
		DigAtLocation(config, 0, new Vector2(0,0), config.startPosition);
		//Begin digging at that first plot of given config
		StartCoroutine(Digging(config, config.dugs[0], config.dugs[0]));
	}

	IEnumerator Digging(DiggerConfig config, DigPlot miner, DigPlot digged)
	{
		//This room are now the current leader
		config.miners.Add(miner);
		//Draft miner and digged
		Previewing(config, miner, digged);
		//Wait for an frame
		yield return null;
		//Begin decide direction to dig at this miner
		DecideDirectionToDig(config, miner);
		//Remove this miner after dig
		config.miners.Remove(miner);
		//Begin check the digging progress if haven't dig enough plot
		if(config.dugs.Count < config.amount) {CheckingDigProgress(config, miner);}
		//Dig are complete when there no miner left
		if(config.miners.Count <= 0) {CompleteDig(config);}
	}

	//? Try to bypassed of dig in 4 direction
	void DecideDirectionToDig(DiggerConfig config, DigPlot miner)
	{
		/// DIG
		//Go through all 4 direction when there still available direction
		if(miner.availableDirection.Count > 0) for (int d = 0; d < 4; d++)
		{
			//Create an temporary replicate of miner's available direction
			List<int> aDir = new List<int>(miner.availableDirection);
			//Exit loop out of temporary direction
			if(aDir.Count == 0) {break;}
			//Randomly decide the available direction to dig
			int decided = UnityEngine.Random.Range(0, aDir.Count);
			//Try to dig at that available decided direction with it randomize result
			TryToDig(config, miner, aDir[decided], RandomizingDigDirection(config)[decided]);
			//Remove this direction from temporary
			aDir.Remove(decided);
		}

		/// BYPASS
		//If there are no available direction and this is the only miner left
		else if(config.miners.Count <= 1)
		{
			//Will continuous run until miner has direction to bypass
			while (miner.bypassedDirection == -1)
			{
				//Randomly bypass anjy direction
				int bypass = UnityEngine.Random.Range(0,4);
				//If the result of bypass direction allow then miner start bypass in that direction
				if(RandomizingDigDirection(config)[bypass] == true) miner.bypassedDirection = bypass;
			}
			//Change the draft color at miner index to stuck
			ChangePreviewColor(config, miner.index, config.preview.stuck);
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
		//? Get the dug next to miner in given direction
		DigPlot nextDig = GetAdjacentDug(config, miner, dir, out Vector2 nextCoord);

		//? Are able to dig in this direction
		//STOP dig and this direction are no longer available when next dug have exist
		if(nextDig != null) {SetDirectionUnavailable(dir, miner); return;}
		//STOP dig if has reach max dug amount or not allow
		if(config.dugs.Count >= config.amount || !allow) {return;}
		//STOP dig if miner has dig over the maximum allow
		if(miner.digCount >= config.miningConstraint.maximum) {return;}

		//? Dig for miner with given direction at next corrdinate
		DigNextPlot(config, miner, dir, nextCoord);
	}

	void TryToBypass(DiggerConfig config, DigPlot bypasser, int dir)
	{
		//Attempt to get the dug in given direction of current bypasser
		DigPlot attempt = GetAdjacentDug(config, bypasser, dir, out Vector2 nextCoord);
		//If the attempt exist
		if(attempt != null) 
		{
			//Use that attempt as an new bypasser to bypass in the same direction
			TryToBypass(config, attempt, dir);
			//Change the draft color at attempt index to bypassed
			ChangePreviewColor(config, attempt.index, config.preview.bypassed);
		}
		//If the attempt havent got dug
		else
		{
			//Dig an new plot for bypasser at the same direction of that empty attempt
			DigNextPlot(config, bypasser, dir, nextCoord);
		}
	}
	
	//? Get the the dug at given direction to the given dug 
	DigPlot GetAdjacentDug(DiggerConfig config, DigPlot dug, int dir, out Vector2 nextCoord)
	{
		//Get the next coordinate at given dug coord coordinate increase with vector of given direction
		nextCoord = dug.coordinate + DirectionIndexToVector(dir);
		//Find the next dug at next coordinate
		return DiggerGeneral.GetDugAtCoordinate(config.dugs, nextCoord);
	}
	
	//? Dig an new plot with given position and coordinates
	DigPlot DigAtLocation(DiggerConfig config, int index, Vector2 coord, Vector2 pos)
	{
		DigPlot newDig = new DigPlot();
		newDig.index = index;
		newDig.coordinate = coord; 
		newDig.position = pos;
		newDig.empty = false;
		//Add the new dig to config list then return it
		config.dugs.Add(newDig); return newDig;
	}

	void DigNextPlot(DiggerConfig config, DigPlot miner, int dir, Vector2 coord)
	{
		//Get the next position by using vector of given direction with given miner 
		Vector2 nextPos = GetPositionInDirection(config, miner, DirectionIndexToVector(dir));
		//Create an new dug with index of current dug count at given coordinate with position has get
		DigPlot newDig = DigAtLocation(config, config.dugs.Count, coord, nextPos);
		//Given miner has dig and neighbor in given direction got dig by it
		miner.digCount++; miner.neighbors[dir].digbyThis = true;
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
			ChangePreviewColor(config, miner.index, config.preview.over);
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
			//Get the neighbor in this direction of this dug
			DigPlot.Neighbor neighbor = config.dugs[d].neighbors[n];
			//Get coordinate of this neighbor by increase this dug coordinate with direction vector
			neighbor.coordinate = config.dugs[d].coordinate + dirVector;
			//Get position of this neighbor by apply this dug with direction vector
			neighbor.position = GetPositionInDirection(config, config.dugs[d], dirVector);
			//Find an dug at this neighbor coodrinate
			DigPlot findDug = DiggerGeneral.GetDugAtCoordinate(config.dugs, neighbor.coordinate);
			//If found an dug
			if(findDug != null)
			{
				//Set this neighbor index as found dug index
				neighbor.index = findDug.index;
				//This neighbor are no nonger empty
				neighbor.empty = false;
				//This dug has lost one empty neighbor
				config.dugs[d].emptyNeighbor--;
			}
		}
	}

	void CompleteDig(DiggerConfig config)
	{
		//Set all 4 neighbors of all dug
		SetAllDugNeighbors(config);
		//No longer digging
		config.isDigging = false;
		//Call complete dig
		config.digCompleted?.Invoke();
		//Clear all preview when need to clear after complete
		if(config.preview.clearAfterDig) ClearPreview(config, false);
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
	void Previewing(DiggerConfig config, DigPlot miner, DigPlot digged)
	{
		//Stop if dont need to review
		if(!config.preview.enable) return;
		//Only need to create new preview if dig haven't get enough preview
		if(config.preview.previewObjs.Count >= config.dugs.Count) return;
		PreviewObj newPre = new PreviewObj();
		newPre.digIndex = miner.index;
		newPre.obj = Instantiate(config.preview.prefab, miner.position, Quaternion.identity);
		newPre.obj.transform.localScale = new Vector2(config.preview.size, config.preview.size);
		newPre.obj.transform.SetParent(config.preview.grouper.transform);
		newPre.obj.name = "Draft of " + newPre.digIndex;
		newPre.render = newPre.obj.GetComponent<SpriteRenderer>();
		config.preview.previewObjs.Add(newPre);
		//Change the preview color at miner index to miner color
		ChangePreviewColor(config, miner.index, config.preview.miner);
		//Change the preview color at digged index to digged color
		ChangePreviewColor(config, digged.index, config.preview.digged);
	}

	void ChangePreviewColor(DiggerConfig config, int peviewIndex, Color color) 
	{
		//Stop if dont need to review
		if(!config.preview.enable) return;
		//Set the color at preview index in given config to given cloor
		config.preview.previewObjs[peviewIndex].render.color = color;
	}

	public void ClearPreview(DiggerConfig config, bool renew)
	{
		//Stop if dont need to review
		if(!config.preview.enable) return;
		//Clear preview objects list
		config.preview.previewObjs.Clear(); 
		//Destroy the preview grouper if it already exist
		if(config.preview.grouper != null) Destroy(config.preview.grouper);
		//Create an new preview grouper if need to renew
		if(renew) config.preview.grouper = new GameObject(); config.preview.grouper.name = "Preview Group";
	}
#endregion
}
} //? namespace close