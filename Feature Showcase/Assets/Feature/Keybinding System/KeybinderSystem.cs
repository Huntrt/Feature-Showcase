using System.Collections;
using UnityEngine;
using TMPro;

public class KeybinderSystem : MonoBehaviour
{
	[SerializeField] bool DDOL;
	[Tooltip("Destroy object that are not DontDestroyOnLoad")] 
	public bool areAssigning;
	[SerializeField] string waitingMessage;
	[SerializeField] string assignAction;
    TextMeshProUGUI assignDisplay;
	public static KeybinderSystem i;

	///Insert more key here...
	public KeyCode Move, Jump, Attack;

	void Awake()
	{
		//If haven't set this component as singleton
		if(i == null)
		{
			//Set this component as singleton
			i = this;
			//Don't destroy on load this if needed
			if(DDOL) {DontDestroyOnLoad(this);}
		}
		//If already set singelton
		else
		{
			//Destroy the duplicate of this object if don't destroy on load enable
			if(DDOL) {Destroy(gameObject);}
		}
	}

	public void StartAssign(KeySetter setter)
	{
		//Get the action of the setter given
		assignAction = setter.action;
		//Get the display of the setter given
		assignDisplay = setter.keyDisplay;
		//Are now begin to assign then start it coroutine
		areAssigning = true; StartCoroutine("Assigning");
	}
	
	IEnumerator Assigning()
	{
		//If currently assigning
		while(areAssigning)
		{
			//Change the assign display to waititng message
			assignDisplay.text = waitingMessage;
            //Go though all the key to check if there is currently any input
            foreach(KeyCode pressedKey in System.Enum.GetValues(typeof(KeyCode)))
			{
				//If there is an input from any keycode and there is assign action
				if(Input.GetKey(pressedKey) && this.GetType().GetField(assignAction) != null)
				{
					//Change keycode variable that has same name as assign action to pressed keycode
					this.GetType().GetField(assignAction).SetValue(this, pressedKey);
					//Change the assign display text to pressed keycode
					assignDisplay.text = pressedKey.ToString();
					//Stop assigning
					areAssigning = false;
				}
			}
			//If no longer assigning
			if(!areAssigning)
			{
				//Remove assign action
				assignAction = "None";
				//Remove assign display
				assignDisplay = null;
			}
			yield return null;
		}
	}
}
