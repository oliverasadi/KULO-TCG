using UnityEngine;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    [Header("UI Panel Prefabs")]
    public GameObject graveyardDisplayPanelPrefab;

    [Header("UI Containers")]
    public Transform overlayCanvas; // Assign your main canvas in the inspector

    private GraveyardDisplayManager graveyardPanelInstance;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        if (overlayCanvas == null)
        {
            overlayCanvas = GameObject.Find("OverlayCanvas")?.transform;
            if (overlayCanvas == null)
                Debug.LogError("UIManager: Could not find OverlayCanvas in scene!");
        }
    }

    public void ShowGraveyardPanel(System.Collections.Generic.List<GameObject> graveyardCards)
    {
        if (graveyardPanelInstance == null)
        {
            GameObject panel = Instantiate(graveyardDisplayPanelPrefab, overlayCanvas);
            graveyardPanelInstance = panel.GetComponent<GraveyardDisplayManager>();

            if (graveyardPanelInstance == null)
            {
                Debug.LogError("UIManager: GraveyardDisplayManager component missing on prefab!");
                return;
            }
        }

        graveyardPanelInstance.ShowGraveyard(graveyardCards);
    }
}
