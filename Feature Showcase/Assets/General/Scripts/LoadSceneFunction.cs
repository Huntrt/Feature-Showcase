using UnityEngine.SceneManagement;
using UnityEngine;

[SerializeField] class LoadSceneFunction : MonoBehaviour
{
	//Function that will load the scene at ID given
	public void LoadScene(int ID) {SceneManager.LoadScene(ID, LoadSceneMode.Single);}
}