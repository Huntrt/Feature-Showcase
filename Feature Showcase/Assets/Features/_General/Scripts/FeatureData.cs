using UnityEngine;
using System;

public class FeatureData : MonoBehaviour
{
	[Serializable] public class Data 
	{
		public string name;
		[TextArea(1,100)] public string description;
		public int sceneId;
		public GameObject indicator;
	}
	[SerializeField] Data data;

	//Send data of this feature to display when click it button
	public void SendData()  {FeatureDataDisplay.i.DisplayData(data);}
}