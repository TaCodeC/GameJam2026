using System.Collections;
using UnityEngine;

namespace GameJam.Player.Cave
{
    [DisallowMultipleComponent]
    public sealed class CavePlayerResetTransition : MonoBehaviour
    {
        [Header("UI")]
        [SerializeField] private CanvasGroup _fadeCanvasGroup;

        [Header("Timing")]
        [SerializeField, Min(0f)] private float _fadeToBlackDuration = 0.35f;
        [SerializeField, Min(0f)] private float _holdBlackDuration;
        [SerializeField, Min(0f)] private float _fadeFromBlackDuration = 0.35f;

        private Rigidbody2D _rigidbody;
        private Coroutine _resetRoutine;
        private float _previousTimeScale = 1f;

        public bool IsRunning => _resetRoutine != null;

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody2D>();
            SetFadeAlpha(0f);
        }

        private void OnDisable()
        {
            if (_resetRoutine == null)
                return;

            Time.timeScale = _previousTimeScale;
            _resetRoutine = null;
        }

        public void StartReset(Transform resetTarget)
        {
            if (_resetRoutine != null)
                return;

            _resetRoutine = StartCoroutine(ResetRoutine(resetTarget));
        }

        private IEnumerator ResetRoutine(Transform resetTarget)
        {
            _previousTimeScale = Time.timeScale;
            Time.timeScale = 0f;

            yield return FadeTo(1f, _fadeToBlackDuration);

            TeleportTo(resetTarget);

            if (_holdBlackDuration > 0f)
                yield return new WaitForSecondsRealtime(_holdBlackDuration);

            yield return FadeTo(0f, _fadeFromBlackDuration);

            Time.timeScale = _previousTimeScale;
            _resetRoutine = null;
        }

        private void TeleportTo(Transform resetTarget)
        {
            if (resetTarget == null)
                return;

            if (_rigidbody != null)
            {
                _rigidbody.linearVelocity = Vector2.zero;
                _rigidbody.angularVelocity = 0f;
                _rigidbody.position = resetTarget.position;
            }

            transform.position = resetTarget.position;
            Physics2D.SyncTransforms();
        }

        private IEnumerator FadeTo(float targetAlpha, float duration)
        {
            if (_fadeCanvasGroup == null)
                yield break;

            _fadeCanvasGroup.blocksRaycasts = true;
            float startAlpha = _fadeCanvasGroup.alpha;

            if (duration <= 0f)
            {
                SetFadeAlpha(targetAlpha);
                yield break;
            }

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                SetFadeAlpha(Mathf.Lerp(startAlpha, targetAlpha, Mathf.Clamp01(elapsed / duration)));
                yield return null;
            }

            SetFadeAlpha(targetAlpha);
        }

        private void SetFadeAlpha(float alpha)
        {
            if (_fadeCanvasGroup == null)
                return;

            _fadeCanvasGroup.alpha = alpha;
            _fadeCanvasGroup.blocksRaycasts = alpha > 0.01f;
            _fadeCanvasGroup.interactable = false;
        }
    }
}
