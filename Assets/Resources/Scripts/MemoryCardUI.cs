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

        thumbnail.sprite = data.thumbnail;
        label.text = $"No.{data.memoryNumber}\n{data.memoryTitle}";
        lockedOverlay.SetActive(data.isLocked);

        Button btn = GetComponent<Button>();
        if (btn != null)
        {
            Debug.Log("[MemoryCardUI] Button component found, setting OnClick listener.");
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

        if (memoryData.isLocked)
        {
            Debug.Log("[MemoryCardUI] Memory is locked. Click ignored.");
            return;
        }

        Debug.Log($"[MemoryCardUI] Showing fullscreen memory: {memoryData.memoryTitle}");

        // Ensure the viewer is instantiated
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
