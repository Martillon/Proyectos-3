using UnityEngine;

namespace Scripts.Core
{
    /// <summary>
    /// This script is responsible for initializing core systems when the game starts.
    /// It should be placed in the persistent 'Program' scene, which is loaded first.
    /// Its primary role is to apply all saved user settings.
    /// </summary>
    public class ProgramInitializer : MonoBehaviour
    {
        // Start is used instead of Awake to ensure that the singletons of other managers
        // (like SettingsManager) have been set in their Awake methods.
        private void Start()
        {
            // It's crucial that this runs after the SettingsManager has initialized.
            if (SettingsManager.Instance != null)
            {
                SettingsManager.Instance.ApplyAllSettings();
            }
            else
            {
                Debug.LogWarning("ProgramInitializer: SettingsManager.Instance was not found in Start(). Settings could not be applied on launch.", this);
            }

            // You could add other one-time initializations here, for example:
            // - Initializing a global analytics service.
            // - Setting the target frame rate: Application.targetFrameRate = 60;
        }
    }
}

