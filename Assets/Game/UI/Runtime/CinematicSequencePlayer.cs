using System.Collections;
using System.Collections.Generic;
using GameJam.Audio;
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
        private const float SceneRevealFailsafeExtraDelay = 0.75f;

        [SerializeField, Min(0.05f)] private float _initialFadeDuration = 0.85f;
        [SerializeField, Min(0.05f)] private float _fadeDuration = 0.55f;
        [SerializeField, Min(0.05f)] private float _slideTransitionFadeDuration = 0.45f;
        [SerializeField, Min(0f)] private float _postFadeInputBlockSeconds = 0.15f;
        [SerializeField, Min(0.25f)] private float _autoAdvanceSeconds = 3f;
        [SerializeField, Min(0.25f)] private float _longSequenceAutoAdvanceSeconds = 8f;
        [SerializeField, Range(0f, 1f)] private float _underwaterWeakenMultiplier = 0.28f;
        [SerializeField] private int _sortingOrder = 3000;
        [SerializeField] private bool _pauseGame;

        private static CinematicSequencePlayer _instance;
        private static bool s_sceneLoadRevealPending;
        private static bool s_sceneHookRegistered;

        private readonly List<UnderwaterSnapshot> _underwaterSnapshots = new();
        private CanvasGroup _imageGroup;
        private CanvasGroup _fadeGroup;
        private Image _image;
        private RectTransform _imageRect;
        private CinematicSideSmokeFadeOverlay _sideSmokeFadeOverlay;
        private float _previousTimeScale = 1f;
        private float _inputBlockedUntil;
        private NavigationDirection _navigationDirection = NavigationDirection.Next;
        private NavigationDirection _requestedNavigationDirection = NavigationDirection.Next;
        private bool _navigationRequestPending;
        private bool _hasImmediateCover;
        private bool _isPlaying;
        private bool _isFading;
        private bool _currentPausesGame;
        private bool _cinematicAudioActive;

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

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void RegisterSceneHook()
        {
            if (s_sceneHookRegistered)
                return;

            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
            s_sceneHookRegistered = true;
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (!s_sceneLoadRevealPending)
                return;

            Instance.StartCoroutine(Instance.SceneRevealFailsafeRoutine(scene.name));
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
            BuildCanvas();
        }

        private void OnDestroy()
        {
            if (_instance == this)
                _instance = null;

            if (_isPlaying)
            {
                RestoreUnderwater();

                if (_currentPausesGame)
                    Time.timeScale = _previousTimeScale;
            }

            _sideSmokeFadeOverlay?.Dispose();
            StopCinematicAudio();
        }

        public void Play(string[] resourceNames, bool weakenUnderwater = false)
        {
            StartCoroutine(PlayRoutine(resourceNames, weakenUnderwater));
        }

        public void CoverScreenImmediately()
        {
            BuildCanvas();
            ForceFadeBlack();
            _hasImmediateCover = true;
        }

        public IEnumerator FadeToBlackRoutine(float duration)
        {
            BuildCanvas();
            _fadeGroup.gameObject.SetActive(true);
            _fadeGroup.blocksRaycasts = true;
            yield return Fade(_fadeGroup, 1f, duration, true);
        }

        public IEnumerator PlayRoutine(
            string[] resourceNames,
            bool weakenUnderwater = false,
            string nextSceneName = "",
            System.Action onBlackBeforeReveal = null)
        {
            if (resourceNames == null || resourceNames.Length == 0)
                yield break;

            while (_isPlaying)
                yield return null;

            _isPlaying = true;
            _currentPausesGame = _pauseGame;
            bool useImmediateCover = _hasImmediateCover;
            _hasImmediateCover = false;
            BuildCanvas();
            ResetCanvasState();
            _previousTimeScale = Time.timeScale;

            if (_currentPausesGame)
                Time.timeScale = 0f;

            StartCinematicAudio();

            AsyncOperation loadOperation = null;
            if (!string.IsNullOrWhiteSpace(nextSceneName))
            {
                loadOperation = SceneManager.LoadSceneAsync(nextSceneName);
                if (loadOperation != null)
                    loadOperation.allowSceneActivation = false;
            }

            if (weakenUnderwater)
                WeakenUnderwater();

            int slideIndex = 0;
            if (useImmediateCover)
                ForceFadeBlack();
            else
                yield return Fade(_fadeGroup, 1f, _initialFadeDuration, true);

            _imageGroup.gameObject.SetActive(true);
            _imageGroup.blocksRaycasts = true;
            _imageGroup.interactable = true;
            _imageGroup.alpha = 1f;
            SetSideFadeVisible(true);
            SetSlide(resourceNames[slideIndex]);
            yield return Fade(_fadeGroup, 0f, _fadeDuration, true);

            while (slideIndex < resourceNames.Length)
            {
                yield return WaitForNavigation(GetAutoAdvanceSeconds(resourceNames.Length));

                int nextSlideIndex = _navigationDirection == NavigationDirection.Previous
                    ? Mathf.Max(0, slideIndex - 1)
                    : slideIndex + 1;

                if (nextSlideIndex >= resourceNames.Length)
                {
                    break;
                }

                if (nextSlideIndex == slideIndex)
                {
                    continue;
                }

                yield return BlinkToSlide(resourceNames[nextSlideIndex]);
                slideIndex = nextSlideIndex;
            }

            if (loadOperation != null)
            {
                yield return Fade(_fadeGroup, 1f, _fadeDuration, true);
                HideImageGroup();
            }
            else
            {
                yield return Fade(_fadeGroup, 1f, _fadeDuration, true);
                HideImageGroup();
            }

            InvokeBlackBeforeReveal(onBlackBeforeReveal);

            if (weakenUnderwater)
                RestoreUnderwater();

            if (loadOperation != null)
            {
                if (_currentPausesGame)
                    Time.timeScale = _previousTimeScale;

                s_sceneLoadRevealPending = true;
                loadOperation.allowSceneActivation = true;
                while (!loadOperation.isDone)
                    yield return null;

                yield return RevealLoadedSceneRoutine();
                yield break;
            }

            yield return Fade(_fadeGroup, 0f, _fadeDuration, true);

            if (_currentPausesGame)
                Time.timeScale = _previousTimeScale;

            HideFadeGroup();
            StopCinematicAudio();
            _isPlaying = false;
            _currentPausesGame = false;
        }

        private IEnumerator RevealLoadedSceneRoutine()
        {
            yield return null;
            ForceFadeBlack();
            yield return Fade(_fadeGroup, 0f, _fadeDuration, true);
            CompleteSceneReveal();
        }

        private IEnumerator SceneRevealFailsafeRoutine(string sceneName)
        {
            yield return null;
            yield return new WaitForSecondsRealtime(_fadeDuration + SceneRevealFailsafeExtraDelay);

            if (!s_sceneLoadRevealPending)
                yield break;

            Debug.LogWarning($"[Cinematics] Limpieza de emergencia del fade negro despues de cargar {sceneName}.", this);
            ForceCompleteSceneReveal();
        }

        private void ForceCompleteSceneReveal()
        {
            if (_fadeGroup != null)
                _fadeGroup.alpha = 0f;

            CompleteSceneReveal();
        }

        private void CompleteSceneReveal()
        {
            s_sceneLoadRevealPending = false;
            HideImageGroup();
            HideFadeGroup();
            ClearResidualBlackFades();
            StopCinematicAudio();
            _isFading = false;
            _isPlaying = false;
            _currentPausesGame = false;
        }

        private void BuildCanvas()
        {
            if (_imageGroup != null && _fadeGroup != null && _image != null)
            {
                EnsureSideSmokeFadeOverlay(_imageGroup.transform);
                return;
            }

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
            _imageRect = imageObject.GetComponent<RectTransform>();
            Center(_imageRect);
            _image = imageObject.GetComponent<Image>();
            _image.color = Color.white;
            _image.preserveAspect = false;
            _image.raycastTarget = false;
            _imageGroup.alpha = 0f;
            _imageGroup.blocksRaycasts = false;
            _imageGroup.interactable = false;
            _imageGroup.gameObject.SetActive(false);
            EnsureSideSmokeFadeOverlay(_imageGroup.transform);

            _fadeGroup = CreateGroup(canvasObject.transform, "Black Fade");
            Image fadeImage = _fadeGroup.GetComponent<Image>();
            fadeImage.color = Color.black;
            _fadeGroup.alpha = 0f;
            _fadeGroup.blocksRaycasts = false;
            _fadeGroup.interactable = false;
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
            group.blocksRaycasts = false;
            group.interactable = false;
            return group;
        }

        private void ResetCanvasState()
        {
            if (_imageGroup != null)
            {
                _imageGroup.alpha = 0f;
                _imageGroup.blocksRaycasts = false;
                _imageGroup.interactable = false;
                _imageGroup.gameObject.SetActive(false);
            }

            if (_fadeGroup != null)
            {
                _fadeGroup.alpha = 0f;
                _fadeGroup.blocksRaycasts = false;
                _fadeGroup.interactable = false;
            }

            if (_image != null)
            {
                _image.sprite = null;
                _image.enabled = false;
            }

            if (_imageRect != null)
                Center(_imageRect);

            SetSideFadeVisible(false);
            _isFading = false;
            _inputBlockedUntil = 0f;
            _navigationRequestPending = false;
        }

        private void HideImageGroup()
        {
            if (_imageGroup == null)
                return;

            _imageGroup.alpha = 0f;
            _imageGroup.blocksRaycasts = false;
            _imageGroup.interactable = false;
            _imageGroup.gameObject.SetActive(false);
            SetSideFadeVisible(false);
        }

        private void ForceFadeBlack()
        {
            if (_fadeGroup == null)
                return;

            _fadeGroup.gameObject.SetActive(true);
            _fadeGroup.alpha = 1f;
            _fadeGroup.blocksRaycasts = true;
            _fadeGroup.interactable = false;
        }

        private void HideFadeGroup()
        {
            if (_fadeGroup == null)
                return;

            _fadeGroup.alpha = 0f;
            _fadeGroup.blocksRaycasts = false;
            _fadeGroup.interactable = false;
            _fadeGroup.gameObject.SetActive(false);
        }

        private void EnsureSideSmokeFadeOverlay(Transform parent)
        {
            _sideSmokeFadeOverlay ??= CinematicSideSmokeFadeOverlay.Create(
                parent,
                RequestNextSlide,
                RequestPreviousSlide,
                this);
        }

        private void SetSideFadeVisible(bool visible)
        {
            _sideSmokeFadeOverlay?.SetVisible(visible);
        }

        private static void ClearResidualBlackFades()
        {
            CanvasGroup[] groups = Resources.FindObjectsOfTypeAll<CanvasGroup>();
            for (int i = 0; i < groups.Length; i++)
            {
                CanvasGroup group = groups[i];
                if (group == null || !IsResidualBlackFade(group))
                    continue;

                if (!group.gameObject.scene.IsValid())
                    continue;

                group.alpha = 0f;
                group.blocksRaycasts = false;
                group.interactable = false;
            }
        }

        private static bool IsResidualBlackFade(CanvasGroup group)
        {
            string groupName = group.gameObject.name;
            return groupName == "Black Fade" || groupName == "End Chase Fade";
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = Vector2.zero;
        }

        private static void Center(RectTransform rect)
        {
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
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

        private void SetSlide(string resourceName)
        {
            _image.sprite = LoadCinematic(resourceName);
            _image.enabled = _image.sprite != null;
            FitImageToCover();
            _imageGroup.alpha = 1f;
        }

        private void FitImageToCover()
        {
            if (_imageRect == null || _imageGroup == null || _image.sprite == null)
                return;

            Canvas.ForceUpdateCanvases();

            RectTransform parentRect = _imageGroup.transform as RectTransform;
            if (parentRect == null)
                return;

            Vector2 parentSize = parentRect.rect.size;
            if (parentSize.x <= 0.01f || parentSize.y <= 0.01f)
                parentSize = new Vector2(Screen.width, Screen.height);

            Rect spriteRect = _image.sprite.rect;
            if (spriteRect.width <= 0.01f || spriteRect.height <= 0.01f)
                return;

            float parentAspect = parentSize.x / parentSize.y;
            float spriteAspect = spriteRect.width / spriteRect.height;
            Vector2 coverSize = parentAspect > spriteAspect
                ? new Vector2(parentSize.x, parentSize.x / spriteAspect)
                : new Vector2(parentSize.y * spriteAspect, parentSize.y);

            Center(_imageRect);
            _imageRect.sizeDelta = coverSize;
        }

        private IEnumerator BlinkToSlide(string resourceName)
        {
            yield return Fade(_fadeGroup, 1f, _slideTransitionFadeDuration, true);
            SetSlide(resourceName);
            yield return Fade(_fadeGroup, 0f, _slideTransitionFadeDuration, true);
            _fadeGroup.blocksRaycasts = false;
        }

        private float GetAutoAdvanceSeconds(int slideCount)
        {
            return slideCount > 1 ? _longSequenceAutoAdvanceSeconds : _autoAdvanceSeconds;
        }

        private IEnumerator WaitForNavigation(float autoAdvanceSeconds)
        {
            _navigationDirection = NavigationDirection.Next;
            _navigationRequestPending = false;
            float elapsed = 0f;
            while (elapsed < autoAdvanceSeconds)
            {
                if (ConsumeNavigationRequest(out NavigationDirection requestedDirection))
                {
                    _navigationDirection = requestedDirection;
                    yield break;
                }

                if (AdvancePressed())
                {
                    _navigationDirection = NavigationDirection.Next;
                    yield break;
                }

                if (PreviousPressed())
                {
                    _navigationDirection = NavigationDirection.Previous;
                    yield break;
                }

                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            _navigationDirection = NavigationDirection.Next;
        }

        private bool AdvancePressed()
        {
            if (!CanReadNavigationInput())
                return false;

            if (Keyboard.current != null
                && (Keyboard.current.dKey.wasPressedThisFrame || Keyboard.current.rightArrowKey.wasPressedThisFrame))
                return true;

            return false;
        }

        private bool PreviousPressed()
        {
            if (!CanReadNavigationInput())
                return false;

            return Keyboard.current != null && Keyboard.current.leftArrowKey.wasPressedThisFrame;
        }

        private bool CanReadNavigationInput()
        {
            return !_isFading && Time.unscaledTime >= _inputBlockedUntil;
        }

        private void RequestNextSlide()
        {
            RequestNavigation(NavigationDirection.Next);
        }

        private void RequestPreviousSlide()
        {
            RequestNavigation(NavigationDirection.Previous);
        }

        private void RequestNavigation(NavigationDirection direction)
        {
            if (!CanReadNavigationInput())
                return;

            _requestedNavigationDirection = direction;
            _navigationRequestPending = true;
        }

        private bool ConsumeNavigationRequest(out NavigationDirection direction)
        {
            direction = _requestedNavigationDirection;
            if (!_navigationRequestPending)
                return false;

            _navigationRequestPending = false;
            return true;
        }

        private IEnumerator Fade(CanvasGroup group, float targetAlpha, float duration, bool blockRaycasts)
        {
            if (group == null)
                yield break;

            _isFading = true;
            group.gameObject.SetActive(true);
            group.blocksRaycasts = blockRaycasts;
            group.interactable = false;

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
            group.blocksRaycasts = blockRaycasts && targetAlpha > 0.01f;
            _isFading = false;
            _inputBlockedUntil = Mathf.Max(_inputBlockedUntil, Time.unscaledTime + _postFadeInputBlockSeconds);
        }

        private void StartCinematicAudio()
        {
            if (_cinematicAudioActive)
                return;

            _cinematicAudioActive = true;
            CinematicAudioController.Active.BeginCinematicAudio();
        }

        private void StopCinematicAudio()
        {
            if (!_cinematicAudioActive)
                return;

            _cinematicAudioActive = false;
            CinematicAudioController.Active.EndCinematicAudio();
        }

        private void InvokeBlackBeforeReveal(System.Action onBlackBeforeReveal)
        {
            if (onBlackBeforeReveal == null)
                return;

            try
            {
                onBlackBeforeReveal.Invoke();
            }
            catch (System.Exception exception)
            {
                Debug.LogException(exception, this);
            }
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

        private enum NavigationDirection
        {
            Next,
            Previous
        }
    }
}
