// En Scripts/UI/InGame/LevelCompleteUIController.cs
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
// using TMPro; // Descomenta si usas TextMeshPro
using Scripts.Core;
using Scripts.Player.Core;
using Scripts.Core.Audio;
using Scripts.Items.Checkpoint; // Para CheckpointManager
using UnityEngine.SceneManagement;
using Unity.Cinemachine; // Asegúrate de tener este using

namespace Scripts.UI.InGame
{
    public class LevelCompleteUIController : MonoBehaviour
    {
        [Header("UI Panel References")]
        [Tooltip("The main CanvasGroup for the entire Level Complete screen panel. Used for fading.")]
        [SerializeField] private CanvasGroup levelCompleteScreenCanvasGroup;
        [Tooltip("Optional: An Image used as a background overlay (e.g., for desaturation or color tint).")]
        [SerializeField] private Image backgroundOverlayImage;
        [Tooltip("GameObject containing the 'Mission Accomplished' message text/image.")]
        [SerializeField] private GameObject messageGroup;
        [Tooltip("GameObject containing the actionable buttons (Continue, Retry, Main Menu).")]
        [SerializeField] private GameObject buttonsGroup;
        [Tooltip("Reference to the in-game HUD GameObject, to be hidden.")]
        [SerializeField] private GameObject inGameHUD;

        [Header("Timing Settings")]
        [Tooltip("Duración para el fade del overlay de fondo (si se usa).")]
        [SerializeField] private float backgroundFadeDuration = 0.5f;
        [Tooltip("Tiempo que se espera DESPUÉS de que el evento OnLevelCompleted se dispara (y la animación de victoria del jugador comienza) ANTES de que la UI comience a aparecer.")]
        [SerializeField] private float delayBeforeUIShow = 0.75f; // Renombrado desde delayAfterPlayerVictoryAnimBeforeCamera
        [Tooltip("Delay DESPUÉS de que el fade de fondo (si existe) haya terminado, ANTES de mostrar el mensaje.")]
        [SerializeField] private float delayBeforeMessage = 0.5f;
        [Tooltip("Delay DESPUÉS de que el mensaje aparece, ANTES de mostrar los botones.")]
        [SerializeField] private float delayAfterMessageBeforeButtons = 1.0f;
        [Tooltip("Target alpha para el overlay de fondo.")]
        [SerializeField] private float backgroundOverlayTargetAlpha = 0.7f;

        [Header("Button References")]
        [SerializeField] private Button btn_Continue;
        [SerializeField] private Button btn_RetryLevel;
        [SerializeField] private Button btn_MainMenu;
        [SerializeField] private Button firstSelectedButtonOnComplete;

        [Header("Audio Feedback")]
        [SerializeField] private UIAudioFeedback uiSoundFeedback;
        [SerializeField] private Sounds levelCompleteMusicOrStinger;
        [SerializeField] private AudioSource uiAudioSource;
        
        private string _completedLevelIdentifier; 
        private Coroutine _showSequenceCoroutine;
        private Transform _playerTransformForCamera; 

        private void Awake()
        {
            if (levelCompleteScreenCanvasGroup != null)
            {
                levelCompleteScreenCanvasGroup.alpha = 0f;
                levelCompleteScreenCanvasGroup.interactable = false;
                levelCompleteScreenCanvasGroup.blocksRaycasts = false;
                levelCompleteScreenCanvasGroup.gameObject.SetActive(false); // Empieza desactivado
            }
            else
            {
                Debug.LogError("LCUIC: levelCompleteScreenCanvasGroup no está asignado!", this);
            }

            if (messageGroup != null) messageGroup.SetActive(false);
            if (buttonsGroup != null) buttonsGroup.SetActive(false);
            if (backgroundOverlayImage != null)
            {
                backgroundOverlayImage.gameObject.SetActive(false);
                Color c = backgroundOverlayImage.color; c.a = 0; backgroundOverlayImage.color = c;
            }
            if (uiAudioSource == null) uiAudioSource = GetComponent<AudioSource>();
        }

        private void OnEnable()
        {
            PlayerEvents.OnLevelCompleted += HandleLevelSuccessfullyCompleted;
            btn_Continue?.onClick.AddListener(OnContinueClicked);
            btn_RetryLevel?.onClick.AddListener(OnRetryLevelClicked);
            btn_MainMenu?.onClick.AddListener(OnMainMenuClicked);
        }

        private void OnDisable()
        {
            PlayerEvents.OnLevelCompleted -= HandleLevelSuccessfullyCompleted;
            btn_Continue?.onClick.RemoveListener(OnContinueClicked);
            btn_RetryLevel?.onClick.RemoveListener(OnRetryLevelClicked);
            btn_MainMenu?.onClick.RemoveListener(OnMainMenuClicked);
        }

        private void HandleLevelSuccessfullyCompleted(string levelId)
        {
            _completedLevelIdentifier = levelId;
            if (inGameHUD != null) inGameHUD.SetActive(false);
            levelCompleteScreenCanvasGroup.alpha = 1f; 
            if (_showSequenceCoroutine != null) StopCoroutine(_showSequenceCoroutine);
            _showSequenceCoroutine = StartCoroutine(ShowLevelCompleteSequence());
        }

         private IEnumerator ShowLevelCompleteSequence()
        {
            //Debug.Log($"[{Time.frameCount}] LCUIC: ShowLevelCompleteSequence STARTED.");
            levelCompleteMusicOrStinger?.Play(uiAudioSource);
            // La animación de victoria del jugador y la detención del Rigidbody son manejados por LevelExit y PlayerVisualController.

            InputManager.Instance?.DisableAllControls(); // Desactivar controles de UI para evitar inputs durante la secuencia
            
            // 1. Esperar un poco después de que el jugador haya alcanzado la salida y su animación de victoria comience.
            if (delayBeforeUIShow > 0)
            {
                //Debug.Log($"[{Time.frameCount}] LCUIC: Esperando {delayBeforeUIShow}s (delayBeforeUIShow).");
                yield return new WaitForSecondsRealtime(delayBeforeUIShow);
            }

            // 2. Activar el panel principal de la UI (aún transparente) y hacer fade del fondo (si existe).
            if (levelCompleteScreenCanvasGroup == null)
            {
                //Debug.LogError("LCUIC: levelCompleteScreenCanvasGroup es NULL. No se puede mostrar la UI.");
                yield break; // Salir de la corrutina si el panel principal no está asignado.
            }
            levelCompleteScreenCanvasGroup.gameObject.SetActive(true); // ¡IMPORTANTE ACTIVAR EL GO!
            //Debug.Log($"[{Time.frameCount}] LCUIC: Activado levelCompleteScreenCanvasGroup. Su alpha es: {levelCompleteScreenCanvasGroup.alpha}");


            Coroutine backgroundFadeCoroutine = null;
            if (backgroundOverlayImage != null)
            {
                backgroundOverlayImage.gameObject.SetActive(true); // Activar el GO de la imagen de overlay
                //Debug.Log($"[{Time.frameCount}] LCUIC: Iniciando fade de backgroundOverlayImage.");
                backgroundFadeCoroutine = StartCoroutine(FadeImageAlpha(backgroundOverlayImage, backgroundOverlayTargetAlpha, backgroundFadeDuration));
            }

            // Esperar a que el fade de fondo termine (si existe y tiene duración)
            if (backgroundFadeCoroutine != null) 
            {
                yield return backgroundFadeCoroutine;
                 //Debug.Log($"[{Time.frameCount}] LCUIC: Fade de backgroundOverlayImage completado.");
            }
            else if (backgroundOverlayImage != null && backgroundFadeDuration > 0) // Si hay imagen y duración pero no corrutina (no debería pasar)
            {
                yield return new WaitForSecondsRealtime(backgroundFadeDuration);
            }
            
            // 3. Mostrar Mensaje
            if (messageGroup != null)
            {
                //Debug.Log($"[{Time.frameCount}] LCUIC: Esperando {delayBeforeMessage}s para mostrar mensaje.");
                yield return new WaitForSecondsRealtime(delayBeforeMessage);
                messageGroup.SetActive(true);
                //Debug.Log($"[{Time.frameCount}] LCUIC: MessageGroup activado.");
            }

            // 4. Mostrar Botones
            yield return new WaitForSecondsRealtime(delayAfterMessageBeforeButtons);
            if (buttonsGroup != null)
            {
                buttonsGroup.SetActive(true);
                levelCompleteScreenCanvasGroup.interactable = true; 
                levelCompleteScreenCanvasGroup.blocksRaycasts = true;
                InputManager.Instance?.EnableUIControls();
                firstSelectedButtonOnComplete?.Select();
                //Debug.Log($"[{Time.frameCount}] LCUIC: ButtonsGroup activado. UI interactuable.");
            }
            
            //Debug.Log($"[{Time.frameCount}] LCUIC: UI secuencia completa. Time.timeScale = 0.");
            Time.timeScale = 0f;
            
        }

        private IEnumerator FadeImageAlpha(Image image, float targetAlpha, float duration)
        {
            // ... (Sin cambios, pero asegúrate de que la imagen esté activa para que el fade sea visible) ...
            if (image == null) { Debug.LogError("FadeImageAlpha: Image es null!"); yield break; }
            if (!image.gameObject.activeInHierarchy) image.gameObject.SetActive(true); // Asegurar que esté activo

            float startAlpha = image.color.a;
            float time = 0;
            // Si la duración es 0 o negativa, aplicar instantáneamente
            if (duration <= 0) {
                Color cDirect = image.color; cDirect.a = targetAlpha; image.color = cDirect;
                yield break;
            }
            while (time < duration)
            {
                time += Time.unscaledDeltaTime;
                float currentAlpha = Mathf.Lerp(startAlpha, targetAlpha, time / duration);
                Color c = image.color;
                c.a = currentAlpha;
                image.color = c;
                yield return null;
            }
            Color finalColor = image.color;
            finalColor.a = targetAlpha;
            image.color = finalColor;
        }

        private void RestoreMainCameraAndCleanup() 
        {
            Time.timeScale = 1f; 
            if (levelCompleteScreenCanvasGroup != null)
            {
                levelCompleteScreenCanvasGroup.alpha = 0f;
                levelCompleteScreenCanvasGroup.interactable = false;
                levelCompleteScreenCanvasGroup.blocksRaycasts = false;
                levelCompleteScreenCanvasGroup.gameObject.SetActive(false); // DESACTIVAR EL PANEL
            }
            // También asegurarse de que los hijos (messageGroup, buttonsGroup) se desactiven si no son hijos del CanvasGroup principal
            if (messageGroup != null && (levelCompleteScreenCanvasGroup == null || !messageGroup.transform.IsChildOf(levelCompleteScreenCanvasGroup.transform))) 
                messageGroup.SetActive(false);
            if (buttonsGroup != null && (levelCompleteScreenCanvasGroup == null || !buttonsGroup.transform.IsChildOf(levelCompleteScreenCanvasGroup.transform))) 
                buttonsGroup.SetActive(false);
            if (backgroundOverlayImage != null && (levelCompleteScreenCanvasGroup == null || !backgroundOverlayImage.transform.IsChildOf(levelCompleteScreenCanvasGroup.transform))) 
            {
                backgroundOverlayImage.gameObject.SetActive(false);
                Color c = backgroundOverlayImage.color; c.a = 0; backgroundOverlayImage.color = c;
            }
            // Si tenías lógica de restaurar cámara, iría aquí.
        }


        private void OnContinueClicked()
        {
            uiSoundFeedback?.PlayClick();
            RestoreMainCameraAndCleanup(); // Esto ya pone Time.timeScale = 1f;

            if (SceneLoader.Instance != null && !string.IsNullOrEmpty(_completedLevelIdentifier))
            {
                string nextLevelToLoad = null;
                int currentIndex = System.Array.IndexOf(SceneLoader.Instance.levels, _completedLevelIdentifier);

                if (currentIndex != -1 && currentIndex + 1 < SceneLoader.Instance.levels.Length)
                {
                    nextLevelToLoad = SceneLoader.Instance.levels[currentIndex + 1];
                    Debug.Log("LCUIC: Current level index: " + currentIndex + ", Next level to load: " + nextLevelToLoad);
                }

                if (!string.IsNullOrEmpty(nextLevelToLoad) && LevelProgressionManager.Instance.IsLevelUnlocked(nextLevelToLoad))
                {
                    Debug.Log($"LCUIC: Continuing to next level: {nextLevelToLoad}");
                    SceneLoader.Instance.LoadLevelByName(nextLevelToLoad);
                }
                else
                {
                    if (!string.IsNullOrEmpty(nextLevelToLoad)) {
                        Debug.LogWarning($"LCUIC: Next level '{nextLevelToLoad}' is not unlocked or doesn't exist. Returning to menu.");
                    } else {
                        Debug.Log("LCUIC: No next level defined or end of game. Returning to menu.");
                    }
                    SceneLoader.Instance.LoadMenu(); 
                }
            }
            else
            {
                Debug.LogError("LCUIC: SceneLoader or completedLevelIdentifier is missing. Cannot continue. Returning to menu.");
                SceneLoader.Instance?.LoadMenu(); // Fallback
            }
        }

        private void OnRetryLevelClicked()
        {
            uiSoundFeedback?.PlayClick();
            RestoreMainCameraAndCleanup();
            CheckpointManager.ResetCheckpointData(); 
            if (!string.IsNullOrEmpty(_completedLevelIdentifier))
            {
                SceneLoader.Instance?.LoadLevelByName(_completedLevelIdentifier);
            }
            else { SceneLoader.Instance?.LoadSceneByBuildIndex(SceneManager.GetActiveScene().buildIndex); }
        }

        private void OnMainMenuClicked()
        {
            uiSoundFeedback?.PlayClick();
            RestoreMainCameraAndCleanup();
            SceneLoader.Instance?.LoadMenu();
        }
    }
}
