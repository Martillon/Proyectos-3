using UnityEngine;

namespace Scripts.Core
{
    
    /// <summary>
    /// ProgramInitializer
    /// 
    /// This script is responsible for initializing core settings when the game starts.
    /// It is expected to be placed in the 'Program' scene, which is loaded first and kept in memory.
    /// - Applies all saved user settings (audio and video) via the SettingsManager.
    /// </summary>
    
    public class ProgramInitializer : MonoBehaviour
    {
        private void Start()
        {
            if (SettingsManager.Instance != null)
            {
                SettingsManager.Instance.ApplyAllSettings();
            }
            else
            {
                Debug.LogWarning("SettingsManager not found. Settings not applied.");
            }
        }
    }
}
