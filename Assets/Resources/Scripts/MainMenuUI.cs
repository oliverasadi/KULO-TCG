using UnityEngine;

public class MainMenuUI : MonoBehaviour
{
    [Header("UI Panels")]
    public GameObject profilePanel;

    /// <summary>
    /// Show the profile panel.
    /// Hook this to your "View Profile" button.
    /// </summary>
    public void ShowProfilePanel()
    {
        if (profilePanel != null)
        {
            profilePanel.SetActive(true);

            // Also refresh profile UI if component is found
            ProfilePanelUI ui = profilePanel.GetComponent<ProfilePanelUI>();
            if (ui != null)
            {
                ui.RefreshUI();
            }
        }
        else
        {
            Debug.LogWarning("profilePanel not assigned in MainMenuUI.");
        }
    }

    /// <summary>
    /// Hide the profile panel.
    /// Hook this to your "Close" button on the panel.
    /// </summary>
    public void HideProfilePanel()
    {
        if (profilePanel != null)
        {
            profilePanel.SetActive(false);
        }
    }
}
