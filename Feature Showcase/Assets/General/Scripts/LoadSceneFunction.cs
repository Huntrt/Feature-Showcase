using UnityEngine.SceneManagement;
using UnityEngine;

public class LoadSceneFunction : MonoBehaviour
{
	//Function that will load the scene at ID given
	public void LoadScene(int ID) {SceneManager.LoadScene(ID, LoadSceneMode.Single);}
}