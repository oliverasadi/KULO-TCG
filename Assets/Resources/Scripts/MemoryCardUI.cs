using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MemoryCardUI : MonoBehaviour
{
    public Image thumbnail;
    public TMP_Text label;
    public GameObject lockedOverlay;

    public void Setup(MemoryCardData data)
    {
        thumbnail.sprite = data.thumbnail;
        label.text = $"No.{data.memoryNumber}\n{data.memoryTitle}";
        lockedOverlay.SetActive(data.isLocked);
    }
}
