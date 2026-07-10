using UnityEngine;

namespace GameJam.Audio
{
    [DisallowMultipleComponent]
    public sealed class SceneMusicController : MonoBehaviour
    {
        private const string EndLevelClipResourcePath = "Audio/nivel final";

        private static SceneMusicController _active;

        private AudioSource _musicSource;
        private AudioClip _endLevelClip;

        public static SceneMusicController Active
        {
            get
            {
                if (_active != null)
                    return _active;

                _active = FindFirstObjectByType<SceneMusicController>(FindObjectsInactive.Include);
                if (_active != null)
                    return _active;

                GameObject controllerObject = new("Scene Music Controller");
                _active = controllerObject.AddComponent<SceneMusicController>();
                return _active;
            }
        }

        private void Awake()
        {
            if (_active != null && _active != this)
            {
                Destroy(gameObject);
                return;
            }

            _active = this;
            EnsureAudioSource();
        }

        public void PlayEndLevelMusic()
        {
            EnsureAudioSource();
            StopOtherLoopingSources();

            if (_endLevelClip == null)
                _endLevelClip = Resources.Load<AudioClip>(EndLevelClipResourcePath);

            if (_endLevelClip == null)
            {
                Debug.LogWarning($"[SceneMusic] No se encontro Resources/{EndLevelClipResourcePath}.", this);
                return;
            }

            _musicSource.clip = _endLevelClip;
            _musicSource.time = 0f;
            _musicSource.Play();
        }

        private void EnsureAudioSource()
        {
            if (_musicSource != null)
                return;

            _musicSource = gameObject.AddComponent<AudioSource>();
            _musicSource.playOnAwake = false;
            _musicSource.loop = true;
            _musicSource.spatialBlend = 0f;
            _musicSource.volume = 1f;
        }

        private void StopOtherLoopingSources()
        {
            AudioSource[] sources = FindObjectsByType<AudioSource>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            for (int i = 0; i < sources.Length; i++)
            {
                AudioSource source = sources[i];
                if (source == null || source == _musicSource || !source.isPlaying || !source.loop)
                    continue;

                source.Stop();
            }
        }
    }
}
