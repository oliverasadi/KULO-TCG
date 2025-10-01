using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;

[CreateAssetMenu(menuName = "KULO/Prologue Sequence")]
public class PrologueSequence : ScriptableObject
{
    [Header("Global type settings")]
    [Range(10, 120)] public float charsPerSecond = 40f;
    public AudioClip defaultVoiceBed;
    public float defaultPostLineHold = 0.25f;

    [Serializable]
    public class Line
    {
        [TextArea(2, 6)] public string text;
        public AudioClip voice;
        public float postHold = -1f;
    }

    [Serializable]
    public class Slide
    {
        [Header("Visuals")]
        public Sprite image;             // leave null if using video
        public VideoClip video;          // leave null if using image
        public bool loopVideo = true;

        [Header("Narration")]
        public List<Line> lines = new List<Line>();

        [Header("Flow")]
        public bool waitForClickAtEnd = true;
    }

    public List<Slide> slides = new List<Slide>();
}
