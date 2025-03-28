using UnityEngine;
using TMPro;

public class FloatingText : MonoBehaviour
{
    public TextMeshProUGUI textComponent;
    public float duration = 100f; // Debug duration; adjust as needed.
    public Vector3 floatSpeed = new Vector3(0, 0f, 0);
    private float timer = 0f;
    private Color originalColor;

    // NEW: Reference to the card this floating text is associated with.
    public GameObject sourceCard;

    private float updateInterval = 2f; // Update interval in seconds
    private float lastUpdateTime;

    void Start()
    {
        if (textComponent == null)
            textComponent = GetComponent<TextMeshProUGUI>();

        originalColor = textComponent.color;
        // Ensure alpha is fully opaque at start.
        textComponent.color = new Color(originalColor.r, originalColor.g, originalColor.b, 1f);
        lastUpdateTime = Time.time; // Set the start time for update interval
        Debug.Log("FloatingText instantiated at: " + transform.position);
    }

    void Update()
    {
        timer += Time.deltaTime;
        // Move upward gradually.
        transform.position += floatSpeed * Time.deltaTime;

        // Update floating text only every 2 seconds
        if (Time.time - lastUpdateTime >= updateInterval)
        {
            UpdateFloatingText(); // Update the floating text
            lastUpdateTime = Time.time; // Reset the time tracker
        }

        // Fade out gradually over the duration.
        float alpha = Mathf.Lerp(1f, 0f, timer / duration);
        textComponent.color = new Color(originalColor.r, originalColor.g, originalColor.b, alpha);

        if (timer >= duration)
        {
            Destroy(gameObject);
        }
    }

    void UpdateFloatingText()
    {
        // If sourceCard is assigned, update the text dynamically.
        if (sourceCard != null)
        {
            CardUI cardUI = sourceCard.GetComponent<CardUI>();
            if (cardUI != null)
            {
                textComponent.text = "Power: " + cardUI.CalculateEffectivePower();
            }
        }

        // Debug log for testing purposes
        Debug.Log("Floating Text Updated");
    }
}
