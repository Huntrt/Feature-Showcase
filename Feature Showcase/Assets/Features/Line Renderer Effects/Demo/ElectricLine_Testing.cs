using UnityEngine;

public class ElectricLine_Testing : MonoBehaviour
{
    [SerializeField] ElectricLine electricLine;
	[SerializeField] float speed;
	[SerializeField] bool useOverwrite;
	[SerializeField] Vector2[] overwrite;
	Vector3 inputDirection;
	Camera cam;

	void Start() {cam = Camera.main;}
	
    void Update()
    {
        if(Input.GetMouseButtonDown(0))
		{
			electricLine.OverwriteFromTarget(overwrite);
			electricLine.target = cam.ScreenToWorldPoint(Input.mousePosition);
			electricLine.enabled = true;
		}
        if(Input.GetMouseButton(0))
		{
			electricLine.OverwriteFromTarget(overwrite);
			electricLine.target = cam.ScreenToWorldPoint(Input.mousePosition);
		}
        if(Input.GetMouseButtonUp(0))
		{
			electricLine.enabled = false;
		}
		MoveInput();
    }

	Vector2 velocity; void MoveInput()
	{
		//Set the input horizontal and vertical direction
		inputDirection = new Vector3(Input.GetAxisRaw("Horizontal"),Input.GetAxisRaw("Vertical"),0);
        //Make diagonal movement no longer faster than vertical, horizontal
        velocity = inputDirection.normalized;
        //Add the speed to velocity
        velocity *= speed;
	}

	void FixedUpdate()
	{
		//Moving the player using velocity has get
		transform.position += (Vector3)(velocity * Time.fixedDeltaTime);
	}
}
