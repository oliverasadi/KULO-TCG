using UnityEngine;

public class AvatarSelectButton : MonoBehaviour
{
    public string avatarKey;

    public void OnClick()
    {
        var profile = ProfileManager.instance.currentProfile;
        profile.selectedAvatar = avatarKey;
        ProfileManager.instance.SaveProfile();

        // Refresh UI
        FindObjectOfType<ProfilePanelUI>().RefreshUI();

        // Optional: Close popup if you want
        // FindObjectOfType<ProfilePanelUI>().CloseAvatarPopup();
    }
}
