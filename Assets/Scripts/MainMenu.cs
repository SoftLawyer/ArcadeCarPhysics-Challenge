using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    public void StartChallenge()
    {
        Debug.Log("Starting Challenge...");
        SceneManager.LoadScene("Challenge");
    }
}
