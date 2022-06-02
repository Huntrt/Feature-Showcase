using UnityEngine;
using System;

public class FeatureData : MonoBehaviour
{
	[TextArea(10,100)] public string description;
	public int sceneId;
	public GameObject indicator;

	//Send data of this feature to display when click it button
	public void SendData()  {FeatureDataDisplay.i.DisplayData(this);}
}