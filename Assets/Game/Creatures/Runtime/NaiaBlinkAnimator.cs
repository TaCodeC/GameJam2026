using System.Collections;
using UnityEngine;

namespace GameJam.Creatures
{
    [DisallowMultipleComponent]
    public sealed class NaiaBlinkAnimator : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer _spriteRenderer;
        [SerializeField] private Sprite _eyesOpenSprite;
        [SerializeField] private Sprite _eyesHalfClosedSprite;
        [SerializeField] private Sprite _eyesClosedSprite;
        [SerializeField] private Vector2 _blinkIntervalRange = new(3.5f, 7f);
        [SerializeField, Min(0.02f)] private float _transitionFrameDuration = 0.07f;
        [SerializeField, Min(0.02f)] private float _closedFrameDuration = 0.1f;

        private Coroutine _blinkRoutine;

        private void Reset()
        {
            _spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }

        private void OnEnable()
        {
            ShowSprite(_eyesOpenSprite);
            _blinkRoutine = StartCoroutine(BlinkRoutine());
        }

        private void OnDisable()
        {
            if (_blinkRoutine != null)
                StopCoroutine(_blinkRoutine);

            _blinkRoutine = null;
            ShowSprite(_eyesOpenSprite);
        }

        private IEnumerator BlinkRoutine()
        {
            while (true)
            {
                float delay = Random.Range(_blinkIntervalRange.x, _blinkIntervalRange.y);
                yield return new WaitForSeconds(delay);

                ShowSprite(_eyesHalfClosedSprite);
                yield return new WaitForSeconds(_transitionFrameDuration);

                ShowSprite(_eyesClosedSprite);
                yield return new WaitForSeconds(_closedFrameDuration);

                ShowSprite(_eyesHalfClosedSprite);
                yield return new WaitForSeconds(_transitionFrameDuration);

                ShowSprite(_eyesOpenSprite);
            }
        }

        private void ShowSprite(Sprite sprite)
        {
            if (_spriteRenderer != null && sprite != null)
                _spriteRenderer.sprite = sprite;
        }

        private void OnValidate()
        {
            float minInterval = Mathf.Max(0.1f, Mathf.Min(_blinkIntervalRange.x, _blinkIntervalRange.y));
            float maxInterval = Mathf.Max(minInterval, Mathf.Max(_blinkIntervalRange.x, _blinkIntervalRange.y));
            _blinkIntervalRange = new Vector2(minInterval, maxInterval);
            _transitionFrameDuration = Mathf.Max(0.02f, _transitionFrameDuration);
            _closedFrameDuration = Mathf.Max(0.02f, _closedFrameDuration);
        }
    }
}
