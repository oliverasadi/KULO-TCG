using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MainMenuSelector : MonoBehaviour
{
    public Button[] menuButtons;

    void Start()
    {
        if (menuButtons != null && menuButtons.Length > 0)
        {
            EventSystem.current.SetSelectedGameObject(menuButtons[0].gameObject);
        }
    }
}
