using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class DeathFadeUI : MonoBehaviour
{
    [SerializeField] private Image fadeImage;
    [SerializeField] private float fadeDuration = 10f;

    public IEnumerator FadeAndReload(float duration)
    {
        if (fadeImage == null)
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            Color color = fadeImage.color;
            color.a = t;
            fadeImage.color = color;
            yield return null;
        }

        Color finalColor = fadeImage.color;
        finalColor.a = 1f;
        fadeImage.color = finalColor;

        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}
