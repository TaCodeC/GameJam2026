using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace GameJam.Player.Cave
{
    [DisallowMultipleComponent]
    public sealed class CavePlayerStageTransition : MonoBehaviour
    {
        [Header("Player")]
        [SerializeField] private CavePlayerSkinController _skinController;
        [SerializeField] private CavePlayerSkin _targetSkin = CavePlayerSkin.Linterna;

        [Header("UI")]
        [SerializeField] private Button _transitionButton;
        [SerializeField] private CanvasGroup _fadeCanvasGroup;
        [SerializeField] private bool _hideButtonAfterTransition = true;

        [Header("Lights")]
        [SerializeField] private GameObject _turbinaSpotLight;
        [SerializeField] private GameObject _linternaSpotLight;

        [Header("Timing")]
        [SerializeField, Min(0f)] private float _fadeToBlackDuration = 0.45f;
        [SerializeField, Min(0f)] private float _holdBlackDuration = 1f;
        [SerializeField, Min(0f)] private float _fadeFromBlackDuration = 0.45f;

        private Coroutine _transitionRoutine;

        private void Awake()
        {
            if (_skinController == null)
                _skinController = GetComponent<CavePlayerSkinController>();

            ResolveSpotLights();
            SetFadeAlpha(0f);
        }

        private void OnEnable()
        {
            if (_transitionButton != null)
                _transitionButton.onClick.AddListener(StartTransition);
        }

        private void OnDisable()
        {
            if (_transitionButton != null)
                _transitionButton.onClick.RemoveListener(StartTransition);
        }

        public void StartTransition()
        {
            StartTransition(null);
        }

        public void StartTransition(Transform teleportTarget)
        {
            if (_transitionRoutine != null || _skinController == null)
                return;

            _transitionRoutine = StartCoroutine(TransitionRoutine(teleportTarget));
        }

        private IEnumerator TransitionRoutine(Transform teleportTarget)
        {
            if (_transitionButton != null)
                _transitionButton.interactable = false;

            yield return FadeTo(1f, _fadeToBlackDuration);

            TeleportTo(teleportTarget);
            _skinController.SetSkin(_targetSkin);
            ApplyLinternaLightState();

            if (_holdBlackDuration > 0f)
                yield return new WaitForSecondsRealtime(_holdBlackDuration);

            yield return FadeTo(0f, _fadeFromBlackDuration);

            if (_transitionButton != null)
            {
                _transitionButton.gameObject.SetActive(!_hideButtonAfterTransition);
                _transitionButton.interactable = !_hideButtonAfterTransition;
            }

            _transitionRoutine = null;
        }

        private void TeleportTo(Transform teleportTarget)
        {
            if (teleportTarget == null)
                return;

            if (TryGetComponent(out Rigidbody2D body))
            {
                body.linearVelocity = Vector2.zero;
                body.angularVelocity = 0f;
                body.position = teleportTarget.position;
            }

            transform.position = teleportTarget.position;
            Physics2D.SyncTransforms();
        }

        private void ApplyLinternaLightState()
        {
            ResolveSpotLights();

            if (_turbinaSpotLight != null)
                _turbinaSpotLight.SetActive(false);

            if (_linternaSpotLight != null)
                _linternaSpotLight.SetActive(true);
        }

        private void ResolveSpotLights()
        {
            if (_turbinaSpotLight == null)
                _turbinaSpotLight = FindChildGameObject("SpotLight_Turbina");

            if (_linternaSpotLight == null)
                _linternaSpotLight = FindChildGameObject("SpotLight_Linterna");
        }

        private GameObject FindChildGameObject(string childName)
        {
            Transform[] children = GetComponentsInChildren<Transform>(true);
            foreach (Transform child in children)
            {
                if (child.name == childName)
                    return child.gameObject;
            }

            return null;
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
