using UnityEngine;

public class RotatingChainIcon : MonoBehaviour
{
    [Header("Rotation")]
    public float rotationSpeed = 90f;   // degrees per second

    [Header("Optional Pulse (after pop-in)")]
    public float pulseAmplitude = 0.05f;   // 0 = no pulse
    public float pulseFrequency = 3f;      // pulses per second

    [Header("Pop-in")]
    public bool playPopIn = true;
    public float popDuration = 0.15f;      // time to pop in
    public float popOvershoot = 1.2f;      // >1 = small overshoot

    private Vector3 baseScale;
    private float popTimer;

    private void Awake()
    {
        baseScale = transform.localScale;

        if (playPopIn && popDuration > 0f)
        {
            // start from zero scale and animate up
            transform.localScale = Vector3.zero;
            popTimer = popDuration;
        }
        else
        {
            popTimer = 0f;
        }
    }

    private void Update()
    {
        // Rotate around Z (works nicely for UI / RectTransform)
        transform.Rotate(0f, 0f, rotationSpeed * Time.unscaledDeltaTime);

        // POP-IN first
        if (playPopIn && popTimer > 0f)
        {
            popTimer -= Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(1f - (popTimer / popDuration)); // 0 → 1
            // ease-out style overshoot
            float s = Mathf.Lerp(0f, popOvershoot, t);

            // when finished, snap to base scale so pulse can use it cleanly
            if (popTimer <= 0f)
                transform.localScale = baseScale;
            else
                transform.localScale = baseScale * s;

            return; // don’t pulse yet while popping in
        }

        // Small breathing / pulsing scale (optional, after pop-in)
        if (pulseAmplitude > 0f)
        {
            float s = 1f + Mathf.Sin(Time.unscaledTime * pulseFrequency * Mathf.PI * 2f) * pulseAmplitude;
            transform.localScale = baseScale * s;
        }
    }
}
