using UnityEngine;
using TMPro;
using System.Collections;

public class ToastManager : MonoBehaviour
{
    public static ToastManager instance;

    public TextMeshProUGUI toastText;
    public float displayDuration = 2f;

    private Coroutine currentToast;

    void Awake()
    {
        if (instance == null)
            instance = this;
        else
            Destroy(gameObject);
    }

    public void ShowToast(string message)
    {
        if (currentToast != null)
            StopCoroutine(currentToast);

        currentToast = StartCoroutine(ToastRoutine(message));
    }

    private IEnumerator ToastRoutine(string message)
    {
        if (toastText == null)
        {
            Debug.LogWarning("ToastManager: toastText not assigned.");
            yield break;
        }

        toastText.text = message;
        toastText.gameObject.SetActive(true);

        yield return new WaitForSeconds(displayDuration);

        toastText.gameObject.SetActive(false);
    }
}
