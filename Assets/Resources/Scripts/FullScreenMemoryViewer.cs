using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class FullscreenMemoryViewer : MonoBehaviour
{
    public static FullscreenMemoryViewer instance;

    [Header("UI Elements")]
    public GameObject rootPanel;
    public Image displayImage;
    public Image dimWall; // translucent black wall behind image (optional)

    void Awake()
    {
        if (instance == null)
        {
            instance = this;

            // Attach to Canvas if needed
            if (transform.parent == null || GetComponentInParent<Canvas>() == null)
            {
                Canvas canvas = FindObjectOfType<Canvas>();
                if (canvas != null)
                {
                    transform.SetParent(canvas.transform, false);
                    Debug.Log("[FullscreenMemoryViewer] Attached to Canvas.");
                }
                else
                {
                    Debug.LogError("[FullscreenMemoryViewer] No Canvas found!");
                }
            }

            if (rootPanel == null && transform.childCount > 0)
            {
                rootPanel = transform.GetChild(0).gameObject;
                Debug.Log("[FullscreenMemoryViewer] Auto-assigned rootPanel.");
            }

            rootPanel?.SetActive(false);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void Show(Sprite image)
    {
        if (displayImage == null)
        {
            displayImage = GetComponentInChildren<Image>();
            Debug.LogWarning("[FullscreenMemoryViewer] displayImage auto-assigned.");
        }

        if (rootPanel != null)
        {
            rootPanel.SetActive(true);
            displayImage.sprite = image;

            // Reset states
            rootPanel.transform.localScale = Vector3.one * 0.9f;
            CanvasGroup cg = rootPanel.GetComponent<CanvasGroup>();
            if (cg == null) cg = rootPanel.AddComponent<CanvasGroup>();
            cg.alpha = 0f;

            // Animate popup
            cg.DOFade(1f, 0.3f).SetEase(Ease.OutQuad);
            rootPanel.transform.DOScale(1f, 0.3f).SetEase(Ease.OutBack);

            // Optional: dim wall fade in
            if (dimWall != null)
            {
                dimWall.color = new Color(0f, 0f, 0f, 0f);
                dimWall.DOFade(0.6f, 0.3f).SetEase(Ease.OutQuad);
            }
        }
        else
        {
            Debug.LogError("[FullscreenMemoryViewer] rootPanel is NULL at Show()");
        }
    }

    public void Hide()
    {
        if (rootPanel == null) return;

        CanvasGroup cg = rootPanel.GetComponent<CanvasGroup>();
        if (cg != null)
        {
            cg.DOFade(0f, 0.2f).SetEase(Ease.InQuad)
              .OnComplete(() => rootPanel.SetActive(false));
        }
        else
        {
            rootPanel.SetActive(false);
        }

        // Dim wall fade out
        if (dimWall != null)
        {
            dimWall.DOFade(0f, 0.2f).SetEase(Ease.InQuad);
        }
    }

    public static void EnsureInstance()
    {
        if (instance == null)
        {
            GameObject prefab = Resources.Load<GameObject>("Prefabs/FullscreenMemoryViewer");
            if (prefab != null)
            {
                GameObject clone = Instantiate(prefab);

                Canvas canvas = FindObjectOfType<Canvas>();
                if (canvas != null)
                    clone.transform.SetParent(canvas.transform, false);

                instance = clone.GetComponent<FullscreenMemoryViewer>();
                Debug.Log("[FullscreenMemoryViewer] Instantiated from Resources.");
            }
            else
            {
                Debug.LogError("[FullscreenMemoryViewer] Prefab not found!");
            }
        }
    }
}
