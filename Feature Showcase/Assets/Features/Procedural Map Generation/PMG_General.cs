using UnityEngine;
using System;

namespace ProceduralMapGeneration
{
	[Serializable] public class PlotData
	{
		public int index = -1;
		public Vector2 coordinate;
		public Vector2 position;
		public bool empty = true;
	}
}
