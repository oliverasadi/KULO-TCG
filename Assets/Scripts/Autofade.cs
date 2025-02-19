using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

public class AutoFade : MonoBehaviour
{
    private static AutoFade instance;
    private Canvas fadeCanvas;
    private Image fadeImage;

    private string levelName = "";
    private int levelIndex = -1;
    private bool fading = false;

    public static bool IsFading => instance != null && instance.fading;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            CreateFadeCanvas();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void CreateFadeCanvas()
    {
        fadeCanvas = new GameObject("FadeCanvas").AddComponent<Canvas>();
        fadeCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        fadeCanvas.sortingOrder = 1000; // Ensure it's on top
        DontDestroyOnLoad(fadeCanvas.gameObject);

        fadeImage = new GameObject("FadeImage").AddComponent<Image>();
        fadeImage.transform.SetParent(fadeCanvas.transform, false);
        fadeImage.rectTransform.anchorMin = Vector2.zero;
        fadeImage.rectTransform.anchorMax = Vector2.one;
        fadeImage.rectTransform.offsetMin = Vector2.zero;
        fadeImage.rectTransform.offsetMax = Vector2.zero;
        fadeImage.color = new Color(0, 0, 0, 0); // Fully transparent initially
    }

    private IEnumerator Fade(float fadeOutTime, float fadeInTime, Color fadeColor)
    {
        fading = true;

        // Fade Out
        yield return StartCoroutine(FadeToColor(fadeColor, fadeOutTime));

        // Load Scene
        if (!string.IsNullOrEmpty(levelName))
            yield return SceneManager.LoadSceneAsync(levelName);
        else
            yield return SceneManager.LoadSceneAsync(levelIndex);

        yield return new WaitForSeconds(0.1f); // Ensure scene loads properly

        // Fade In
        yield return StartCoroutine(FadeToColor(new Color(0, 0, 0, 0), fadeInTime));

        fading = false;
    }

    private IEnumerator FadeToColor(Color targetColor, float duration)
    {
        Color startColor = fadeImage.color;
        float time = 0f;

        while (time < duration)
        {
            fadeImage.color = Color.Lerp(startColor, targetColor, time / duration);
            time += Time.deltaTime;
            yield return null;
        }

        fadeImage.color = targetColor;
    }

    public static void LoadScene(string sceneName, float fadeOutTime = 1f, float fadeInTime = 1f, Color fadeColor = default)
    {
        if (IsFading) return;
        instance.levelName = sceneName;
        instance.StartCoroutine(instance.Fade(fadeOutTime, fadeInTime, fadeColor == default ? Color.black : fadeColor));
    }

    public static void LoadScene(int sceneIndex, float fadeOutTime = 1f, float fadeInTime = 1f, Color fadeColor = default)
    {
        if (IsFading) return;
        instance.levelName = "";
        instance.levelIndex = sceneIndex;
        instance.StartCoroutine(instance.Fade(fadeOutTime, fadeInTime, fadeColor == default ? Color.black : fadeColor));
    }
}
