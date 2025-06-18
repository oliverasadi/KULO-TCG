using UnityEngine;

public class MuseumButtonResponder : MonoBehaviour
{
    public enum MuseumSection { Memories, Wardrobe, SoundRoom }
    public MuseumSection section;

    [Header("Tam Tam Voice Clips")]
    public AudioClip memoriesClip;
    public AudioClip wardrobeClip;
    public AudioClip soundRoomClip;

    public MemoriesPanelManager memoriesPanelManager; // Assign via inspector


    [Header("Icon Manager")]
    public IconManager iconManager; // 🔗 Drag your IconManager into this in Inspector

    public void OnButtonClick()
    {
        switch (section)
        {
            case MuseumSection.Memories:
                PlayVoice(memoriesClip);
                iconManager?.ShowIcon(IconManager.IconType.Memories);
                break;

            case MuseumSection.Wardrobe:
                PlayVoice(wardrobeClip);
                iconManager?.ShowIcon(IconManager.IconType.Wardrobe);
                break;

            case MuseumSection.SoundRoom:
                PlayVoice(soundRoomClip);
                iconManager?.ShowIcon(IconManager.IconType.Sound);
                break;
        }

        iconManager?.LockIcon(); // 🔒 Lock Tam Tam expression after click
        AudioManager.instance?.PlayButtonClickSound();
        memoriesPanelManager.OpenPanel(); // 👈 Show the new panel
    }

    private void PlayVoice(AudioClip clip)
    {
        if (clip != null)
        {
            AudioManager.instance?.PlaySFX(clip);
        }
        else
        {
            Debug.LogWarning("[Tam Tam] Missing voice clip for: " + section);
        }
    }
}
