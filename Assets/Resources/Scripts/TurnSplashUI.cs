using UnityEngine;
using TMPro;
using System.Collections;

public class TurnSplashUI : MonoBehaviour
{
    public TextMeshProUGUI splashText;

    // Call this method to set up the splash text and start the fade/auto-destroy coroutine.
    public void Setup(string message)
    {
        splashText.text = message;
        StartCoroutine(AutoHideAfterDelay(1f));
    }

    private IEnumerator AutoHideAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        Destroy(gameObject);
    }
}
