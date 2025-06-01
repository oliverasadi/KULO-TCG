using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections;

public class MainMenuController : MonoBehaviour
{
    [Header("Initial Selection")]
    public Button defaultSelectedButton;

    void Start()
    {
        StartCoroutine(SelectFirstButtonNextFrame());
    }

    private IEnumerator SelectFirstButtonNextFrame()
    {
        yield return null; // Wait one frame
        EventSystem.current.SetSelectedGameObject(null); // Clear any previous selection
        EventSystem.current.SetSelectedGameObject(defaultSelectedButton.gameObject); // Focus the first button
    }
}
