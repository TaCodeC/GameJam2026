using GameJam.Audio;
using UnityEngine;

namespace GameJam.UI
{
    [DisallowMultipleComponent]
    public sealed class InstructionsAudioController : MonoBehaviour
    {
        private const string InstructionsClipResourcePath = "Audio/Pantallas de carga";

        private static InstructionsAudioController _active;

        private AudioSource _instructionsSource;
        private AudioClip _instructionsClip;
        private int _playDepth;

        public static InstructionsAudioController Active
        {
            get
            {
                if (_active != null)
                    return _active;

                _active = FindFirstObjectByType<InstructionsAudioController>(FindObjectsInactive.Include);
                if (_active != null)
                    return _active;

                GameObject controllerObject = new GameObject("Instructions Audio Controller");
                _active = controllerObject.AddComponent<InstructionsAudioController>();
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

        public void BeginInstructionsAudio()
        {
            _playDepth++;
            if (_playDepth > 1)
                return;

            EnsureAudioSource();
            PauseLevelMusicSources();
            PlayInstructionsMusic();
        }

        public void EndInstructionsAudio()
        {
            if (_playDepth <= 0)
                return;

            _playDepth--;
            if (_playDepth > 0)
                return;

            if (_instructionsSource != null)
                _instructionsSource.Stop();

            ResumeLevelMusicSources();
        }

        private void EnsureAudioSource()
        {
            if (_instructionsSource != null)
                return;

            _instructionsSource = gameObject.AddComponent<AudioSource>();
            _instructionsSource.playOnAwake = false;
            _instructionsSource.loop = true;
            _instructionsSource.spatialBlend = 0f;
            _instructionsSource.volume = 1f;
        }

        private void PlayInstructionsMusic()
        {
            if (_instructionsClip == null)
                _instructionsClip = Resources.Load<AudioClip>(InstructionsClipResourcePath);

            if (_instructionsClip == null)
            {
                Debug.LogWarning($"[InstructionsAudio] No se encontro Resources/{InstructionsClipResourcePath}.", this);
                return;
            }

            _instructionsSource.clip = _instructionsClip;
            _instructionsSource.time = 0f;
            _instructionsSource.Play();
        }

        private void PauseLevelMusicSources()
        {
            LoopingMusicPauseCoordinator.PauseFor(this, _instructionsSource);
        }

        private void ResumeLevelMusicSources()
        {
            LoopingMusicPauseCoordinator.ReleaseFor(this);
        }
    }
}
