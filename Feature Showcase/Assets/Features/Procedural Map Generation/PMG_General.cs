using UnityEngine;
using System;

namespace ProceduralMapGeneration
{
	[Serializable] public class PlotData
	{
		public Vector2 coordinate;
		public Vector2 position;
		public bool empty = true;
	} 

	[Serializable] public class PlotWithNeighbors
	{
		public int emptyNeighbor = 4;
		public PlotData[] neighbors = new PlotData[4]
		{
			new PlotData(),
			new PlotData(),
			new PlotData(),
			new PlotData()
		};
	} 

	public static class PMG_General
	{
		
	}
}
