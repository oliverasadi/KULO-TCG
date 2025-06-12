using UnityEngine;
using System;

[Serializable]
public class CharacterData
{
    public string characterName;
    public Sprite characterThumbnail;
    public Sprite fullArtImage;
    public string description;
    public AudioClip voiceLine;
    public DeckDataSO assignedDeck; // or remove if not using decks yet
}
