using UnityEngine;

public class Keybinding_Testing : MonoBehaviour
{
	[SerializeField] float speed;
	public Vector3 inputDirection;
	public Rigidbody2D Rigidbody;
	KeyManager key;

	//Get the key manager
	void Start() {key = KeyManager.i;}
	
    void Update()
    {
		//Only move when key are nt assigning
		if(!key.areAssigning) MoveInput();
	}

	Vector2 velocity; void MoveInput()
	{
		//Reset input direction
		inputDirection = Vector3.zero;
		//@ Change input direction base on keycode of key manager
		if(Input.GetKey(key.Up)) {inputDirection.y = 1;}
		if(Input.GetKey(key.Down)) {inputDirection.y = -1;}
		if(Input.GetKey(key.Left)) {inputDirection.x = -1;}
		if(Input.GetKey(key.Right)) {inputDirection.x = 1;}
        //Make diagonal movement no longer faster than vertical, horizontal
        velocity = inputDirection.normalized;
        //Add the speed to velocity
        velocity *= speed;
	}

	void FixedUpdate()
	{
		//Moving the player using velocity has get
		Rigidbody.MovePosition(Rigidbody.position + velocity * Time.fixedDeltaTime);
	}
}
