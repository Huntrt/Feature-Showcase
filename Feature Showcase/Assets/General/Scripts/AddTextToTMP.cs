using UnityEngine;
using TMPro;

public class AddTextToTMP : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI display;
	[SerializeField] TMP_InputField field;

	void Update()
	{
		//If press enter while input field has been edit
		if(field.text != "" && Input.GetKeyDown(KeyCode.Return))
		{
			//Replace the display text to be field text
			if(display.text == "") {display.text = field.text;}
			//Add field text to display
			else {display.text = display.text + "\n" + field.text;} 
			//Clear field text
			field.text = "";
		}
		//Focus the input field if press enter while input field are not focus
		if(!field.isFocused && Input.GetKeyDown(KeyCode.Return)) {field.ActivateInputField();}
	}

	//Clear text display
	public void Clear() {display.text = "";}
}
