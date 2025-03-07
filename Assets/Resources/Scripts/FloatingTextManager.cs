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

    // Instantiates a floating text at a given world position as a child of this manager.
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

    // Instantiates a floating text as a child of the provided parent, with a specified local offset.
    public void ShowFloatingTextAsChild(Transform parent, Vector3 localOffset, string message)
    {
        if (floatingTextPrefab == null)
        {
            Debug.LogError("FloatingText prefab is not assigned!");
            return;
        }
        // Instantiate as child of the given parent.
        GameObject floatingText = Instantiate(floatingTextPrefab, parent);
        // Set its local position to the offset.
        floatingText.transform.localPosition = localOffset;
        TextMeshProUGUI tmp = floatingText.GetComponent<TextMeshProUGUI>();
        if (tmp != null)
            tmp.text = message;
    }
}
