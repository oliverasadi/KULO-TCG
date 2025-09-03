using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.SceneManagement;

public class MainMenuController : MonoBehaviour
{
    [Header("Initial Selection")]
    public Button defaultSelectedButton;

    void Awake()
    {
        Debug.Log($"[MMC] Awake in scene '{SceneManager.GetActiveScene().name}' (id={GetInstanceID()})");
    }

    void OnEnable()
    {
        Debug.Log($"[MMC] OnEnable in '{SceneManager.GetActiveScene().name}'");
    }

    void Start()
    {
        StartCoroutine(SelectFirstButtonNextFrame());
    }

    void OnDestroy()
    {
        Debug.Log($"[MMC] OnDestroy in '{SceneManager.GetActiveScene().name}' (id={GetInstanceID()})");
    }

    private IEnumerator SelectFirstButtonNextFrame()
    {
        yield return null; // wait a frame so EventSystem/Canvas are ready

        if (EventSystem.current == null)
        {
            Debug.LogWarning("[MMC] No EventSystem found in scene.");
            yield break;
        }

        if (defaultSelectedButton == null)
        {
            // Optional: try find a fallback by tag/name
            var fallback = GameObject.FindWithTag("DefaultMenuButton")?.GetComponent<Button>();
            defaultSelectedButton = fallback;
        }

        if (defaultSelectedButton != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
            EventSystem.current.SetSelectedGameObject(defaultSelectedButton.gameObject);
            Debug.Log($"[MMC] Selected default button: {defaultSelectedButton.name}");
        }
        else
        {
            Debug.LogWarning("[MMC] defaultSelectedButton is not assigned and no fallback found.");
        }
    }
}
