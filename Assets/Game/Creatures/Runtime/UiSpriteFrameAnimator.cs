using UnityEngine;
using UnityEngine.UI;

namespace GameJam.Creatures
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Image))]
    public sealed class UiSpriteFrameAnimator : MonoBehaviour
    {
        [SerializeField] private Image _targetImage;
        [SerializeField] private Sprite[] _frames = new Sprite[0];
        [SerializeField, Min(0.01f)] private float _frameRate = 8f;
        [SerializeField] private bool _playOnEnable = true;

        private int _frameIndex;
        private float _elapsed;
        private bool _isPlaying;

        public bool IsPlaying => _isPlaying;

        private void Reset()
        {
            _targetImage = GetComponent<Image>();
        }

        private void Awake()
        {
            if (_targetImage == null)
                _targetImage = GetComponent<Image>();
        }

        private void OnEnable()
        {
            _isPlaying = _playOnEnable;
            _elapsed = 0f;
            _frameIndex = 0;
            ApplyFrame();
        }

        private void Update()
        {
            if (!_isPlaying || _targetImage == null || _frames == null || _frames.Length == 0)
                return;

            _elapsed += Time.unscaledDeltaTime;
            float secondsPerFrame = 1f / _frameRate;

            while (_elapsed >= secondsPerFrame)
            {
                _elapsed -= secondsPerFrame;
                _frameIndex = (_frameIndex + 1) % _frames.Length;
                ApplyFrame();
            }
        }

        public void SetPlaying(bool isPlaying)
        {
            _isPlaying = isPlaying;
        }

        public void Play()
        {
            SetPlaying(true);
        }

        public void Pause()
        {
            SetPlaying(false);
        }

        private void ApplyFrame()
        {
            if (_targetImage == null || _frames == null || _frames.Length == 0)
                return;

            Sprite frame = _frames[_frameIndex];
            if (frame != null)
                _targetImage.sprite = frame;
        }
    }
}
