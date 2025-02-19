using UnityEngine;

public class ExitGame : MonoBehaviour
{
    public void QuitGame()
    {
        Debug.Log("Game is exiting..."); // ✅ Logs exit in Unity Editor
        Application.Quit(); // ✅ Closes the game in a built version
    }
}
