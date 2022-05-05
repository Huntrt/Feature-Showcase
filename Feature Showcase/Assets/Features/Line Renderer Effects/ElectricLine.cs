using UnityEngine;

public class ElectricLine : MonoBehaviour
{
	public GameObject test;
    public LineRenderer lineRenderer;
	[Tooltip("The electric will refresh every second")]
	public float interval; float intervalCounter;
	[Tooltip("The distance between each point")]
	public Distance distance; [System.Serializable] public class Distance {public float min,max;}
	[Tooltip("The chance for an point to added every distance")]
	public float frequency;
	[Tooltip("Does point auto align it distance or will randomly choose")]
	[SerializeField] bool pointAlign;
	[Tooltip("How far the point can move away from it origin")]
	public float width;
	public Vector2 target;

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

		//% Manual refresh
		if(Input.GetKeyDown(KeyCode.Space)) {Refresh();}
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
		line.positionCount++; line.SetPosition(line.positionCount-1, target);
	}

	void SetupPoints(LineRenderer line)
	{
		//Sert start position as this object position
		Vector3 start = transform.position;
		//Record the position for each point
		Vector3 pointPos = start;
		//Get the euler angle from the start to target
		float angle = Mathf.Atan2(target.y - start.y, target.x - start.x) * (180/Mathf.PI);
		//Get the distance between this object and the target
		float total = Vector2.Distance(transform.position, target);
		//The amount of distance has occupied and it counter
		float occupied = 0; int counter = 0;
		//While haven't occupied the total distance
		while (occupied <= total)
		{
			//Increase line position count and counter
			line.positionCount++; counter++;
			//Getting random distance in mix max 
			float dist = Random.Range(distance.min, distance.max);
			//Get the direction that will use to find vector along it
			Vector3 dir = start + Quaternion.AngleAxis(angle, Vector3.forward) * Vector3.right;
			//Get the next point position by multiple direction with distance  
			pointPos += dir * dist;
			//Set counted line position at point 
			line.SetPosition(counter, pointPos);
			Instantiate(test, pointPos, Quaternion.identity);
			//Has occupied the amount of distance has get
			occupied += dist;
		}
	}
}