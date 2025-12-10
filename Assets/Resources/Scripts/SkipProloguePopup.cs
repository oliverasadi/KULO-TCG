using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SkipProloguePopup : MonoBehaviour
{
    [Header("UI")]
    public TextMeshProUGUI headerText;   // Optional
    public TextMeshProUGUI bodyText;     // Optional
    public Button yesButton;
    public Button noButton;

    [Header("Animation")]
    [Tooltip("How far below its final position the popup starts (in pixels).")]
    public float slideDistanceY = 200f;
    [Tooltip("How long the slide-in takes (seconds).")]
    public float slideDuration = 0.4f;

    private Action _onYes;
    private Action _onNo;

    private RectTransform _rect;
    private Vector2 _targetPos;
    private Vector2 _startPos;

    private void Awake()
    {
        _rect = GetComponent<RectTransform>();
        if (_rect != null)
        {
            _targetPos = _rect.anchoredPosition;
            // start a bit below the screen (for bottom-anchored popup)
            _startPos = _targetPos + new Vector2(0f, -slideDistanceY);
            _rect.anchoredPosition = _startPos;
        }
    }

    private void OnEnable()
    {
        if (_rect != null)
        {
            StopAllCoroutines();
            StartCoroutine(SlideIn());
        }
    }

    private IEnumerator SlideIn()
    {
        float t = 0f;
        while (t < slideDuration)
        {
            t += Time.unscaledDeltaTime;              // ignore game timescale
            float normalized = Mathf.Clamp01(t / slideDuration);

            // Ease-out curve (starts fast, slows at the end)
            float eased = 1f - Mathf.Pow(1f - normalized, 3f);

            _rect.anchoredPosition = Vector2.Lerp(_startPos, _targetPos, eased);
            yield return null;
        }

        _rect.anchoredPosition = _targetPos;
    }

    // ---------------------------------------------------------------------

    public void Initialize(Action onYes, Action onNo)
    {
        _onYes = onYes;
        _onNo = onNo;

        if (yesButton != null)
        {
            yesButton.onClick.RemoveAllListeners();
            yesButton.onClick.AddListener(HandleYes);
        }

        if (noButton != null)
        {
            noButton.onClick.RemoveAllListeners();
            noButton.onClick.AddListener(HandleNo);
        }
    }

    private void HandleYes()
    {
        _onYes?.Invoke();
        Destroy(gameObject);
    }

    private void HandleNo()
    {
        _onNo?.Invoke();
        Destroy(gameObject);
    }
}
