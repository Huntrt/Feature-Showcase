using UnityEngine.UI;
using UnityEngine;
using TMPro;

public class Dg_GUI : MonoBehaviour
{
    public TextMeshProUGUI plotCounter;
	public Image plotProgress;
	public DiggerGeneration dg;

	void Update()
	{
		//Stop if digger not generating
		if(!dg.generating) return;
		//Update the plot progress bar between created and needed plot
		plotProgress.fillAmount = (float)dg.plots.Count/ (float)dg.amount;
		//Display the amount of plot has create and needed
		plotCounter.text = "Plot: " + dg.plots.Count + "/" + dg.amount;
	}
}
