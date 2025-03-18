using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;


public class EvolutionSplashUI : MonoBehaviour
{
    [Header("Assign in Inspector")]
    public Image splashImage;
    public TextMeshProUGUI splashText;

    // Called by GridManager after instantiating
    public void Setup(string baseName, string evoName)
    {
        // Example text: "Rookie Dragon > Dark Dragon Knight"
        splashText.text = baseName + " > " + evoName;

        // Optionally you can start a coroutine to fade in/out or auto-destroy after some delay:
        StartCoroutine(AutoHideAfterDelay(2f));
    }

    private IEnumerator AutoHideAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        Destroy(gameObject);
    }
}
