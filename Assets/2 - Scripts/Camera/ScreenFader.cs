using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// A singleton utility for fading the screen to a solid color (e.g., black) and back.
/// Uses a CanvasGroup for efficient alpha fading and raycast blocking.
/// </summary>
public class ScreenFader : MonoBehaviour
{
    public static ScreenFader Instance { get; private set; }

    [Header("UI References")]
    [Tooltip("The Image component used for the fade effect. It's color will be set to Fade Color.")]
    [SerializeField] private Image fadeImage;
    [Tooltip("The CanvasGroup controlling the fade panel. If not assigned, one will be added to the Fade Image's GameObject.")]
    [SerializeField] private CanvasGroup fadeCanvasGroup;

    [Header("Fade Configuration")]
    [Tooltip("The default duration for fade-in and fade-out animations in seconds.")]
    [SerializeField] private float defaultFadeDuration = 0.5f;
    [Tooltip("The color to fade to/from.")]
    [SerializeField] private Color fadeColor = Color.black;

    private Coroutine _activeFadeCoroutine;
    private GameObject _fadePanelGO; // The GameObject containing the fade UI elements.

    private void Awake()
    {
        // Singleton setup
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        // This component should be in a persistent scene.
        // DontDestroyOnLoad(gameObject); // Uncomment if needed.

        // --- Initialization and Validation ---
        if (fadeImage == null)
        {
            Debug.LogError("ScreenFader: Fade Image is not assigned in the Inspector.", this);
            enabled = false;
            return;
        }

        _fadePanelGO = fadeImage.gameObject;

        // Ensure we have a CanvasGroup. It's the best way to handle fading.
        if (fadeCanvasGroup == null)
        {
            fadeCanvasGroup = _fadePanelGO.GetComponent<CanvasGroup>();
            if (fadeCanvasGroup == null)
            {
                fadeCanvasGroup = _fadePanelGO.AddComponent<CanvasGroup>();
                // Debug.Log("ScreenFader: Added a CanvasGroup component to the fade image's GameObject.", this); // For debugging
            }
        }
        
        // --- Set Initial State ---
        // Configure the panel, then hide it.
        _fadePanelGO.SetActive(true); // Temporarily activate to configure.
        fadeImage.color = fadeColor;
        fadeCanvasGroup.alpha = 0f;
        fadeCanvasGroup.blocksRaycasts = false;
        _fadePanelGO.SetActive(false); // Deactivate after configuration.
    }

    /// <summary>
    /// Fades the screen from transparent to the configured solid color (e.g., to black).
    /// </summary>
    /// <param name="duration">Optional override for the fade duration.</param>
    /// <returns>A Coroutine handle for the fade process.</returns>
    public Coroutine FadeToBlack(float? duration = null)
    {
        if (fadeCanvasGroup == null) return null;

        if (_activeFadeCoroutine != null) StopCoroutine(_activeFadeCoroutine);
        _activeFadeCoroutine = StartCoroutine(FadeRoutine(1f, duration ?? defaultFadeDuration));
        return _activeFadeCoroutine;
    }

    /// <summary>
    /// Fades the screen from the solid color back to transparent.
    /// </summary>
    /// <param name="duration">Optional override for the fade duration.</param>
    /// <returns>A Coroutine handle for the fade process.</returns>
    public Coroutine FadeToClear(float? duration = null)
    {
        if (fadeCanvasGroup == null) return null;
        
        if (_activeFadeCoroutine != null) StopCoroutine(_activeFadeCoroutine);
        _activeFadeCoroutine = StartCoroutine(FadeRoutine(0f, duration ?? defaultFadeDuration));
        return _activeFadeCoroutine;
    }

    private IEnumerator FadeRoutine(float targetAlpha, float fadeDuration)
    {
        // 1. Prepare the panel for the fade
        _fadePanelGO.SetActive(true);
        float startAlpha = fadeCanvasGroup.alpha;
        float time = 0f;

        // If fading to black, block input immediately.
        // If fading to clear, raycasts will be unblocked at the end.
        fadeCanvasGroup.blocksRaycasts = (targetAlpha > 0.5f);

        // 2. Perform the fade over time
        // Use unscaledDeltaTime to ensure fades work even if Time.timeScale is 0 (e.g., in a pause menu).
        while (time < fadeDuration)
        {
            time += Time.unscaledDeltaTime;
            fadeCanvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, time / fadeDuration);
            yield return null;
        }

        // 3. Set final state and clean up
        fadeCanvasGroup.alpha = targetAlpha;
        fadeCanvasGroup.blocksRaycasts = (targetAlpha > 0.5f);

        // Optimization: If the screen is fully clear, deactivate the panel GameObject.
        if (Mathf.Approximately(targetAlpha, 0f))
        {
            _fadePanelGO.SetActive(false);
        }

        _activeFadeCoroutine = null;
    }
}