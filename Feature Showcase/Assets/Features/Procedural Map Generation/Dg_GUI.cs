using UnityEngine.UI;
using UnityEngine;
using TMPro;

public class Dg_GUI : MonoBehaviour
{
    public TextMeshProUGUI plotCounter;
	public TMP_InputField amountSetter;
	public ProceduralMapGeneration.Digger.DiggerAlgorithm dg;

	void Start()
	{
		//Set the input field to be digger generator amount
		amountSetter.text = dg.amount.ToString();
	}

	void Update()
	{
		//Stop if digger not generating
		if(!dg.generating) return;
		//Display the amount of plot has create and needed
		plotCounter.text = "Plot: " + dg.plots.Count + "/" + dg.amount;
	}

	//Update digger generator amount to input field amount if changed
	public void SetPlotAmount() {dg.amount = int.Parse(amountSetter.text);}
}
