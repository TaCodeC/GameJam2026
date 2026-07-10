using System;
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
    public sealed class ComicCinematicPlayer : MonoBehaviour
    {
        private const string ResourceRoot = "Cinematics/Comic/";
        private const float SceneRevealFailsafeExtraDelay = 0.75f;

        [SerializeField, Min(0f)] private float _postTransitionInputBlockSeconds = 0.12f;
        [SerializeField, Range(0f, 1f)] private float _underwaterWeakenMultiplier = 0.28f;
        [SerializeField] private int _sortingOrder = 3100;
        [SerializeField] private bool _allowLiveAssetUpdates = true;

        private static ComicCinematicPlayer _instance;
        private static bool s_sceneLoadRevealPending;
        private static bool s_sceneHookRegistered;

        private readonly List<UnderwaterSnapshot> _underwaterSnapshots = new();
        private CanvasGroup _pageGroup;
        private Image _pageBackground;
        private Image _pageImage;
        private RectTransform _pageImageRect;
        private CinematicSideSmokeFadeOverlay _sideSmokeFadeOverlay;
        private CanvasGroup _fadeGroup;
        private float _previousTimeScale = 1f;
        private float _currentOutroFadeDuration = 0.55f;
        private float _inputBlockedUntil;
        private ComicCinematicAsset _activeCinematic;
        private int _activeShotIndex = -1;
        private NavigationDirection _navigationDirection = NavigationDirection.Next;
        private NavigationDirection _requestedNavigationDirection = NavigationDirection.Next;
        private bool _navigationRequestPending;
        private bool _isPlaying;
        private bool _isFading;
        private bool _currentPausesGame;
        private bool _cinematicAudioActive;

        public static ComicCinematicPlayer Instance
        {
            get
            {
                if (_instance != null)
                    return _instance;

                _instance = FindFirstObjectByType<ComicCinematicPlayer>(FindObjectsInactive.Include);
                if (_instance != null)
                    return _instance;

                GameObject playerObject = new GameObject("Comic Cinematic Player");
                _instance = playerObject.AddComponent<ComicCinematicPlayer>();
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

        private void LateUpdate()
        {
            if (!_isPlaying || _activeCinematic == null || _activeShotIndex < 0)
                return;

            RefreshCurrentShotFromAsset(_activeCinematic, _activeShotIndex);
        }

        public void Play(ComicCinematicAsset cinematic, string nextSceneName = "")
        {
            StartCoroutine(PlayRoutine(cinematic, nextSceneName));
        }

        public void PlayResource(string resourceName, string nextSceneName = "")
        {
            StartCoroutine(PlayResourceRoutine(resourceName, nextSceneName));
        }

        public void CoverScreenImmediately()
        {
            BuildCanvas();
            ForceFadeBlack();
        }

        public IEnumerator PlayResourceRoutine(
            string resourceName,
            string nextSceneName = "",
            Action onBlackBeforeReveal = null)
        {
            ComicCinematicAsset cinematic = LoadComicCinematic(resourceName);
            if (cinematic == null)
                yield break;

            yield return PlayRoutine(cinematic, nextSceneName, onBlackBeforeReveal);
        }

        public IEnumerator PlayRoutine(
            ComicCinematicAsset cinematic,
            string nextSceneName = "",
            Action onBlackBeforeReveal = null)
        {
            if (cinematic == null || cinematic.ShotCount == 0)
                yield break;

            int shotIndex = FindFirstShotIndex(cinematic);
            if (shotIndex < 0)
            {
                Debug.LogWarning("[ComicCinematics] La cinematica no tiene shots validos.", this);
                yield break;
            }

            ComicCinematicShot firstShot = cinematic.GetShot(shotIndex);
            Sprite firstPage = ResolvePage(cinematic, firstShot);
            if (firstPage == null)
                yield break;

            while (_isPlaying)
                yield return null;

            Debug.Log($"[ComicCinematics] Playing {cinematic.name} ({cinematic.ShotCount} shots).", this);

            _isPlaying = true;
            _currentPausesGame = cinematic.PauseGame;
            _currentOutroFadeDuration = cinematic.OutroFadeDuration;
            BuildCanvas();
            ResetCanvasState(cinematic);
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

            if (cinematic.WeakenUnderwater)
                WeakenUnderwater();

            _pageGroup.gameObject.SetActive(true);
            _pageGroup.alpha = 1f;
            _pageGroup.blocksRaycasts = true;
            _pageGroup.interactable = true;
            SetSideFadeVisible(true);
            SetActiveShot(cinematic, shotIndex);
            ApplyPose(CalculatePose(firstPage, firstShot.NormalizedFocus, firstShot.GetZoom(cinematic.DefaultZoom), firstShot.ZoomOffset));
            ForceFadeBlack();
            yield return Fade(_fadeGroup, 0f, cinematic.InitialFadeDuration, true);

            while (shotIndex < cinematic.ShotCount)
            {
                ComicCinematicShot shot = cinematic.GetShot(shotIndex);
                if (shot == null)
                {
                    shotIndex = GetNextShotIndex(cinematic, shotIndex, NavigationDirection.Next);
                    continue;
                }

                SetActiveShot(cinematic, shotIndex);
                yield return WaitForAdvance(cinematic, shotIndex);

                int nextShotIndex = GetNextShotIndex(cinematic, shotIndex, _navigationDirection);
                if (nextShotIndex >= cinematic.ShotCount)
                    break;

                if (nextShotIndex == shotIndex)
                    continue;

                yield return TransitionToShot(cinematic, nextShotIndex);
                shotIndex = nextShotIndex;
            }

            yield return Fade(_fadeGroup, 1f, cinematic.OutroFadeDuration, true);
            HidePageGroup();
            InvokeBlackBeforeReveal(onBlackBeforeReveal);

            if (cinematic.WeakenUnderwater)
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

            yield return Fade(_fadeGroup, 0f, cinematic.OutroFadeDuration, true);

            if (_currentPausesGame)
                Time.timeScale = _previousTimeScale;

            HideFadeGroup();
            StopCinematicAudio();
            ClearActiveShot();
            _isPlaying = false;
            _currentPausesGame = false;
        }

        private IEnumerator RevealLoadedSceneRoutine()
        {
            yield return null;
            ForceFadeBlack();
            yield return Fade(_fadeGroup, 0f, _currentOutroFadeDuration, true);
            CompleteSceneReveal();
        }

        private IEnumerator SceneRevealFailsafeRoutine(string sceneName)
        {
            yield return null;
            yield return new WaitForSecondsRealtime(_currentOutroFadeDuration + SceneRevealFailsafeExtraDelay);

            if (!s_sceneLoadRevealPending)
                yield break;

            Debug.LogWarning($"[ComicCinematics] Limpieza de emergencia del fade negro despues de cargar {sceneName}.", this);
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
            HidePageGroup();
            HideFadeGroup();
            ClearResidualComicFades();
            StopCinematicAudio();
            ClearActiveShot();
            _isFading = false;
            _isPlaying = false;
            _currentPausesGame = false;
        }

        private void BuildCanvas()
        {
            if (_pageGroup != null && _fadeGroup != null && _pageImage != null)
            {
                EnsureSideSmokeFadeOverlay(_pageGroup.transform);
                return;
            }

            GameObject canvasObject = new GameObject("Comic Cinematic Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(transform, false);

            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = _sortingOrder;

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            _pageGroup = CreateGroup(canvasObject.transform, "Comic Page View");
            _pageBackground = _pageGroup.GetComponent<Image>();
            _pageBackground.color = Color.black;
            _pageBackground.raycastTarget = true;

            GameObject imageObject = new GameObject("Comic Page", typeof(RectTransform), typeof(Image));
            imageObject.transform.SetParent(_pageGroup.transform, false);
            _pageImageRect = imageObject.GetComponent<RectTransform>();
            Center(_pageImageRect);
            _pageImage = imageObject.GetComponent<Image>();
            _pageImage.color = Color.white;
            _pageImage.preserveAspect = false;
            _pageImage.raycastTarget = false;

            EnsureSideSmokeFadeOverlay(_pageGroup.transform);

            _fadeGroup = CreateGroup(canvasObject.transform, "Comic Black Fade");
            Image fadeImage = _fadeGroup.GetComponent<Image>();
            fadeImage.color = Color.black;
            fadeImage.raycastTarget = true;
        }

        private static CanvasGroup CreateGroup(Transform parent, string name)
        {
            GameObject groupObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
            groupObject.transform.SetParent(parent, false);
            Stretch(groupObject.GetComponent<RectTransform>());

            Image image = groupObject.GetComponent<Image>();
            image.color = Color.clear;
            image.raycastTarget = true;

            CanvasGroup group = groupObject.GetComponent<CanvasGroup>();
            group.alpha = 0f;
            group.blocksRaycasts = false;
            group.interactable = false;
            group.gameObject.SetActive(false);
            return group;
        }

        private void EnsureSideSmokeFadeOverlay(Transform parent)
        {
            _sideSmokeFadeOverlay ??= CinematicSideSmokeFadeOverlay.Create(
                parent,
                RequestNextShot,
                RequestPreviousShot,
                this);
        }

        private void ResetCanvasState(ComicCinematicAsset cinematic)
        {
            if (_pageGroup != null)
            {
                _pageGroup.alpha = 0f;
                _pageGroup.blocksRaycasts = false;
                _pageGroup.interactable = false;
                _pageGroup.gameObject.SetActive(false);
            }

            if (_pageBackground != null)
                _pageBackground.color = new Color(0f, 0f, 0f, cinematic.BackgroundAlpha);

            if (_pageImage != null)
            {
                _pageImage.sprite = null;
                _pageImage.enabled = false;
            }

            if (_pageImageRect != null)
                Center(_pageImageRect);

            SetSideFadeVisible(false);

            if (_fadeGroup != null)
            {
                _fadeGroup.alpha = 0f;
                _fadeGroup.blocksRaycasts = false;
                _fadeGroup.interactable = false;
                _fadeGroup.gameObject.SetActive(false);
            }

            _isFading = false;
            _inputBlockedUntil = 0f;
            _navigationRequestPending = false;
        }

        private void HidePageGroup()
        {
            if (_pageGroup == null)
                return;

            _pageGroup.alpha = 0f;
            _pageGroup.blocksRaycasts = false;
            _pageGroup.interactable = false;
            _pageGroup.gameObject.SetActive(false);
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

        private void SetSideFadeVisible(bool visible)
        {
            _sideSmokeFadeOverlay?.SetVisible(visible);
        }

        private ComicPose CalculatePose(Sprite sprite, Rect focusRect, float zoom, Vector2 zoomOffset)
        {
            Vector2 viewportSize = GetViewportSize();
            Rect spriteRect = sprite.rect;
            float focusWidth = Mathf.Max(1f, spriteRect.width * focusRect.width);
            float focusHeight = Mathf.Max(1f, spriteRect.height * focusRect.height);
            float scale = Mathf.Max(viewportSize.x / focusWidth, viewportSize.y / focusHeight);
            scale *= Mathf.Max(0.01f, zoom);

            Vector2 imageSize = new(spriteRect.width * scale, spriteRect.height * scale);
            Vector2 focusCenter = focusRect.center;
            Vector2 localFocusCenter = new(
                (focusCenter.x - 0.5f) * imageSize.x,
                (focusCenter.y - 0.5f) * imageSize.y);
            Vector2 viewportOffset = new(viewportSize.x * zoomOffset.x, viewportSize.y * zoomOffset.y);

            return new ComicPose(sprite, imageSize, -localFocusCenter + viewportOffset);
        }

        private Vector2 GetViewportSize()
        {
            Canvas.ForceUpdateCanvases();

            RectTransform parentRect = _pageGroup != null ? _pageGroup.transform as RectTransform : null;
            if (parentRect != null)
            {
                Vector2 size = parentRect.rect.size;
                if (size.x > 0.01f && size.y > 0.01f)
                    return size;
            }

            return new Vector2(Mathf.Max(1, Screen.width), Mathf.Max(1, Screen.height));
        }

        private void ApplyPose(ComicPose pose)
        {
            _pageImage.sprite = pose.Sprite;
            _pageImage.enabled = pose.Sprite != null;
            _pageImageRect.sizeDelta = pose.Size;
            _pageImageRect.anchoredPosition = pose.Position;
        }

        private IEnumerator TransitionToShot(ComicCinematicAsset cinematic, int shotIndex)
        {
            ComicCinematicShot shot = cinematic.GetShot(shotIndex);
            if (shot == null)
                yield break;

            Sprite page = ResolvePage(cinematic, shot);
            if (page == null)
                yield break;

            ComicPose targetPose = CalculatePose(page, shot.NormalizedFocus, shot.GetZoom(cinematic.DefaultZoom), shot.ZoomOffset);
            ComicCinematicTransitionMode transition = shot.Transition;

            if (_pageImage.sprite != page && transition == ComicCinematicTransitionMode.SmoothPan)
                transition = ComicCinematicTransitionMode.FadeThroughBlack;

            switch (transition)
            {
                case ComicCinematicTransitionMode.Cut:
                    ApplyPose(targetPose);
                    BlockInputBriefly();
                    break;

                case ComicCinematicTransitionMode.FadeThroughBlack:
                    float fadeDuration = shot.GetFadeThroughBlackDuration(cinematic.DefaultFadeThroughBlackDuration);
                    yield return Fade(_fadeGroup, 1f, fadeDuration * 0.5f, true);
                    ApplyPose(targetPose);
                    yield return Fade(_fadeGroup, 0f, fadeDuration * 0.5f, true);
                    break;

                default:
                    yield return AnimatePose(targetPose, shot.GetMoveDuration(cinematic.DefaultMoveDuration), shot.Ease);
                    break;
            }
        }

        private IEnumerator AnimatePose(ComicPose targetPose, float duration, ComicCinematicEase ease)
        {
            if (_pageImage.sprite != targetPose.Sprite || duration <= 0.01f)
            {
                ApplyPose(targetPose);
                BlockInputBriefly();
                yield break;
            }

            _isFading = true;
            Vector2 startSize = _pageImageRect.sizeDelta;
            Vector2 startPosition = _pageImageRect.anchoredPosition;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float progress = Mathf.Clamp01(elapsed / duration);
                float eased = EvaluateEase(progress, ease);
                _pageImageRect.sizeDelta = Vector2.LerpUnclamped(startSize, targetPose.Size, eased);
                _pageImageRect.anchoredPosition = Vector2.LerpUnclamped(startPosition, targetPose.Position, eased);
                yield return null;
            }

            ApplyPose(targetPose);
            _isFading = false;
            BlockInputBriefly();
        }

        private IEnumerator WaitForAdvance(ComicCinematicAsset cinematic, int shotIndex)
        {
            _navigationDirection = NavigationDirection.Next;
            _navigationRequestPending = false;
            float elapsed = 0f;

            while (true)
            {
                ComicCinematicShot shot = cinematic.GetShot(shotIndex);
                if (shot == null)
                {
                    _navigationDirection = NavigationDirection.Next;
                    yield break;
                }

                RefreshCurrentShotFromAsset(cinematic, shotIndex);

                bool canUseInput = shot.AdvanceMode != ComicCinematicAdvanceMode.AutoOnly;
                if (canUseInput && ConsumeNavigationRequest(out NavigationDirection requestedDirection))
                {
                    _navigationDirection = requestedDirection;
                    yield break;
                }

                if (canUseInput && AdvancePressed())
                {
                    _navigationDirection = NavigationDirection.Next;
                    yield break;
                }

                if (canUseInput && PreviousPressed())
                {
                    _navigationDirection = NavigationDirection.Previous;
                    yield break;
                }

                float holdSeconds = shot.GetHoldDuration(cinematic.DefaultHoldDuration);
                if (shot.AdvanceMode != ComicCinematicAdvanceMode.InputOnly && elapsed >= holdSeconds)
                {
                    _navigationDirection = NavigationDirection.Next;
                    yield break;
                }

                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }
        }

        private void RefreshCurrentShotFromAsset(ComicCinematicAsset cinematic, int shotIndex)
        {
            if (!ShouldLiveUpdateAsset(cinematic) || _isFading || _pageImage == null || _pageImageRect == null)
                return;

            ComicCinematicShot shot = cinematic.GetShot(shotIndex);
            if (shot == null)
                return;

            if (_pageBackground != null)
                _pageBackground.color = new Color(0f, 0f, 0f, cinematic.BackgroundAlpha);

            Sprite page = ResolvePage(cinematic, shot, false);
            if (page == null)
                return;

            ApplyPose(CalculatePose(page, shot.NormalizedFocus, shot.GetZoom(cinematic.DefaultZoom), shot.ZoomOffset));
        }

        private void SetActiveShot(ComicCinematicAsset cinematic, int shotIndex)
        {
            _activeCinematic = cinematic;
            _activeShotIndex = shotIndex;
        }

        private void ClearActiveShot()
        {
            _activeCinematic = null;
            _activeShotIndex = -1;
        }

        private bool ShouldLiveUpdateAsset(ComicCinematicAsset cinematic)
        {
            return _allowLiveAssetUpdates && cinematic != null && cinematic.LiveUpdateWhilePlaying;
        }

        private bool AdvancePressed()
        {
            if (!CanReadNavigationInput())
                return false;

            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
                return true;

            if (Keyboard.current != null
                && (Keyboard.current.dKey.wasPressedThisFrame
                    || Keyboard.current.rightArrowKey.wasPressedThisFrame
                    || Keyboard.current.spaceKey.wasPressedThisFrame
                    || Keyboard.current.enterKey.wasPressedThisFrame))
                return true;

            return Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame;
        }

        private bool PreviousPressed()
        {
            if (!CanReadNavigationInput())
                return false;

            return Keyboard.current != null
                && (Keyboard.current.aKey.wasPressedThisFrame
                    || Keyboard.current.leftArrowKey.wasPressedThisFrame
                    || Keyboard.current.backspaceKey.wasPressedThisFrame);
        }

        private bool CanReadNavigationInput()
        {
            return !_isFading && Time.unscaledTime >= _inputBlockedUntil;
        }

        private void RequestNextShot()
        {
            RequestNavigation(NavigationDirection.Next);
        }

        private void RequestPreviousShot()
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
            BlockInputBriefly();
        }

        private void BlockInputBriefly()
        {
            _inputBlockedUntil = Mathf.Max(_inputBlockedUntil, Time.unscaledTime + _postTransitionInputBlockSeconds);
        }

        private static float EvaluateEase(float progress, ComicCinematicEase ease)
        {
            switch (ease)
            {
                case ComicCinematicEase.Linear:
                    return progress;

                case ComicCinematicEase.EaseInOutSine:
                    return 0.5f - Mathf.Cos(progress * Mathf.PI) * 0.5f;

                case ComicCinematicEase.EaseOutCubic:
                    float inverse = 1f - progress;
                    return 1f - inverse * inverse * inverse;

                case ComicCinematicEase.SmoothStep:
                    return progress * progress * (3f - 2f * progress);

                default:
                    return progress * progress * progress * (progress * (progress * 6f - 15f) + 10f);
            }
        }

        private int FindFirstShotIndex(ComicCinematicAsset cinematic)
        {
            for (int i = 0; i < cinematic.ShotCount; i++)
            {
                if (cinematic.GetShot(i) != null)
                    return i;
            }

            return -1;
        }

        private int GetNextShotIndex(ComicCinematicAsset cinematic, int currentIndex, NavigationDirection direction)
        {
            int step = direction == NavigationDirection.Previous ? -1 : 1;
            int index = currentIndex + step;

            while (index >= 0 && index < cinematic.ShotCount)
            {
                if (cinematic.GetShot(index) != null)
                    return index;

                index += step;
            }

            return direction == NavigationDirection.Previous ? currentIndex : cinematic.ShotCount;
        }

        private Sprite ResolvePage(ComicCinematicAsset cinematic, ComicCinematicShot shot, bool warnIfMissing = true)
        {
            Sprite page = shot.PageOverride != null ? shot.PageOverride : cinematic.DefaultPage;
            if (page == null && warnIfMissing)
                Debug.LogWarning($"[ComicCinematics] El shot en {cinematic.name} no tiene pagina asignada.", this);

            return page;
        }

        private ComicCinematicAsset LoadComicCinematic(string resourceName)
        {
            if (string.IsNullOrWhiteSpace(resourceName))
                return null;

            ComicCinematicAsset cinematic = Resources.Load<ComicCinematicAsset>(resourceName);
            if (cinematic != null)
                return cinematic;

            cinematic = Resources.Load<ComicCinematicAsset>(ResourceRoot + resourceName);
            if (cinematic == null)
                Debug.LogWarning($"[ComicCinematics] No se encontro Resources/{ResourceRoot}{resourceName}.", this);

            return cinematic;
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

        private void InvokeBlackBeforeReveal(Action onBlackBeforeReveal)
        {
            if (onBlackBeforeReveal == null)
                return;

            try
            {
                onBlackBeforeReveal.Invoke();
            }
            catch (Exception exception)
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

        private static void ClearResidualComicFades()
        {
            CanvasGroup[] groups = Resources.FindObjectsOfTypeAll<CanvasGroup>();
            for (int i = 0; i < groups.Length; i++)
            {
                CanvasGroup group = groups[i];
                if (group == null || group.gameObject.name != "Comic Black Fade")
                    continue;

                if (!group.gameObject.scene.IsValid())
                    continue;

                group.alpha = 0f;
                group.blocksRaycasts = false;
                group.interactable = false;
            }
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

        private readonly struct ComicPose
        {
            public readonly Sprite Sprite;
            public readonly Vector2 Size;
            public readonly Vector2 Position;

            public ComicPose(Sprite sprite, Vector2 size, Vector2 position)
            {
                Sprite = sprite;
                Size = size;
                Position = position;
            }
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
