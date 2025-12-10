using UnityEngine;

public class UIDrift : MonoBehaviour
{
    [Header("Drift Settings")]
    public float amplitude = 20f;
    public float speed = 2f;

    private RectTransform rt;
    private Vector2 startPos;

    void Start()
    {
        rt = GetComponent<RectTransform>();
        // startPos will be overridden externally via SetBasePosition
    }

    public void SetBasePosition(Vector2 newStartPos)
    {
        startPos = newStartPos;
    }

    void Update()
    {
        if (rt == null) return;

        float offsetX = Mathf.Sin(Time.time * speed) * amplitude;
        rt.anchoredPosition = new Vector2(startPos.x + offsetX, startPos.y);
    }
}
