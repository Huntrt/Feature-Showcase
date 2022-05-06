using System.Collections.Generic;
using UnityEngine;

public class ElectricLine : MonoBehaviour
{
	//% Debug variable
	public GameObject test; List<GameObject> testeds = new List<GameObject>();
    public LineRenderer lineRenderer;
	[Tooltip("The effect will refresh every second")]
	public float interval; float intervalCounter;
	[Tooltip("How far can the point move away from it initialize position")]
	public Amplitude amplitude;
	[Tooltip("How many percent can each point get of total line")]
	public Spacing spacing = new Spacing();
	public Vector2 target;

	[System.Serializable] 
	public class Spacing {[Range(0,100)] public float min; [Range(0.1f,100)]public float max = 0.1f;}
	[System.Serializable] public class Amplitude {public float min; public float max;}

	void Update()
	{
		// //Counting interval counter
		// intervalCounter += Time.deltaTime;
		// //If interval counter has reach needed amount
		// if(intervalCounter >= interval) 
		// {
		// 	//Refresh the electric effect
		// 	Refresh();
		// 	//Reset the interval counter
		// 	intervalCounter -= intervalCounter;
		// }
	}

	public void Refresh()
	{
		//Save the line renderer then set it first position
		LineRenderer line = lineRenderer; line.positionCount = 1;
		//Set the line first position at this object position
		line.SetPosition(0, transform.position);
		//Begin setting up in between point
		SetupPoints(line);
		//Add the line final position to be target position
		line.SetPosition(line.positionCount-1, target);
	}

	void SetupPoints(LineRenderer line)
	{
		//% Clear all the debug object
		for (int t = 0; t < testeds.Count; t++) {Destroy(testeds[t]);} testeds.Clear();
		//Sert start position as this object position
		Vector3 start = transform.position;
		//Record the latest spacing position
		Vector3 spaced = start;
		//Get the euler angle from the start to target
		float angle = Mathf.Atan2(target.y - start.y, target.x - start.x) * (180/Mathf.PI);
		//Get direction of angle 
		Vector3 direction = GetDirection(angle);
		//Get the distance between this object and the target
		float total = Vector2.Distance(transform.position, target);
		//The amount of distance has occupied and it counter
		float occupied = 0; int counter = 0;
		//While haven't occupied the total distance
		while (occupied <= total)
		{
			//Increase line position count and counter
			line.positionCount++; counter++;
		//? Decide spacing
			//Getting current distance by get randomize spacing percent of total distance
			float distance = (Random.Range(spacing.min, spacing.max) / 100) * total;
			//Get the next spacing position by multiple direction with distance  
			spaced += direction * distance;
			//Has occupied the amount of distance has get
			occupied += distance;
		//? Amplifying
			//! Improve
			float rot = 90; if(Random.Range(0,2) == 0) {rot = -90;}
			//Get the direction for amplifying by rotating angle
			Vector3 amp = GetDirection(angle + rot);
			//Set point position as amplify direction multiply with amplitude amount 
			Vector2 point = spaced + (amp * Random.Range(amplitude.min, amplitude.max));
			// Vector2 point = spaced;
			//Set counted line position at point
			line.SetPosition(counter, point);
			//% Debug object instantiate
			if(occupied+distance < total) testeds.Add(Instantiate(test, point, Quaternion.identity));
		}
	}

	Vector3 GetDirection(float angle)
	{
		//Convert angle to radians
		float radians = angle * Mathf.Deg2Rad;
		//Return the direction by apply cos, sin to radians
		return new Vector3(Mathf.Cos(radians), Mathf.Sin(radians), 0);
	}
}