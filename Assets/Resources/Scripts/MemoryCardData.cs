using UnityEngine;

[CreateAssetMenu(fileName = "NewMemoryCard", menuName = "KULO/Museum Memory")]
public class MemoryCardData : ScriptableObject
{
    [Header("Metadata")]
    public string memoryTitle;
    public int memoryNumber;
    public bool isLocked = true;

    [Header("Visuals")]
    public Sprite thumbnail;
    public Sprite fullscreenImage;

    [Header("Unique ID")]
    public string cardID; // 🆕 This is used for unlocking persistence
}
