using UnityEngine;
using UnityEngine.SceneManagement;

public class PostGamePopupUI : MonoBehaviour
{
    [Header("Optional XP Screen Reference")]
    public PostGameXPPanel xpPanel;

    public void OnRestartPressed() => GameManager.instance.RestartGame();

    public void OnHomePressed() => SceneManager.LoadScene("MainMenu");

    public void OnQuitPressed() => Application.Quit();

    public void OnChangeCharacterPressed() => SceneManager.LoadScene("CharacterSelectScene");

    public void OnResultsPressed()
    {
        SceneManager.LoadScene("XPResultsScene");
    }
}
