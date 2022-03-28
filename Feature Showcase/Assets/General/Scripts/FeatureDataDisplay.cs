using UnityEngine.SceneManagement;
using UnityEngine;
using TMPro;

public class FeatureDataDisplay : MonoBehaviour
{
	[SerializeField] GameObject displayPanel;
	[SerializeField] TextMeshProUGUI featureNameDisplay, featureDescriptionDisplay;
	FeatureData.Data current; bool hasDisplay;
	//Turn this class into singleton
    public static FeatureDataDisplay i; void Awake() {i = this;}

	public void DisplayData(FeatureData.Data data)
	{
		//Deactive the old indicator if has display
		if(hasDisplay) {current.indicator.SetActive(false);}
		//Has display
		hasDisplay = true;
		//Save the given data as current data
		current = data;
		//Display the name of current data
		featureNameDisplay.text = current.name;
		//Display the description of current data
		featureDescriptionDisplay.text = current.description;
		//Active the current indicator
		current.indicator.SetActive(true);
		//Active the display panel
		displayPanel.SetActive(true);
	}

	public void CloseDisplay()
	{
		//Dective the current indicator
		current.indicator.SetActive(false);
		//Dective the display panel
		displayPanel.SetActive(false);
	}

	//Load the current scene ID
	public void LoadFeatureScene() {SceneManager.LoadScene(current.sceneId, LoadSceneMode.Single);}
}
