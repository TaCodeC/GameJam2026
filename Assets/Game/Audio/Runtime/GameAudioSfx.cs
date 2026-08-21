using UnityEngine;

namespace GameJam.Audio
{
    [DisallowMultipleComponent]
    public sealed class GameAudioSfx : MonoBehaviour
    {
        private const string AluxeCoinClipResourcePath = "Audio/Aluxe Coin";
        private const string ClickClipResourcePath = "Audio/click";
        private const string StalactiteClipResourcePathA = "Audio/estalactita1";
        private const string StalactiteClipResourcePathB = "Audio/estalactita2";
        private const float StalactiteRetriggerBlockSeconds = 0.18f;

        private static GameAudioSfx _instance;

        private AudioSource _source;
        private AudioClip _aluxeCoinClip;
        private AudioClip _clickClip;
        private AudioClip _stalactiteClipA;
        private AudioClip _stalactiteClipB;
        private float _stalactiteBlockedUntil;

        private static GameAudioSfx Instance
        {
            get
            {
                if (_instance != null)
                    return _instance;

                _instance = FindFirstObjectByType<GameAudioSfx>(FindObjectsInactive.Include);
                if (_instance != null)
                    return _instance;

                GameObject sfxObject = new("Game Audio SFX");
                _instance = sfxObject.AddComponent<GameAudioSfx>();
                return _instance;
            }
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);
            EnsureAudioSource();
        }

        public static void PlayAluxeCoin()
        {
            GameAudioSfx instance = Instance;
            instance.Play(instance.LoadClip(ref instance._aluxeCoinClip, AluxeCoinClipResourcePath));
        }

        public static void PlayClick()
        {
            GameAudioSfx instance = Instance;
            instance.Play(instance.LoadClip(ref instance._clickClip, ClickClipResourcePath));
        }

        public static void PlayRandomStalactite()
        {
            Instance.PlayRandomStalactiteInternal();
        }

        private void PlayRandomStalactiteInternal()
        {
            if (Time.unscaledTime < _stalactiteBlockedUntil)
                return;

            AudioClip clipA = LoadClip(ref _stalactiteClipA, StalactiteClipResourcePathA);
            AudioClip clipB = LoadClip(ref _stalactiteClipB, StalactiteClipResourcePathB);
            AudioClip clip = Random.value < 0.5f ? clipA : clipB;

            if (clip == null)
                clip = clipA != null ? clipA : clipB;

            if (clip == null)
                return;

            _stalactiteBlockedUntil = Time.unscaledTime + StalactiteRetriggerBlockSeconds;
            Play(clip);
        }

        private void EnsureAudioSource()
        {
            if (_source != null)
                return;

            _source = gameObject.AddComponent<AudioSource>();
            _source.playOnAwake = false;
            _source.loop = false;
            _source.spatialBlend = 0f;
            _source.volume = 1f;
        }

        private AudioClip LoadClip(ref AudioClip clip, string resourcePath)
        {
            if (clip != null)
                return clip;

            clip = Resources.Load<AudioClip>(resourcePath);
            if (clip == null)
                Debug.LogWarning($"[GameAudioSfx] No se encontro Resources/{resourcePath}.", this);

            return clip;
        }

        private void Play(AudioClip clip)
        {
            if (clip == null)
                return;

            EnsureAudioSource();
            _source.PlayOneShot(clip);
        }
    }
}
