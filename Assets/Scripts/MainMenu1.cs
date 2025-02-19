using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu1 : MonoBehaviour // ✅ Must inherit from MonoBehaviour
{
    public void PlayGame()
    {
        AutoFade.LoadScene("KULO", 0.3f, 0.3f, Color.black); // ✅ Ensure "GameScene" exists in Build Settings
    }

    public void ExitGame()
    {
        Debug.Log("Game is quitting...");
        Application.Quit(); // ✅ Works in built application, not in the Unity Editor
    }
}