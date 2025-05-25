using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class ScreenFader : MonoBehaviour
{
    public static ScreenFader Instance { get; private set; }

    [SerializeField] private Image fadeImage; // ASIGNA ESTO EN EL INSPECTOR
    [SerializeField] private CanvasGroup fadeCanvasGroup; // ASIGNA ESTO (Opcional, pero recomendado)
    [SerializeField] private float defaultFadeDuration = 0.5f;
    [SerializeField] private Color fadeColor = Color.black; 

    private GameObject _fadeImageOwnerGO; // El GameObject que contiene la imagen

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            // DontDestroyOnLoad(gameObject);

            if (fadeImage == null && fadeCanvasGroup == null) {
                Debug.LogError("ScreenFader: Ni Fade Image ni Fade Canvas Group están asignados.", this);
                enabled = false; return;
            }
            
            // Determinar el GameObject a activar/desactivar
            if (fadeImage != null) _fadeImageOwnerGO = fadeImage.gameObject;
            else if (fadeCanvasGroup != null) _fadeImageOwnerGO = fadeCanvasGroup.gameObject;

            // Si no se asignó CanvasGroup pero sí Image, intentar obtenerlo/añadirlo
            if (fadeCanvasGroup == null && fadeImage != null)
            {
                fadeCanvasGroup = fadeImage.GetComponent<CanvasGroup>();
                if (fadeCanvasGroup == null) fadeCanvasGroup = fadeImage.gameObject.AddComponent<CanvasGroup>();
            }

            // Estado inicial (asegurar que esté configurado y desactivado si no lo estaba ya)
            if (_fadeImageOwnerGO != null) _fadeImageOwnerGO.SetActive(true); // Activar para configurar
            if (fadeImage != null) fadeImage.color = fadeColor;
            if (fadeCanvasGroup != null)
            {
                fadeCanvasGroup.alpha = 0;
                fadeCanvasGroup.blocksRaycasts = false;
            }
            else if (fadeImage != null)
            {
                Color c = fadeImage.color; c.a = 0; fadeImage.color = c;
                fadeImage.raycastTarget = false;
            }
            if (_fadeImageOwnerGO != null) _fadeImageOwnerGO.SetActive(false); // Desactivar después de configurar
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public Coroutine FadeToBlack(float? duration = null)
    {
        if (_fadeImageOwnerGO == null) return null;
        _fadeImageOwnerGO.SetActive(true); // Activar el objeto del fade
        StopAllCoroutines();
        return StartCoroutine(FadeRoutine(1f, duration ?? defaultFadeDuration));
    }

    public Coroutine FadeToClear(float? duration = null)
    {
        if (_fadeImageOwnerGO == null) return null;
        // No es necesario _fadeImageOwnerGO.SetActive(true); aquí si ya está activo por FadeToBlack
        // Pero si se pudiera llamar FadeToClear sin un FadeToBlack previo, sería necesario.
        // Por seguridad, lo activamos.
        if(!_fadeImageOwnerGO.activeSelf) _fadeImageOwnerGO.SetActive(true);

        StopAllCoroutines();
        return StartCoroutine(FadeRoutine(0f, duration ?? defaultFadeDuration));
    }

    private IEnumerator FadeRoutine(float targetAlpha, float fadeDuration)
    {
        // ... (Lógica de FadeRoutine como en la Opción 1, usando fadeImage y fadeCanvasGroup) ...
        // Asegúrate de que al final del fade a transparente, desactives el _fadeImageOwnerGO
        // si targetAlpha es (o está muy cerca de) 0.
        
        // Ejemplo del final de FadeRoutine:
        // ... (lerp de alpha) ...
        // yield return null;
        // } // Fin del while

        // Aplicar estado final
        if (fadeCanvasGroup != null) {
            fadeCanvasGroup.alpha = targetAlpha;
            fadeCanvasGroup.blocksRaycasts = (targetAlpha > 0.5f);
        } else if (fadeImage != null) {
            Color c = fadeImage.color; c.a = targetAlpha; fadeImage.color = c;
            fadeImage.raycastTarget = (targetAlpha > 0.5f);
        }

        if (targetAlpha < 0.01f && _fadeImageOwnerGO != null) // Si es transparente, desactivar el GO
        {
            _fadeImageOwnerGO.SetActive(false);
        }
        yield return null; // Asegurar que el coroutine termina correctamente
    }
}
