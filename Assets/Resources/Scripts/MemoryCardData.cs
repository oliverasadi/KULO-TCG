using UnityEngine;

[CreateAssetMenu(fileName = "NewMemoryCard", menuName = "KULO/Museum Memory Card")]
public class MemoryCardData : ScriptableObject
{
    public int memoryNumber;
    public string memoryTitle;
    public Sprite thumbnail;
    public bool isLocked = false;
}
