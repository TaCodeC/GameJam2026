using UnityEngine;

namespace GameJam.Audio
{
    [DisallowMultipleComponent]
    public sealed class CinematicAudioController : MonoBehaviour
    {
        private const string CinematicClipResourcePath = "Audio/Cinematicas";

        private static CinematicAudioController _active;

        private AudioSource _cinematicSource;
        private AudioClip _cinematicClip;
        private int _playDepth;

        public static CinematicAudioController Active
        {
            get
            {
                if (_active != null)
                    return _active;

                _active = FindFirstObjectByType<CinematicAudioController>(FindObjectsInactive.Include);
                if (_active != null)
                    return _active;

                GameObject controllerObject = new("Cinematic Audio Controller");
                _active = controllerObject.AddComponent<CinematicAudioController>();
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

        private void Update()
        {
            if (_playDepth <= 0)
                return;

            EnsureAudioSource();
            LoopingMusicPauseCoordinator.RefreshFor(this, _cinematicSource);
        }

        public void BeginCinematicAudio()
        {
            _playDepth++;
            if (_playDepth > 1)
                return;

            EnsureAudioSource();
            PauseLevelMusicSources();
            PlayCinematicMusic();
        }

        public void EndCinematicAudio()
        {
            if (_playDepth <= 0)
                return;

            _playDepth--;
            if (_playDepth > 0)
                return;

            if (_cinematicSource != null)
                _cinematicSource.Stop();

            ResumeLevelMusicSources();
        }

        private void EnsureAudioSource()
        {
            if (_cinematicSource != null)
                return;

            _cinematicSource = gameObject.AddComponent<AudioSource>();
            _cinematicSource.playOnAwake = false;
            _cinematicSource.loop = true;
            _cinematicSource.spatialBlend = 0f;
            _cinematicSource.volume = 1f;
        }

        private void PlayCinematicMusic()
        {
            if (_cinematicClip == null)
                _cinematicClip = Resources.Load<AudioClip>(CinematicClipResourcePath);

            if (_cinematicClip == null)
            {
                Debug.LogWarning($"[CinematicAudio] No se encontro Resources/{CinematicClipResourcePath}.", this);
                return;
            }

            _cinematicSource.clip = _cinematicClip;
            _cinematicSource.time = 0f;
            _cinematicSource.Play();
        }

        private void PauseLevelMusicSources()
        {
            LoopingMusicPauseCoordinator.PauseFor(this, _cinematicSource);
        }

        private void ResumeLevelMusicSources()
        {
            LoopingMusicPauseCoordinator.ReleaseFor(this);
        }
    }
}
