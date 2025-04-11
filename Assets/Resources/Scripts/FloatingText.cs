using UnityEngine;
using TMPro;


public class FloatingText : MonoBehaviour
{
    public TextMeshProUGUI textComponent;
    public float duration = 100f; // Debug duration; adjust as needed.
    public Vector3 floatSpeed = new Vector3(0, 0f, 0);
    private float timer = 0f;
    private Color originalColor;

    // Reference to the card this floating text is associated with.
    public GameObject sourceCard;

    private float lastUpdateTime = 0f;
    public float updateInterval = 0.5f;  // Only update every 0.5 seconds (you can tweak this value)
    private string lastDisplayedPower = ""; // Store the last displayed power text

    void Start()
    {
        if (textComponent == null)
            textComponent = GetComponent<TextMeshProUGUI>();

        originalColor = textComponent.color;
        // Ensure alpha is fully opaque at start.
        textComponent.color = new Color(originalColor.r, originalColor.g, originalColor.b, 1f);
        lastUpdateTime = Time.time; // Set the start time for update interval
        lastDisplayedPower = textComponent.text; // Store the initial value of the floating text
        Debug.Log("FloatingText instantiated at: " + transform.position);
    }

    void Update()
    {
        timer += Time.deltaTime;
        // Move upward gradually.
        transform.position += floatSpeed * Time.deltaTime;

        // Update floating text only every `updateInterval` seconds
        if (Time.time - lastUpdateTime >= updateInterval)
        {
            UpdateFloatingText();  // Update the floating text
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
        if (sourceCard != null)
        {
            CardUI cardUI = sourceCard.GetComponent<CardUI>();
            if (cardUI != null)
            {
                int newPower = cardUI.CalculateEffectivePower();  // Calculate the updated power from CardUI
                string newPowerText = "Power: " + newPower;  // Formatted power text for floating text

                // Only update floating text if the power text has changed
                if (textComponent.text != newPowerText)
                {
                    textComponent.text = newPowerText;  // Update the floating text to show the new power
                }

                // Only update the Card Info Panel if the power has changed (prevents back-and-forth updates)
                if (cardUI.cardInfoPanel != null && cardUI.cardInfoPanel.cardPowerText.text != newPowerText)
                {
                    // Check if the card in FloatingText is the same as the one in CardInfoPanel
                    if (cardUI.cardInfoPanel.CurrentCardUI != null && cardUI.cardInfoPanel.CurrentCardUI.cardData == cardUI.cardData)
                    {
                        Debug.Log("Updating Card Info Panel power text to: " + newPower);
                        cardUI.cardInfoPanel.cardPowerText.text = newPower.ToString();  // Directly set the Card Info Panel's power text
                    }
                }
            }
        }
    }
}
