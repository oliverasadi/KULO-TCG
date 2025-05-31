using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class PreviewLinePulse : MonoBehaviour
{
    private Image lineImage;
    private Coroutine pulseRoutine;

    private void Awake()
    {
        lineImage = GetComponent<Image>();
    }

    private void OnEnable()
    {
        StartPulse();
    }

    private void OnDisable()
    {
        StopPulse();
    }

    public void StartPulse()
    {
        if (pulseRoutine != null) StopCoroutine(pulseRoutine);
        pulseRoutine = StartCoroutine(PulseRoutine());
    }

    public void StopPulse()
    {
        if (pulseRoutine != null) StopCoroutine(pulseRoutine);
        pulseRoutine = null;
        if (lineImage != null)
            lineImage.color = new Color(lineImage.color.r, lineImage.color.g, lineImage.color.b, 1f);
    }

    private IEnumerator PulseRoutine()
    {
        while (true)
        {
            float duration = 0.6f;
            float time = 0f;
            while (time < duration)
            {
                float alpha = Mathf.PingPong(time * 2f, 0.5f) + 0.5f; // pulse between 0.5–1 alpha
                if (lineImage != null)
                {
                    Color c = lineImage.color;
                    c.a = alpha;
                    lineImage.color = c;
                }
                time += Time.deltaTime;
                yield return null;
            }
        }
    }
}
