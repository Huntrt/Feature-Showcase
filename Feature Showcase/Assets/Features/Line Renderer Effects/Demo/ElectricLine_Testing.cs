using UnityEngine;

public class ElectricLine_Testing : MonoBehaviour
{
    [SerializeField] ElectricLine electricLine;
	[SerializeField] bool auto;
	Camera cam;

	void Start() {cam = Camera.main;}
	
    void Update()
    {
        if(Input.GetMouseButtonDown(0) && !auto)
		{
			electricLine.enabled = true;
			electricLine.target = cam.ScreenToWorldPoint(Input.mousePosition);
			electricLine.Refresh();
		}
        if(Input.GetMouseButton(0) && auto)
		{
			electricLine.enabled = true;
			electricLine.target = cam.ScreenToWorldPoint(Input.mousePosition);
			electricLine.Refresh();
		}
    }
}
