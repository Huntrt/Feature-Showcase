using System.Collections.Generic;
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
		public static PlotData GetPlotAtCoordinate(List<PlotData> plotList, Vector2 findCoordinate)
		{
			//Go through all the plot inside list given
			for (int p = 0; p < plotList.Count; p++)
			{
				//Return the plot at the same coordinate as coordinate need to find
				if(plotList[p].coordinate == findCoordinate) {return plotList[p];}
			}
			return null;
		}

		public static PlotData GetPlotAtPosition(List<PlotData> plotList, Vector2 findPosition)
		{
			//Go through all the plot inside list given
			for (int p = 0; p < plotList.Count; p++)
			{
				//Return the plot at the same position as position need to find
				if(plotList[p].position == findPosition) {return plotList[p];}
			}
			return null;
		}
	}
}
