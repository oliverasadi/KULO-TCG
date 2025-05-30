using UnityEngine;
using UnityEngine.SceneManagement;

public class PostGamePopupUI : MonoBehaviour
{
    public void OnRestartPressed() => GameManager.instance.RestartGame();
    public void OnHomePressed() => SceneManager.LoadScene("MainMenu");
    public void OnQuitPressed() => Application.Quit();
    public void OnChangeCharacterPressed() => SceneManager.LoadScene("CharacterSelectScene");
}
