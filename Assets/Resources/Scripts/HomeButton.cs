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
        GetComponent<Button>().onClick.AddListener(() =>
        {
            if (XPResultDataHolder.instance != null)
                XPResultDataHolder.instance.Clear(); // Optional clean-up

            SceneManager.LoadScene(mainMenuSceneName);
        });
    }
}
