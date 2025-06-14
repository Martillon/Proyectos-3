using UnityEngine;
using System.Collections;
using Scripts.Core;

/// <summary>
/// A temporary debug-only utility to manually trigger the "OnSceneReady" event
/// for testing a level directly in the editor.
/// This component will automatically destroy itself in any non-editor build.
/// </summary>
public class Debug_SceneStarter : MonoBehaviour
{
    private void Awake()
    {
        // Safety check to ensure this object doesn't exist in a final build.
#if !UNITY_EDITOR
            Destroy(gameObject);
            return;
#endif
    }

    private void Start()
    {
        Debug.LogWarning("!!! DEBUG SCENE STARTER IS ACTIVE !!! This script manually fires OnSceneReady.");
        StartCoroutine(FireSceneReadyEventAfterDelay());
    }

    private IEnumerator FireSceneReadyEventAfterDelay()
    {
        // Wait for the end of the first frame to allow other scripts to subscribe.
        yield return new WaitForEndOfFrame();

        // --- THE FIX ---
        // Call the new public static method in SceneLoader.
        // This is the safe and correct way to trigger the event from an external class.
        SceneLoader.Debug_FireSceneReady();

        // The fallback check for InputManager is still a good idea.
        if (InputManager.Instance != null && InputManager.Instance.Controls != null && !InputManager.Instance.Controls.Player.enabled)
        {
            Debug.LogWarning("Debug_SceneStarter: Player controls were not enabled by the event. Forcing enable as a fallback.");
            InputManager.Instance.EnablePlayerControls();
        }

        // Self-destruct after the job is done.
        Destroy(gameObject);
    }
}
