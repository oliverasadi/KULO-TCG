using UnityEngine;

[CreateAssetMenu(menuName = "CutIn/CutInData")]
public class CutInData : ScriptableObject
{
    public string cutInName;
    public Sprite characterPortrait;
    public Sprite extraGraphic;
    public AudioClip cutInSFX;
    public string topText;
    public string bottomText;
    public Color accentColor;
}
