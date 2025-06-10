using UnityEngine;

namespace Scripts.Core.Audio
{
    public class LevelMusicController: MonoBehaviour
    {
        [SerializeField] private AudioSource musicSource;
        [SerializeField] private AudioClip levelMusic;

        private void Awake()
        {
            // Don't play on awake.
            musicSource.playOnAwake = false;
        }

        private void OnEnable()
        {
            // Subscribe to the event.
            SceneLoader.OnSceneReady += StartLevelMusic;
        }

        private void OnDisable()
        {
            // Always unsubscribe!
            SceneLoader.OnSceneReady -= StartLevelMusic;
        }

        private void StartLevelMusic()
        {
            // This method will only be called when the scene is truly ready.
            //Debug.Log("LevelMusicController: OnSceneReady received. Starting music.");
            if (musicSource != null && levelMusic != null)
            {
                musicSource.clip = levelMusic;
                musicSource.loop = true;
                musicSource.Play();
            }
        }
    }
}