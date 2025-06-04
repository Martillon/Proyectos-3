using UnityEngine;
using System.Collections;
using UnityEngine.UI; // Para Button si tienes un botón de volver

namespace Scripts.UI.MainMenu // O donde tengas tus controladores de UI
{
    public class CreditsController : MonoBehaviour
    {
        [Header("Panel References")]
        [Tooltip("El CanvasGroup del primer panel de créditos.")]
        [SerializeField] private CanvasGroup creditsPanel1;
        [Tooltip("El CanvasGroup del segundo panel de créditos.")]
        [SerializeField] private CanvasGroup creditsPanel2;

        [Header("Timing Settings")]
        [Tooltip("Tiempo (en segundos) que cada panel de créditos permanece visible (alpha = 1).")]
        [SerializeField] private float displayDuration = 5.0f;
        [Tooltip("Tiempo (en segundos) para la animación de fade in y fade out de los paneles.")]
        [SerializeField] private float fadeDuration = 0.75f;

        private Coroutine _creditsSequenceCoroutine;

        private void Awake()
        {
            // Asegurar que los paneles empiecen correctamente
            if (creditsPanel1 == null || creditsPanel2 == null)
            {
                Debug.LogError("CreditsController: Uno o ambos CanvasGroup de los paneles de créditos no están asignados.", this);
                enabled = false; // No puede funcionar sin los paneles
                return;
            }

            // Empezar con el panel 1 visible y el panel 2 oculto (o al revés si prefieres)
            InitializePanelState(creditsPanel1, true);  // Alfa 1, interactuable
            InitializePanelState(creditsPanel2, false); // Alfa 0, no interactuable
        }
        
        private void OnEnable() // Llamado cuando el GameObject que tiene este script se activa
        {
            // Iniciar la secuencia de créditos cuando el panel se muestra
            if (creditsPanel1 != null && creditsPanel2 != null)
            {
                if (_creditsSequenceCoroutine != null)
                {
                    StopCoroutine(_creditsSequenceCoroutine);
                }
                _creditsSequenceCoroutine = StartCoroutine(CreditsSequenceRoutine());
            }
        }

        private void OnDisable() // Llamado cuando el GameObject se desactiva
        {
            // Detener la secuencia si el panel de créditos se oculta
            if (_creditsSequenceCoroutine != null)
            {
                StopCoroutine(_creditsSequenceCoroutine);
                _creditsSequenceCoroutine = null;
            }
            // Restablecer el estado inicial por si se vuelve a abrir
            InitializePanelState(creditsPanel1, true); 
            InitializePanelState(creditsPanel2, false);
        }

        private void InitializePanelState(CanvasGroup panel, bool મુખ્યVisible)
        {
            if (panel != null)
            {
                panel.alpha = મુખ્યVisible ? 1f : 0f;
                panel.interactable = મુખ્યVisible;
                panel.blocksRaycasts = મુખ્યVisible;
                // Opcional: activar/desactivar el GameObject si el alpha 0 no lo oculta completamente
                // panel.gameObject.SetActive(главныйVisible); 
            }
        }

        private IEnumerator CreditsSequenceRoutine()
        {
            // Asegurar estado inicial antes del bucle
            InitializePanelState(creditsPanel1, true);
            InitializePanelState(creditsPanel2, false);
            
            // Para que el bucle funcione correctamente incluso si Time.timeScale es 0 (ej. si se accede desde un menú de pausa)
            // los WaitForSeconds deben ser WaitForSecondsRealtime.
            // Los fades ya usan Time.unscaledDeltaTime si usas el ScreenFader o una lógica similar.

            while (gameObject.activeInHierarchy) // Continuar mientras el panel de créditos esté activo
            {
                // --- Mostrar Panel 1 ---
                // Ya debería estar visible por InitializePanelState o el ciclo anterior.
                // Si no, hacer fade in:
                if (creditsPanel1.alpha < 1f) 
                    yield return StartCoroutine(FadeCanvasGroup(creditsPanel1, 1f, fadeDuration));
                
                SetPanelInteractable(creditsPanel1, true);
                SetPanelInteractable(creditsPanel2, false); // Asegurar que el otro no sea interactuable
                Debug.Log($"[{Time.frameCount}] Credits: Panel 1 Displaying for {displayDuration}s");
                yield return new WaitForSecondsRealtime(displayDuration);

                // --- Fade Out Panel 1, Fade In Panel 2 ---
                Debug.Log($"[{Time.frameCount}] Credits: Fading Out Panel 1, Fading In Panel 2");
                SetPanelInteractable(creditsPanel1, false);
                StartCoroutine(FadeCanvasGroup(creditsPanel1, 0f, fadeDuration)); // Iniciar fade out
                yield return StartCoroutine(FadeCanvasGroup(creditsPanel2, 1f, fadeDuration)); // Esperar a que este fade in termine
                
                SetPanelInteractable(creditsPanel2, true);
                Debug.Log($"[{Time.frameCount}] Credits: Panel 2 Displaying for {displayDuration}s");
                yield return new WaitForSecondsRealtime(displayDuration);

                // --- Fade Out Panel 2, Fade In Panel 1 ---
                Debug.Log($"[{Time.frameCount}] Credits: Fading Out Panel 2, Fading In Panel 1");
                SetPanelInteractable(creditsPanel2, false);
                StartCoroutine(FadeCanvasGroup(creditsPanel2, 0f, fadeDuration)); // Iniciar fade out
                yield return StartCoroutine(FadeCanvasGroup(creditsPanel1, 1f, fadeDuration)); // Esperar a que este fade in termine
                // El bucle se repetirá, mostrando el panel 1 de nuevo
            }
        }

        private void SetPanelInteractable(CanvasGroup panel, bool interactable)
        {
            if (panel != null)
            {
                panel.interactable = interactable;
                panel.blocksRaycasts = interactable;
            }
        }

        private IEnumerator FadeCanvasGroup(CanvasGroup cg, float targetAlpha, float duration)
        {
            if (cg == null) yield break;
            if (duration <= 0) // Aplicar instantáneamente si la duración es cero o negativa
            {
                cg.alpha = targetAlpha;
                yield break;
            }

            // cg.gameObject.SetActive(true); // Asegurar que el GO esté activo para el fade
            float startAlpha = cg.alpha;
            float time = 0;

            while (time < duration)
            {
                time += Time.unscaledDeltaTime; // Usar tiempo no escalado para que funcione si Time.timeScale es 0
                cg.alpha = Mathf.Lerp(startAlpha, targetAlpha, time / duration);
                yield return null;
            }
            cg.alpha = targetAlpha; // Asegurar el valor final
        }
        
    }
}
