using UnityEngine;

public class ElectricLine_Testing : MonoBehaviour
{
    [SerializeField] ElectricLine electricLine;
	Camera cam;

	void Start() {cam = Camera.main;}
	
    void Update()
    {
        if(Input.GetMouseButtonDown(0))
		{
			electricLine.enabled = true;
			electricLine.target = cam.ScreenToWorldPoint(Input.mousePosition);
			electricLine.Refresh();
			return;
		}
    }
}
