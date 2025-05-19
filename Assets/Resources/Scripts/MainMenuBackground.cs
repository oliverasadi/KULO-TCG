using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class MenuBackgroundPair
{
    public Sprite backgroundImage;
    public Sprite foregroundImage;

    [Header("Foreground Layout Overrides")]
    public Vector2 anchoredPosition;
    public Vector2 sizeDelta;
    public Vector3 scale;

    [Header("Drift Settings")]
    public float driftOffsetX = 0f;
    public float driftAmplitude = 20f;
    public float driftSpeed = 0.5f;
}


public class MainMenuBackground : MonoBehaviour
{
    [Header("UI References")]
    public Image backgroundTarget;
    public Image foregroundTarget;
    public CanvasGroup backgroundCanvasGroup;
    public CanvasGroup foregroundCanvasGroup;

    [Header("Slideshow Settings")]
    public List<MenuBackgroundPair> backgroundPairs;
    public float switchInterval = 8f;
    public float fadeDuration = 1f;
    public bool loop = true;

    private List<int> shuffledIndices = new List<int>();
    private int currentIndex = 0;

    void Start()
    {
        if (backgroundPairs == null || backgroundPairs.Count == 0)
        {
            Debug.LogWarning("No background pairs assigned.");
            return;
        }

        if (backgroundCanvasGroup == null) backgroundCanvasGroup = backgroundTarget.GetComponent<CanvasGroup>();
        if (foregroundCanvasGroup == null) foregroundCanvasGroup = foregroundTarget.GetComponent<CanvasGroup>();

        ShuffleIndices();
        ApplyPair(shuffledIndices[currentIndex]);
        StartCoroutine(CyclePairs());
    }

    IEnumerator CyclePairs()
    {
        while (true)
        {
            yield return new WaitForSeconds(switchInterval);

            currentIndex++;

            if (currentIndex >= shuffledIndices.Count)
            {
                if (loop)
                {
                    ShuffleIndices();
                    currentIndex = 0;
                }
                else
                {
                    yield break;
                }
            }

            yield return StartCoroutine(FadeOut());
            ApplyPair(shuffledIndices[currentIndex]);
            yield return StartCoroutine(FadeIn());
        }
    }

    void ApplyPair(int i)
    {
        MenuBackgroundPair pair = backgroundPairs[i];

        if (backgroundTarget != null)
            backgroundTarget.sprite = pair.backgroundImage;

        if (foregroundTarget != null)
        {
            foregroundTarget.sprite = pair.foregroundImage;

            bool isFirstFrame = Time.frameCount <= 1;

            if (isFirstFrame)
            {
                // 🧠 Apply layout immediately on first frame — no coroutine, no flicker
                ApplyRectTransformImmediately(pair);
            }
            else
            {
                foregroundTarget.enabled = false;
                StartCoroutine(ApplyRectTransformWithDelay(pair));
            }
        }
    }

    void ApplyRectTransformImmediately(MenuBackgroundPair pair)
    {
        RectTransform rt = foregroundTarget.rectTransform;

        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);

        rt.anchoredPosition = pair.anchoredPosition;
        rt.sizeDelta = pair.sizeDelta;
        rt.localScale = pair.scale;

        // Drift setup
        UIDrift drift = foregroundTarget.GetComponent<UIDrift>();
        if (drift != null)
        {
            Vector2 start = pair.anchoredPosition;
            start.x += pair.driftOffsetX;
            drift.SetBasePosition(start);
            drift.amplitude = pair.driftAmplitude;
            drift.speed = pair.driftSpeed;
        }
    }



    IEnumerator ApplyRectTransformWithDelay(MenuBackgroundPair pair)
    {
        yield return null;
        yield return null;

        RectTransform rt = foregroundTarget.rectTransform;

        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);

        rt.anchoredPosition = pair.anchoredPosition;
        rt.sizeDelta = pair.sizeDelta;
        rt.localScale = pair.scale;

        // Apply drift values
        UIDrift drift = foregroundTarget.GetComponent<UIDrift>();
        if (drift != null)
        {
            Vector2 start = pair.anchoredPosition;
            start.x += pair.driftOffsetX;
            drift.SetBasePosition(start);
            drift.amplitude = pair.driftAmplitude;
            drift.speed = pair.driftSpeed;
        }

        foregroundTarget.enabled = true; // ✅ enable now that layout is ready

        Debug.Log($"✔ Foreground layout set: pos={rt.anchoredPosition}, size={rt.sizeDelta}, scale={rt.localScale}");
    }


    IEnumerator FadeOut()
    {
        float t = 0f;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            float alpha = 1 - (t / fadeDuration);
            if (backgroundCanvasGroup != null) backgroundCanvasGroup.alpha = alpha;
            if (foregroundCanvasGroup != null) foregroundCanvasGroup.alpha = alpha;
            yield return null;
        }
    }

    IEnumerator FadeIn()
    {
        float t = 0f;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            float alpha = t / fadeDuration;
            if (backgroundCanvasGroup != null) backgroundCanvasGroup.alpha = alpha;
            if (foregroundCanvasGroup != null) foregroundCanvasGroup.alpha = alpha;
            yield return null;
        }
    }

    void ShuffleIndices()
    {
        shuffledIndices.Clear();
        for (int i = 0; i < backgroundPairs.Count; i++)
            shuffledIndices.Add(i);

        for (int i = 0; i < shuffledIndices.Count; i++)
        {
            int rand = Random.Range(i, shuffledIndices.Count);
            (shuffledIndices[i], shuffledIndices[rand]) = (shuffledIndices[rand], shuffledIndices[i]);
        }
    }
}
