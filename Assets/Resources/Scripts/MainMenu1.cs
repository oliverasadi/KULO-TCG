using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

public class MainMenu1 : MonoBehaviour // ✅ Must inherit from MonoBehaviour
{
    public void PlayGame()
    {
        AutoFade.LoadScene("CharacterSelectScene", 0.3f, 0.3f, Color.black); // ✅ Ensure "GameScene" exists in Build Settings
    }
    public void OpenDeckEditor()
    {
        AutoFade.LoadScene("DeckEditorScene"); // ✅ Load deck editor scene
    }
    public void OpenMuseum()
    {
        AutoFade.LoadScene("MuseumMenu"); // ✅ Load Museum Menu
    }
    public void OpenHelp()
    {
        AutoFade.LoadScene("HelpScene"); // Replace with your actual help scene name
    }

    public void ExitGame()
    {
        Debug.Log("Game is quitting...");
        Application.Quit(); // ✅ Works in built application, not in the Unity Editor
    }
}