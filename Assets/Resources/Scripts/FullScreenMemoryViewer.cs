using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class FullscreenMemoryViewer : MonoBehaviour
{
    public static FullscreenMemoryViewer instance;

    [Header("UI Elements")]
    public GameObject rootPanel;
    public RectTransform zoomContainer;
    public Image displayImage;
    public Image dimWall;

    private float zoom = 1f;
    private float zoomMin = 1f;
    private float zoomMax = 3f;

    private Vector2 dragStartLocal;
    private Vector2 dragStartAnchored;

    private RectTransform canvasRect;

    private Tween zoomTween;
    private Tween panTween;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;

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

            canvasRect = GetComponentInParent<Canvas>()?.GetComponent<RectTransform>();

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

    void Update()
    {
        if (!rootPanel.activeSelf || zoomContainer == null) return;

        // 🔴 Press Escape to close
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Hide();
            return;
        }

        // 🔍 Zooming with smooth tween
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll != 0f)
        {
            zoom = Mathf.Clamp(zoom + scroll * 0.5f, zoomMin, zoomMax);
            zoomTween?.Kill();
            zoomTween = zoomContainer.DOScale(Vector3.one * zoom, 0.2f).SetEase(Ease.OutSine);
            ClampZoomPosition();
        }

        // 👆 Only allow panning if zoomed in
        if (zoom <= 1.01f) return;

        if (Input.GetMouseButtonDown(2))
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                zoomContainer, Input.mousePosition, null, out dragStartLocal
            );
            dragStartAnchored = zoomContainer.anchoredPosition;
        }
        else if (Input.GetMouseButton(2))
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                zoomContainer, Input.mousePosition, null, out Vector2 currentLocal
            );
            Vector2 delta = currentLocal - dragStartLocal;
            Vector2 targetPos = ClampPosition(dragStartAnchored + delta);

            panTween?.Kill();
            panTween = zoomContainer.DOAnchorPos(targetPos, 0.15f).SetEase(Ease.OutQuad);
        }
    }


    private Vector2 ClampPosition(Vector2 pos)
    {
        if (canvasRect == null || zoomContainer == null) return pos;

        Vector2 canvasSize = canvasRect.rect.size;
        Vector2 zoomedSize = zoomContainer.rect.size * zoom;

        float maxX = Mathf.Max(0, (zoomedSize.x - canvasSize.x) / 2f);
        float maxY = Mathf.Max(0, (zoomedSize.y - canvasSize.y) / 2f);

        return new Vector2(
            Mathf.Clamp(pos.x, -maxX, maxX),
            Mathf.Clamp(pos.y, -maxY, maxY)
        );
    }

    private void ClampZoomPosition()
    {
        zoomContainer.anchoredPosition = ClampPosition(zoomContainer.anchoredPosition);
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

            // Reset zoom/pan
            zoom = 1f;
            zoomTween?.Kill();
            zoomContainer.localScale = Vector3.one;
            zoomContainer.anchoredPosition = Vector2.zero;

            // Animate popup
            rootPanel.transform.localScale = Vector3.one * 0.9f;
            CanvasGroup cg = rootPanel.GetComponent<CanvasGroup>();
            if (cg == null) cg = rootPanel.AddComponent<CanvasGroup>();
            cg.alpha = 0f;

            cg.DOFade(1f, 0.3f).SetEase(Ease.OutQuad);
            rootPanel.transform.DOScale(1f, 0.3f).SetEase(Ease.OutBack);

            if (dimWall != null)
            {
                dimWall.color = new Color(0f, 0f, 0f, 0f);
                dimWall.DOFade(0.6f, 0.3f).SetEase(Ease.OutQuad);
            }
        }
    }

    public void Hide()
    {
        Debug.Log("[FullscreenMemoryViewer] Hide triggered.");

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
