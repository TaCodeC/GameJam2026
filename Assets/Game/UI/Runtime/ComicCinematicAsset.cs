using UnityEngine;

namespace GameJam.UI
{
    [CreateAssetMenu(menuName = "Game Jam/Cinematics/Comic Cinematic", fileName = "ComicCinematic")]
    public sealed class ComicCinematicAsset : ScriptableObject
    {
        [Header("Pages")]
        [SerializeField] private Sprite _defaultPage;
        [SerializeField] private ComicCinematicShot[] _shots = { new() };

        [Header("Development")]
        [SerializeField] private bool _liveUpdateWhilePlaying = true;

        [Header("Playback")]
        [SerializeField] private bool _pauseGame;
        [SerializeField] private bool _weakenUnderwater;
        [SerializeField, Range(0f, 1f)] private float _backgroundAlpha = 1f;
        [SerializeField, Min(0.01f)] private float _initialFadeDuration = 0.55f;
        [SerializeField, Min(0.01f)] private float _outroFadeDuration = 0.55f;
        [SerializeField, Min(0.01f)] private float _defaultMoveDuration = 1.1f;
        [SerializeField, Min(0f)] private float _defaultHoldDuration = 2.2f;
        [SerializeField, Min(0.01f)] private float _defaultFadeThroughBlackDuration = 0.42f;
        [SerializeField, Min(0.1f)] private float _defaultZoom = 1.05f;

        public Sprite DefaultPage => _defaultPage;
        public bool LiveUpdateWhilePlaying => _liveUpdateWhilePlaying;
        public bool PauseGame => _pauseGame;
        public bool WeakenUnderwater => _weakenUnderwater;
        public float BackgroundAlpha => _backgroundAlpha;
        public float InitialFadeDuration => _initialFadeDuration;
        public float OutroFadeDuration => _outroFadeDuration;
        public float DefaultMoveDuration => _defaultMoveDuration;
        public float DefaultHoldDuration => _defaultHoldDuration;
        public float DefaultFadeThroughBlackDuration => _defaultFadeThroughBlackDuration;
        public float DefaultZoom => _defaultZoom;
        public int ShotCount => _shots != null ? _shots.Length : 0;

        public ComicCinematicShot GetShot(int index)
        {
            if (_shots == null || index < 0 || index >= _shots.Length)
                return null;

            return _shots[index];
        }

        private void OnValidate()
        {
            _backgroundAlpha = Mathf.Clamp01(_backgroundAlpha);
            _initialFadeDuration = Mathf.Max(0.01f, _initialFadeDuration);
            _outroFadeDuration = Mathf.Max(0.01f, _outroFadeDuration);
            _defaultMoveDuration = Mathf.Max(0.01f, _defaultMoveDuration);
            _defaultHoldDuration = Mathf.Max(0f, _defaultHoldDuration);
            _defaultFadeThroughBlackDuration = Mathf.Max(0.01f, _defaultFadeThroughBlackDuration);
            _defaultZoom = Mathf.Max(0.1f, _defaultZoom);

            if (_shots == null || _shots.Length == 0)
                _shots = new[] { new ComicCinematicShot() };
        }
    }
}
