using UnityEngine.UI;
using UnityEngine;
using System;
using TMPro;

public class CameraController : MonoBehaviour
{
	public Camera cam;
	public float moveSpeed;
	public ZoomSetting zoom;
	Vector2 inputDirection;
	[Header("Indicator Setting")]
	public bool useIndicator;
	public RectTransform moveIndicator;
	public Image moveIndicatorImg;
	public Color indicatorOff, indicatorOn;
	public TextMeshProUGUI zoomCounter;
	[Serializable] public class ZoomSetting {public float speed,reset,max,min;}
	//Set this class to singleton
	public static CameraController i {get{if(_i==null){_i = GameObject.FindObjectOfType<CameraController>();}return _i;}} static CameraController _i;

    void Update()
    {
		Movement(); Zoom();
		DisplayIndicator();
    }

	void Movement()
	{
		//Get the input horizontal and vertical direction
		inputDirection = new Vector3(Input.GetAxisRaw("Horizontal"),Input.GetAxisRaw("Vertical"),0);
		//Stop if there no input direction 
		if(inputDirection == Vector2.zero) return;
		//Save the cam transform
		Transform camTF = cam.transform;
		//Moving the cam position using input direction multiply with speed
		camTF.position = (Vector2)camTF.position + inputDirection.normalized * (moveSpeed * Time.deltaTime);
		//Reset camera position Z back to -10
		camTF.position = new Vector3(camTF.position.x, camTF.position.y, -10);
	}

	void Zoom()
	{
		//Stop if the mouse are not scrolling
		if(Input.mouseScrollDelta.y == 0) return;
		//Increase or decrease the orthographic size with scrool amount and zoom speed
		cam.orthographicSize -= Input.mouseScrollDelta.y * (zoom.speed * Time.deltaTime);
		//Clamming the cam zooming value
		cam.orthographicSize = Mathf.Clamp(cam.orthographicSize, zoom.min, zoom.max);
	}
	public void ResetZoom() {cam.orthographicSize = zoom.reset;}

	void DisplayIndicator()
	{
		//Stop if don't need indicator
		if(!useIndicator) return;
		//Display the zoom counter as cam orthographic size round up
		zoomCounter.text = Math.Round(cam.orthographicSize,1).ToString();
		//Set move indicator color to off
		moveIndicatorImg.color = indicatorOff;
		//Stoff if there no input direction
		if(inputDirection == Vector2.zero) return;
		//Get the position theat been increase by input
		Vector2 next = (Vector2)transform.position + inputDirection;
		//Rotate indicator towrd the next position
		moveIndicator.up = next - (Vector2)transform.position;
		//Set move indicator color to on
		moveIndicatorImg.color = indicatorOn;
	}
}