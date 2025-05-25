// ScreenFader.cs - Colocar en un GameObject con un componente Image UI
// que cubra toda la pantalla (Canvas > UI > Image).
// El color inicial de la imagen debe ser negro con Alpha = 0.
using UnityEngine;
using UnityEngine.UI; // Para Image
using System.Collections;

public class ScreenFader : MonoBehaviour
{
    public static ScreenFader Instance { get; private set; }

    [SerializeField] private Image fadeImage;
    [SerializeField] private float defaultFadeDuration = 0.5f;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            // DontDestroyOnLoad(gameObject); // Si quieres que persista entre escenas
            if (fadeImage == null)
            {
                fadeImage = GetComponent<Image>();
            }
            if (fadeImage == null)
            {
                Debug.LogError("ScreenFader: No Image component found or assigned!", this);
                enabled = false;
                return;
            }
            // Asegurar que empieza transparente
            Color c = fadeImage.color;
            c.a = 0;
            fadeImage.color = c;
            fadeImage.raycastTarget = false; // Para no bloquear inputs
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public Coroutine FadeToBlack(float? duration = null)
    {
        return StartCoroutine(FadeRoutine(1f, duration ?? defaultFadeDuration));
    }

    public Coroutine FadeToClear(float? duration = null)
    {
        return StartCoroutine(FadeRoutine(0f, duration ?? defaultFadeDuration));
    }

    private IEnumerator FadeRoutine(float targetAlpha, float duration)
    {
        if (fadeImage == null) yield break;

        fadeImage.raycastTarget = true; // Bloquear inputs durante el fade (opcional)
        float startAlpha = fadeImage.color.a;
        float time = 0;

        while (time < duration)
        {
            time += Time.unscaledDeltaTime; // Usar unscaledDeltaTime para que funcione si Time.timeScale es 0
            float alpha = Mathf.Lerp(startAlpha, targetAlpha, time / duration);
            Color c = fadeImage.color;
            c.a = alpha;
            fadeImage.color = c;
            yield return null;
        }

        Color finalColor = fadeImage.color;
        finalColor.a = targetAlpha;
        fadeImage.color = finalColor;
        fadeImage.raycastTarget = (targetAlpha > 0.5f); // Solo bloquear si es mayormente opaco
    }
}
