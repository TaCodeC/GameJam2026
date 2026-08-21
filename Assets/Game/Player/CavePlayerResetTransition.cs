using System.Collections;
using UnityEngine;
using UnityEngine.UI;

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
        [SerializeField] private int _fallbackFadeSortingOrder = 5000;

        private Rigidbody2D _rigidbody;
        private Coroutine _resetRoutine;
        private float _previousTimeScale = 1f;

        public bool IsRunning => _resetRoutine != null;

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody2D>();
            EnsureFadeCanvas();
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
            EnsureFadeCanvas();
            _previousTimeScale = Time.timeScale;
            Time.timeScale = 0f;

            yield return FadeTo(1f, _fadeToBlackDuration);

            Vector3 previousPlayerPosition = transform.position;
            bool teleported = TeleportTo(resetTarget);
            if (teleported)
                CaveCameraSnapper.SnapAfterTeleport(transform, previousPlayerPosition);

            if (_holdBlackDuration > 0f)
                yield return new WaitForSecondsRealtime(_holdBlackDuration);

            yield return FadeTo(0f, _fadeFromBlackDuration);

            Time.timeScale = _previousTimeScale;
            TimedCanvasFader.ShowSceneHint();
            _resetRoutine = null;
        }

        private bool TeleportTo(Transform resetTarget)
        {
            if (resetTarget == null)
                return false;

            if (_rigidbody != null)
            {
                _rigidbody.linearVelocity = Vector2.zero;
                _rigidbody.angularVelocity = 0f;
                _rigidbody.position = resetTarget.position;
            }

            transform.position = resetTarget.position;
            Physics2D.SyncTransforms();
            return true;
        }

        private IEnumerator FadeTo(float targetAlpha, float duration)
        {
            if (_fadeCanvasGroup == null)
                yield break;

            _fadeCanvasGroup.gameObject.SetActive(true);
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

        private void EnsureFadeCanvas()
        {
            if (_fadeCanvasGroup != null)
                return;

            GameObject canvasObject = new GameObject("Cave Reset Fade Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = _fallbackFadeSortingOrder;

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            GameObject imageObject = new GameObject("Black Fade", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
            imageObject.transform.SetParent(canvasObject.transform, false);

            RectTransform rect = imageObject.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = Vector2.zero;

            Image image = imageObject.GetComponent<Image>();
            image.color = Color.black;
            image.raycastTarget = true;

            _fadeCanvasGroup = imageObject.GetComponent<CanvasGroup>();
            _fadeCanvasGroup.alpha = 0f;
            _fadeCanvasGroup.blocksRaycasts = false;
            _fadeCanvasGroup.interactable = false;
        }
    }
}
