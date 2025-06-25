using UnityEngine;
using TMPro;

public class FloatingTextManager : MonoBehaviour
{
    public static FloatingTextManager instance;
    public GameObject floatingTextPrefab;  // Assign your FloatingText prefab in the Inspector

    void Awake()
    {
        if (instance == null)
            instance = this;
        else
            Destroy(gameObject);
    }

    // ─────────────────────────────────────────────────────
    // 1. Standard: World position, auto-parented to manager
    // ─────────────────────────────────────────────────────
    public void ShowFloatingText(Vector3 position, string message)
    {
        if (floatingTextPrefab == null)
        {
            Debug.LogError("FloatingText prefab is not assigned!");
            return;
        }
        GameObject floatingText = Instantiate(floatingTextPrefab, position, Quaternion.identity, transform);
        TextMeshProUGUI tmp = floatingText.GetComponent<TextMeshProUGUI>();
        if (tmp != null)
            tmp.text = message;
    }

    // ─────────────────────────────────────────────────────
    // 2. Child of Transform, optional duration
    // ─────────────────────────────────────────────────────
    public void ShowFloatingTextAsChild(Transform parent, Vector3 localOffset, string message, float durationOverride = -1f)
    {
        if (floatingTextPrefab == null)
        {
            Debug.LogError("FloatingText prefab is not assigned!");
            return;
        }

        GameObject floatingText = Instantiate(floatingTextPrefab, parent);
        floatingText.transform.localPosition = localOffset;

        TextMeshProUGUI tmp = floatingText.GetComponent<TextMeshProUGUI>();
        if (tmp != null) tmp.text = message;

        FloatingText ft = floatingText.GetComponent<FloatingText>();
        if (ft != null && durationOverride > 0f)
            ft.duration = durationOverride;
    }

    // ─────────────────────────────────────────────────────
    // 3. World-space UI, colorized, auto-destroys
    // ─────────────────────────────────────────────────────
    public void ShowFloatingTextWorld(string message, Vector3 worldPosition, Color color)
    {
        if (floatingTextPrefab == null)
        {
            Debug.LogError("FloatingText prefab is not assigned!");
            return;
        }

        GameObject floatingText = Instantiate(floatingTextPrefab, worldPosition, Quaternion.identity);

        Canvas canvas = floatingText.GetComponentInParent<Canvas>();
        if (canvas != null)
            canvas.renderMode = RenderMode.WorldSpace;

        TextMeshProUGUI tmp = floatingText.GetComponent<TextMeshProUGUI>();
        if (tmp != null)
        {
            tmp.text = message;
            tmp.color = color;
        }

        Destroy(floatingText, 1.5f);
    }
}
