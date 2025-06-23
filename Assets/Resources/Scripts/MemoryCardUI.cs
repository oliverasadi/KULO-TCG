using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MemoryCardUI : MonoBehaviour
{
    public Image thumbnail;
    public TMP_Text label;
    public GameObject lockedOverlay;

    private MemoryCardData memoryData;

    public void Setup(MemoryCardData data)
    {
        memoryData = data;
        Debug.Log($"[MemoryCardUI] Setup called for: {data.memoryTitle}");

        // Check unlock status from profile instead of relying on SO value
        bool isUnlocked = false;
        var profile = ProfileManager.instance?.currentProfile;
        if (profile != null && profile.unlockedMemories.Contains(data.cardID))
        {
            isUnlocked = true;
        }

        // Update visuals
        thumbnail.sprite = data.thumbnail;
        label.text = $"No.{data.memoryNumber}\n{data.memoryTitle}";
        lockedOverlay.SetActive(!isUnlocked);

        Button btn = GetComponent<Button>();
        if (btn != null)
        {
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(OnClick);
        }
        else
        {
            Debug.LogWarning("[MemoryCardUI] No Button component found on prefab!");
        }
    }

    private void OnClick()
    {
        Debug.Log("[MemoryCardUI] OnClick called.");

        if (memoryData == null)
        {
            Debug.LogError("[MemoryCardUI] memoryData is NULL!");
            return;
        }

        var profile = ProfileManager.instance?.currentProfile;
        if (profile == null || !profile.unlockedMemories.Contains(memoryData.cardID))
        {
            Debug.Log("[MemoryCardUI] Memory is locked. Click ignored.");
            return;
        }

        Debug.Log($"[MemoryCardUI] Showing fullscreen memory: {memoryData.memoryTitle}");

        FullscreenMemoryViewer.EnsureInstance();

        if (FullscreenMemoryViewer.instance != null)
        {
            FullscreenMemoryViewer.instance.Show(memoryData.thumbnail);
        }
        else
        {
            Debug.LogError("[MemoryCardUI] FullscreenMemoryViewer.instance is STILL NULL!");
        }
    }
}
