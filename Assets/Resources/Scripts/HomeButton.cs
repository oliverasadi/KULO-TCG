using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class HomeButton : MonoBehaviour
{
    [Tooltip("Name of the Main Menu scene to load.")]
    public string mainMenuSceneName = "MainMenu";

    void Awake()
    {
        GetComponent<Button>().onClick.AddListener(GoHome);
    }

    private void GoHome()
    {
        // Safety: unpause + clear UI selection before switching scenes
        Time.timeScale = 1f;
        UnityEngine.EventSystems.EventSystem.current?.SetSelectedGameObject(null);

        // Optional: clear any statics you know about
        if (XPResultDataHolder.instance != null) XPResultDataHolder.instance.Clear();

        Debug.Log($"[HomeButton] Loading '{mainMenuSceneName}' in Single mode from '{SceneManager.GetActiveScene().name}'");
        SceneManager.LoadScene(mainMenuSceneName, LoadSceneMode.Single);
    }
}
