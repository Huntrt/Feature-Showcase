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
	int overwriteMode; // 0 = none | 1 = start -> target | 2 = start <- target
	Vector2[] overwritePoints;
	public event System.Action onDraw;

	[System.Serializable] 
	public class Spacing {[Range(0,100)] public float min; [Range(0.1f,100)]public float max = 0.1f;}
	[System.Serializable] public class Amplitude {public bool inOrder; public float min; public float max;}

	//Draw upon enable
	void OnEnable() {Draw();}

	void Update()
	{
		//Counting interval counter
		intervalCounter += Time.deltaTime;
		//If interval counter has reach needed amount
		if(intervalCounter >= interval) 
		{
			//Draw the electric effect
			Draw();
			//Reset the interval counter
			intervalCounter -= intervalCounter;
		}
	}

	public void Draw()
	{
		//Save the line renderer then set reset is position count
		LineRenderer line = lineRenderer; line.positionCount = 1;
		//Set the line first position at this object position
		line.SetPosition(0, transform.position);
		//Begin setting up in between point
		SetupPoints(line);
		//Add the line final position to be target position
		line.SetPosition(line.positionCount-1, target);
		//Begin overwrite point
		Overwriting();
		//Call on draw event after finish draw electric
		onDraw?.Invoke();
	}

	void SetupPoints(LineRenderer line)
	{
		//% Clear all the debug object
		for (int t = 0; t < testeds.Count; t++) {Destroy(testeds[t]);} testeds.Clear();
		//Set start position as this object position
		Vector2 start = transform.position;
		//Print an error and stop code if start has the same vector as target
		if(start == target) {Debug.LogWarning("'start' and 'target' can't be the same vector in Electric Line.cs"); return;}
		//Get the distance from start to target
		float total = Vector2.Distance(start, target);
		//Get the euler angle from start to target
		float angle = Mathf.Atan2(target.y - start.y, target.x - start.x) * (180/Mathf.PI);
		//Get direction from start to target 
		Vector2 direction = (target - start).normalized;
		//The amount of distance has occupied, it counter and latest spaced position
		float occupied = 0; int counter = 0; Vector2 spaced = start;
		//While haven't occupied the total distance
		while (occupied <= total)
		{
			//Create new line position and increase counter
			line.positionCount++; counter++;
			//Getting current distance by get randomize spacing percent of total distance
			float distance = (Random.Range(spacing.min, spacing.max) / 100) * total;
			//Get the next spacing position by multiple direction with distance  
			spaced += direction * distance;
			//Has occupied the amount of distance has get
			occupied += distance;
			//Set point position as amplify direction calculated that got multiply with randomize amplitude 
			Vector2 point = spaced + (Amplifying(angle) * Random.Range(amplitude.min, amplitude.max));
			//Set counted line position at point
			line.SetPosition(counter, point);
			//% Debug object instantiate
			testeds.Add(Instantiate(test, point, Quaternion.identity));
		}
		//% Destroy the last test object
		Destroy(testeds[testeds.Count-1]);
	}

	int side; Vector2 Amplifying(float angle)
	{
		//The rotation that will apply to angle
		float rot = 0;
		//If amplitude not in order then radomly choosed if rotation will be 90 or -90
		if(!amplitude.inOrder) {rot = 90; if(Random.Range(0,2) == 0) {rot = -90;} ;}
		//If amplitude are in order
		else 
		{
			//Randomly decide first side will be 1 or 2
			if(side == 0) {side = Random.Range(1,3);}
			//Rotation be 90 if side 1 and -90 if side 2 while cycle between side
			if(side == 1) {side = 2; rot = 90;} else {side = 1; rot = -90;}
		}
		//Convert angle that get increase with rotation to radians
		float radians = (angle + rot) * Mathf.Deg2Rad;
		//Return the direction to amplify by apply cos, sin to radians
		return new Vector2(Mathf.Cos(radians), Mathf.Sin(radians));
	}

	//@ Function that allow for other to overwrite point
	public void OverwriteFromStart(Vector2[] points) {overwritePoints = points; overwriteMode = 1;}
	public void OverwriteFromTarget(Vector2[] points) {overwritePoints = points; overwriteMode = 2;}

	void Overwriting()
	{
		//Don't overwrite anything if mode are 0 or there no point to overwrite
		if(overwriteMode == 0 || overwritePoints.Length == 0) {return;}
		//Get the first index at 1, the last index before target and the amount of point available
		int first = 1; int last = lineRenderer.positionCount-1; int count = lineRenderer.positionCount-3;
		//The overwrite mode current use
		switch(overwriteMode)
		{
			//If 1 then overwrite from start -> target
			case 1:
				//Starting from the first index
				for (int p = first; p < last; p++)
				{
					//Stop if has overwritten all the point
					if(p > overwritePoints.Length) {break;}
					//Set current line position to current overwrite point
					lineRenderer.SetPosition(p, overwritePoints[p-1]);
				}
			break;
			//If 2 then overwrite from target -> start
			case 2:
				//Count the time has overwrite
				int co = 0;
				for (int p = last-1; p >= first ; p--)
				{
					//Stop if has overwritten all the point
					if(co+1 > overwritePoints.Length) {break;}
					//Set current line position to current overwrite point
					lineRenderer.SetPosition(p, overwritePoints[co]);
					//Has overwrite once more
					co++;
				}
			break;
		}
	}
}