using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

public class ScreenFader : MonoBehaviour
{
    public Image fadeImage;
    public float fadeDuration = 1f;

    void Awake()
    {
        if (fadeImage == null)
            fadeImage = GetComponentInChildren<Image>();
    }

    // --------------------------------------------------------------
    // FADE IN
    // --------------------------------------------------------------
    public IEnumerator FadeIn()
    {
        if (fadeImage == null)
        {
            Debug.LogWarning("ScreenFader: fadeImage missing.");
            yield break;
        }

        Color c = fadeImage.color;
        float t = 0;

        while (t < 1)
        {
            t += Time.deltaTime / fadeDuration;
            c.a = 1 - t;
            fadeImage.color = c;
            yield return null;
        }

        c.a = 0;
        fadeImage.color = c;
    }

    // --------------------------------------------------------------
    // FADE OUT ONLY
    // --------------------------------------------------------------
    public IEnumerator FadeOut()
    {
        if (fadeImage == null)
        {
            Debug.LogWarning("ScreenFader: fadeImage missing.");
            yield break;
        }

        Color c = fadeImage.color;
        float t = 0;

        while (t < 1)
        {
            t += Time.deltaTime / fadeDuration;
            c.a = t;
            fadeImage.color = c;
            yield return null;
        }

        c.a = 1;
        fadeImage.color = c;
    }

    // --------------------------------------------------------------
    // FADE OUT + LOAD SCENE
    // --------------------------------------------------------------
    public IEnumerator FadeOutAndLoad(string sceneName)
    {
        yield return FadeOut();
        SceneManager.LoadScene(sceneName);
    }

    // --------------------------------------------------------------
    // SAFE WRAPPER (no fadeImage → loads instantly)
    // --------------------------------------------------------------
    public IEnumerator FadeOutAndLoadSafe(string sceneName)
    {
        if (fadeImage == null)
        {
            Debug.LogWarning("ScreenFader not found — loading scene instantly.");
            SceneManager.LoadScene(sceneName);
            yield break;
        }

        yield return FadeOut();
        SceneManager.LoadScene(sceneName);
    }
}

