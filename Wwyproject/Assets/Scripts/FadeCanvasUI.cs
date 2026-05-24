using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class FadeCanvasUI : MonoBehaviour
{
    [SerializeField] private Image fadeImage;
    [SerializeField] private float duration = 1f;

    public void FadeIn()  { StartCoroutine(Fade(0f, 1f)); }
    public void FadeOut() { StartCoroutine(Fade(1f, 0f)); }

    private IEnumerator Fade(float from, float to)
    {
        if (fadeImage == null) yield break;

        Color c = fadeImage.color;
        c.a = from;
        fadeImage.color = c;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            c.a = Mathf.Lerp(from, to, elapsed / duration);
            fadeImage.color = c;
            yield return null;
        }

        c.a = to;
        fadeImage.color = c;
    }
}
