using System.Collections.Generic;
using UnityEngine;
using System;

namespace ProceduralMapGeneration.Digger
{
	[Serializable] public class DiggerConfig
	{
		[Header("------ Settings ------")]
		[Tooltip("The amount of plot will need to dig")]
		public int amount;
		[Tooltip("The world position spacing between each plot")]
		public float spacing;
		[Tooltip("The position to begin dig")]
		public Vector2 startPosition;
		public DigChance digChance; [Serializable] public class DigChance 
		{
			[Tooltip("The percent chance for miner to dig an new plot in any direction")]
			public float generalChance;
			[Tooltip("Enable to set the chance of each direction")]
			public bool useDirectionChance; 
			[Range(0,100)] public float up,down,left,right;
		}
		[Tooltip("The minimum and maximum amount miner required or allow to dig")]
		public MiningConstraint miningConstraint; [Serializable] public class MiningConstraint
		{
			[Range(0,4)] public int minimum; [Range(1,4)] public int maximum = 1;
		}
		[HideInInspector] public bool isDigging; public Action digCompleted;
		public PreviewConfig preview;
		[Header("------- Outputs -------")]
		public List<DigPlot> dugs = new List<DigPlot>();
		public List<DigPlot> miners = new List<DigPlot>();
	}
	[Serializable] public class DigPlot : PlotData
	{
		[Serializable] public class Neighbor : PlotData {public bool digbyThis, hasBridge;}
		public Neighbor[] neighbors = new Neighbor[4]
		{
			new Neighbor(),
			new Neighbor(),
			new Neighbor(),
			new Neighbor()
		};
		public int emptyNeighbor = 4, digCount;
		[HideInInspector] public int bypassedDirection = -1;
		[HideInInspector] public List<int> availableDirection = new List<int>{0,1,2,3};
	}

	[Serializable] public class PreviewConfig
	{
		public bool enable, clearAfterDig;
		public GameObject prefab;
		public float size;
		public Color digged, miner, stuck, bypassed, over;
		public enum Colors {digged, miner, stuck, bypassed, over};
		[HideInInspector] public GameObject grouper;
		public List<PreviewObj> previewObjs = new List<PreviewObj>();
	}
	[Serializable] public class PreviewObj 
	{
		public int digIndex; 
		public GameObject obj; 
		public SpriteRenderer render;
	}

public static class DiggerGeneral
{
	public static DigPlot GetDugAtCoordinate(List<DigPlot> digList, Vector2 coordinate)
	{
		//Go through all the dig in given list
		for (int d = 0; d < digList.Count; d++)
		{
			//Return the dug that has the same coordinate as given coordinate
			if(digList[d].coordinate == coordinate) return digList[d];
		}
		return null;
	}
	public static DigPlot GetDugAtPosition(List<DigPlot> digList, Vector2 position)
	{
		//Go through all the dig in given list
		for (int d = 0; d < digList.Count; d++)
		{
			//Return the dug that has the same position as given position
			if(digList[d].position == position) return digList[d];
		}
		return null;
	}
}
} //? close namespace