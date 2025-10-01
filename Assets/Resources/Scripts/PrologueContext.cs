using UnityEngine;

public static class PrologueContext
{
    public static PrologueSequence sequence;
    public static DeckDataSO deck;
    public static AudioClip voiceLineAfter;   // optional: play after prologue, before gameplay
    public static string characterName;

    public static void Set(PrologueSequence seq, DeckDataSO d, AudioClip voice, string charName)
    {
        sequence = seq;
        deck = d;
        voiceLineAfter = voice;
        characterName = charName;
    }

    public static void Clear()
    {
        sequence = null;
        deck = null;
        voiceLineAfter = null;
        characterName = null;
    }

    public static bool IsValid => sequence != null;
}
