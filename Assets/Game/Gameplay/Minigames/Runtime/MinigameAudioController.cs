using System.Collections.Generic;
using UnityEngine;

namespace GameJam.Gameplay.Minigames
{
    [DisallowMultipleComponent]
    public sealed class MinigameAudioController : MonoBehaviour
    {
        private const string MinigameClipResourcePath = "Audio/Juegos cueva";

        private static MinigameAudioController _active;

        private readonly List<AudioSource> _pausedLevelSources = new();
        private AudioSource _minigameSource;
        private AudioClip _minigameClip;
        private int _playDepth;

        public static MinigameAudioController Active
        {
            get
            {
                if (_active != null)
                    return _active;

                _active = FindFirstObjectByType<MinigameAudioController>(FindObjectsInactive.Include);
                if (_active != null)
                    return _active;

                GameObject controllerObject = new GameObject("Minigame Audio Controller");
                _active = controllerObject.AddComponent<MinigameAudioController>();
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
            DontDestroyOnLoad(gameObject);
            EnsureAudioSource();
        }

        public void BeginMinigameAudio()
        {
            _playDepth++;
            if (_playDepth > 1)
                return;

            EnsureAudioSource();
            PauseLevelMusicSources();
            PlayMinigameMusic();
        }

        public void EndMinigameAudio()
        {
            if (_playDepth <= 0)
                return;

            _playDepth--;
            if (_playDepth > 0)
                return;

            if (_minigameSource != null)
                _minigameSource.Stop();

            ResumeLevelMusicSources();
        }

        private void EnsureAudioSource()
        {
            if (_minigameSource != null)
                return;

            _minigameSource = gameObject.AddComponent<AudioSource>();
            _minigameSource.playOnAwake = false;
            _minigameSource.loop = true;
            _minigameSource.spatialBlend = 0f;
            _minigameSource.volume = 1f;
        }

        private void PlayMinigameMusic()
        {
            if (_minigameClip == null)
                _minigameClip = Resources.Load<AudioClip>(MinigameClipResourcePath);

            if (_minigameClip == null)
            {
                Debug.LogWarning($"[MinigameAudio] No se encontro Resources/{MinigameClipResourcePath}.", this);
                return;
            }

            _minigameSource.clip = _minigameClip;
            _minigameSource.time = 0f;
            _minigameSource.Play();
        }

        private void PauseLevelMusicSources()
        {
            _pausedLevelSources.Clear();
            AudioSource[] sources = FindObjectsByType<AudioSource>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);

            for (int i = 0; i < sources.Length; i++)
            {
                AudioSource source = sources[i];
                if (source == null || source == _minigameSource || !source.isPlaying || !source.loop)
                    continue;

                source.Pause();
                _pausedLevelSources.Add(source);
            }
        }

        private void ResumeLevelMusicSources()
        {
            for (int i = 0; i < _pausedLevelSources.Count; i++)
            {
                AudioSource source = _pausedLevelSources[i];
                if (source != null)
                    source.UnPause();
            }

            _pausedLevelSources.Clear();
        }
    }
}
