using ProceduralMapGeneration.Digger;
using UnityEngine.UI;
using UnityEngine;
using TMPro;

namespace ProceduralMapGeneration.Digger
{
public class DiggerGUI : MonoBehaviour
{
    public TextMeshProUGUI plotCounter;
	public TMP_InputField amountSetter;
	public DiggerBuilder_Room builder;
	DiggerConfig config;

	void Start()
	{
		//Set the config to be builder's config
		config = builder.diggerConfig;
		//Set the input field to be digger generator amount
		amountSetter.text = config.amount.ToString();
	}

	void Update()
	{
		//Stop if currently not digging
		if(!config.isDigging) return;
		//Display the amount of plot has dig and the amount need to dig
		plotCounter.text = "Plot: " + config.dugs.Count + "/" + config.amount;
	}

	//Update digger generator amount to input field amount if changed
	public void SetPlotAmount() {config.amount = int.Parse(amountSetter.text);}
}
} //? Close namespace