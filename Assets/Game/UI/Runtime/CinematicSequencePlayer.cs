using System.Collections;
using System.Collections.Generic;
using GameJam.Rendering.Underwater;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace GameJam.UI
{
    [DisallowMultipleComponent]
    public sealed class CinematicSequencePlayer : MonoBehaviour
    {
        private const string ResourceRoot = "Cinematics/";

        [SerializeField, Min(0.05f)] private float _fadeDuration = 0.35f;
        [SerializeField, Min(0.25f)] private float _autoAdvanceSeconds = 3f;
        [SerializeField, Range(0f, 1f)] private float _underwaterWeakenMultiplier = 0.28f;
        [SerializeField] private int _sortingOrder = 3000;

        private static CinematicSequencePlayer _instance;

        private readonly List<UnderwaterSnapshot> _underwaterSnapshots = new();
        private CanvasGroup _imageGroup;
        private CanvasGroup _fadeGroup;
        private Image _image;
        private float _previousTimeScale = 1f;
        private bool _isPlaying;

        public static CinematicSequencePlayer Instance
        {
            get
            {
                if (_instance != null)
                    return _instance;

                _instance = FindFirstObjectByType<CinematicSequencePlayer>(FindObjectsInactive.Include);
                if (_instance != null)
                    return _instance;

                GameObject playerObject = new GameObject("Cinematic Sequence Player");
                _instance = playerObject.AddComponent<CinematicSequencePlayer>();
                return _instance;
            }
        }

        public bool IsPlaying => _isPlaying;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);
            BuildCanvas();
        }

        public void Play(string[] resourceNames, bool weakenUnderwater = false)
        {
            StartCoroutine(PlayRoutine(resourceNames, weakenUnderwater));
        }

        public IEnumerator PlayRoutine(string[] resourceNames, bool weakenUnderwater = false, string nextSceneName = "")
        {
            if (resourceNames == null || resourceNames.Length == 0)
                yield break;

            while (_isPlaying)
                yield return null;

            _isPlaying = true;
            BuildCanvas();
            _previousTimeScale = Time.timeScale;
            Time.timeScale = 0f;

            AsyncOperation loadOperation = null;
            if (!string.IsNullOrWhiteSpace(nextSceneName))
            {
                loadOperation = SceneManager.LoadSceneAsync(nextSceneName);
                if (loadOperation != null)
                    loadOperation.allowSceneActivation = false;
            }

            if (weakenUnderwater)
                WeakenUnderwater();

            yield return Fade(_fadeGroup, 1f, _fadeDuration);
            _imageGroup.gameObject.SetActive(true);

            for (int i = 0; i < resourceNames.Length; i++)
            {
                _image.sprite = LoadCinematic(resourceNames[i]);
                _image.enabled = _image.sprite != null;
                yield return Fade(_imageGroup, 1f, _fadeDuration);
                yield return WaitForAdvance();

                if (i < resourceNames.Length - 1)
                {
                    yield return Fade(_imageGroup, 0f, _fadeDuration * 0.5f);
                }
            }

            yield return Fade(_fadeGroup, 1f, _fadeDuration);
            _imageGroup.alpha = 0f;
            _imageGroup.gameObject.SetActive(false);

            if (weakenUnderwater)
                RestoreUnderwater();

            Time.timeScale = _previousTimeScale;
            _isPlaying = false;

            if (loadOperation != null)
            {
                loadOperation.allowSceneActivation = true;
                while (!loadOperation.isDone)
                    yield return null;

                yield return Fade(_fadeGroup, 0f, _fadeDuration);
                yield break;
            }

            yield return Fade(_fadeGroup, 0f, _fadeDuration);
        }

        private void BuildCanvas()
        {
            if (_imageGroup != null && _fadeGroup != null && _image != null)
                return;

            GameObject canvasObject = new GameObject("Cinematic Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(transform, false);

            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = _sortingOrder;

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            _imageGroup = CreateGroup(canvasObject.transform, "Cinematic Image");
            GameObject imageObject = new GameObject("Image", typeof(RectTransform), typeof(Image));
            imageObject.transform.SetParent(_imageGroup.transform, false);
            RectTransform imageRect = imageObject.GetComponent<RectTransform>();
            Stretch(imageRect);
            _image = imageObject.GetComponent<Image>();
            _image.color = Color.white;
            _image.preserveAspect = true;
            _image.raycastTarget = false;
            _imageGroup.alpha = 0f;
            _imageGroup.gameObject.SetActive(false);

            _fadeGroup = CreateGroup(canvasObject.transform, "Black Fade");
            Image fadeImage = _fadeGroup.GetComponent<Image>();
            fadeImage.color = Color.black;
            _fadeGroup.alpha = 0f;
            _fadeGroup.blocksRaycasts = true;
        }

        private static CanvasGroup CreateGroup(Transform parent, string name)
        {
            GameObject groupObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
            groupObject.transform.SetParent(parent, false);
            Stretch(groupObject.GetComponent<RectTransform>());

            Image background = groupObject.GetComponent<Image>();
            background.color = Color.clear;
            background.raycastTarget = true;

            CanvasGroup group = groupObject.GetComponent<CanvasGroup>();
            group.alpha = 0f;
            return group;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = Vector2.zero;
        }

        private Sprite LoadCinematic(string resourceName)
        {
            if (string.IsNullOrWhiteSpace(resourceName))
                return null;

            Sprite sprite = Resources.Load<Sprite>(ResourceRoot + resourceName);
            if (sprite == null)
                Debug.LogWarning($"[Cinematics] No se encontro la imagen Resources/{ResourceRoot}{resourceName}.", this);

            return sprite;
        }

        private IEnumerator WaitForAdvance()
        {
            float elapsed = 0f;
            while (elapsed < _autoAdvanceSeconds)
            {
                if (AdvancePressed())
                    yield break;

                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }
        }

        private static bool AdvancePressed()
        {
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
                return true;

            if (Keyboard.current != null
                && (Keyboard.current.dKey.wasPressedThisFrame || Keyboard.current.rightArrowKey.wasPressedThisFrame))
                return true;

            return Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame;
        }

        private static IEnumerator Fade(CanvasGroup group, float targetAlpha, float duration)
        {
            if (group == null)
                yield break;

            float startAlpha = group.alpha;
            float elapsed = 0f;
            duration = Mathf.Max(0.01f, duration);

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                group.alpha = Mathf.Lerp(startAlpha, targetAlpha, elapsed / duration);
                yield return null;
            }

            group.alpha = targetAlpha;
        }

        private void WeakenUnderwater()
        {
            _underwaterSnapshots.Clear();
            UnderwaterEffectController[] controllers = FindObjectsByType<UnderwaterEffectController>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);

            for (int i = 0; i < controllers.Length; i++)
            {
                UnderwaterEffectController controller = controllers[i];
                if (controller == null || !controller.isActiveAndEnabled)
                    continue;

                _underwaterSnapshots.Add(new UnderwaterSnapshot(controller));
                controller.tintIntensity *= _underwaterWeakenMultiplier;
                controller.darkness *= _underwaterWeakenMultiplier;
                controller.verticalGradientStrength *= _underwaterWeakenMultiplier;
                controller.vignetteStrength *= _underwaterWeakenMultiplier;
                controller.distortionStrength *= _underwaterWeakenMultiplier;
                controller.causticsIntensity *= _underwaterWeakenMultiplier;
                controller.lightIntensity *= _underwaterWeakenMultiplier;
                controller.ApplySettings();
            }
        }

        private void RestoreUnderwater()
        {
            for (int i = 0; i < _underwaterSnapshots.Count; i++)
            {
                _underwaterSnapshots[i].Restore();
            }

            _underwaterSnapshots.Clear();
        }

        private readonly struct UnderwaterSnapshot
        {
            private readonly UnderwaterEffectController _controller;
            private readonly float _tintIntensity;
            private readonly float _darkness;
            private readonly float _verticalGradientStrength;
            private readonly float _vignetteStrength;
            private readonly float _distortionStrength;
            private readonly float _causticsIntensity;
            private readonly float _lightIntensity;

            public UnderwaterSnapshot(UnderwaterEffectController controller)
            {
                _controller = controller;
                _tintIntensity = controller.tintIntensity;
                _darkness = controller.darkness;
                _verticalGradientStrength = controller.verticalGradientStrength;
                _vignetteStrength = controller.vignetteStrength;
                _distortionStrength = controller.distortionStrength;
                _causticsIntensity = controller.causticsIntensity;
                _lightIntensity = controller.lightIntensity;
            }

            public void Restore()
            {
                if (_controller == null)
                    return;

                _controller.tintIntensity = _tintIntensity;
                _controller.darkness = _darkness;
                _controller.verticalGradientStrength = _verticalGradientStrength;
                _controller.vignetteStrength = _vignetteStrength;
                _controller.distortionStrength = _distortionStrength;
                _controller.causticsIntensity = _causticsIntensity;
                _controller.lightIntensity = _lightIntensity;
                _controller.ApplySettings();
            }
        }
    }
}
