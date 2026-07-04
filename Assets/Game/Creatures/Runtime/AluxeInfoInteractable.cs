using System.Collections;
using GameJam.Player.Platform;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
#endif

namespace GameJam.Creatures
{
    [DisallowMultipleComponent]
    public sealed class AluxeInfoInteractable : MonoBehaviour
    {
        [Header("Content")]
        [SerializeField] private Sprite _infoCardSprite;
        [SerializeField] private string _nearbyMessage = "Shh! quieres saber algo?";

        [Header("Interaction")]
        [SerializeField] private Transform _player;
        [SerializeField] private bool _autoFindPlayer = true;
        [SerializeField, Min(0.1f)] private float _interactionDistance = 3f;
        [SerializeField] private bool _closeInfoWhenLeavingRange = true;
        [SerializeField] private bool _showNearbyBubble = true;

        [Header("Pause")]
        [SerializeField] private bool _pauseTimeWhileShowingInfo = true;
        [SerializeField] private bool _lockPlayerMovementWhileShowingInfo = true;

        [Header("Bubble")]
        [SerializeField] private Vector3 _bubbleOffset = new(0f, 4.35f, 0f);
        [SerializeField] private Vector2 _bubbleSize = new(330f, 92f);
        [SerializeField, Min(0.001f)] private float _bubbleWorldScale = 0.01f;

        [Header("References")]
        [SerializeField] private GameJam.Input.GameInput _gameInput;
        [SerializeField] private Collider2D _clickTarget;
        [SerializeField] private SpriteRenderer _spriteRenderer;

        [Header("Overlay Buttons")]
        [SerializeField] private Sprite _closeButtonSprite;
        [SerializeField] private Sprite _closeButtonHighlightedSprite;
        [SerializeField] private Vector2 _closeButtonSize = new(96f, 96f);

        private Canvas _bubbleCanvas;
        private TMP_Text _bubbleText;
        private GameObject _overlayRoot;
        private Image _overlayCardImage;
        private CanvasGroup _overlayGroup;
        private Coroutine _temporaryBubbleRoutine;
        private string _temporaryBubbleMessage;
        private bool _isNearby;
        private bool _isShowingInfo;
        private bool _hasTemporaryBubbleMessage;
        private InfoOverlayPauseLock _infoOverlayPauseLock;

        private void Reset()
        {
            _clickTarget = GetComponent<Collider2D>();
            _spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }

        private void Awake()
        {
            if (_clickTarget == null)
                _clickTarget = GetComponent<Collider2D>();

            if (_spriteRenderer == null)
                _spriteRenderer = GetComponentInChildren<SpriteRenderer>();

            if (_gameInput == null)
                _gameInput = FindFirstObjectByType<GameJam.Input.GameInput>();

            ResolvePlayer();
            CreateBubble();
            SetBubbleVisible(false);
        }

        private void OnEnable()
        {
            if (_gameInput != null)
                _gameInput.ActionPressed += OnActionPressed;
        }

        private void OnDisable()
        {
            if (_gameInput != null)
                _gameInput.ActionPressed -= OnActionPressed;

            if (_isShowingInfo)
                HideInfoCard();
        }

        private void OnDestroy()
        {
            ReleaseInfoOverlayPause();

            if (_overlayRoot != null)
                Destroy(_overlayRoot);
        }

        private void Update()
        {
            if (_player == null && _autoFindPlayer)
                ResolvePlayer();

            bool isNearby = IsPlayerNearby();
            if (isNearby != _isNearby)
            {
                _isNearby = isNearby;
                if (!_hasTemporaryBubbleMessage)
                    SetBubbleVisible(ShouldShowNearbyBubble());
            }

            if (!_isNearby)
            {
                if (_isShowingInfo && _closeInfoWhenLeavingRange)
                    HideInfoCard();

                return;
            }

            if (!_isShowingInfo && WasKeyboardInteractPressed())
                ShowInfoCard();

            if (!_isShowingInfo && WasPointerPressedOnAluxe())
                ShowInfoCard();

            if (_isShowingInfo && WasClosePressed())
                HideInfoCard();
        }

        private void LateUpdate()
        {
            if (_bubbleCanvas == null || !_bubbleCanvas.gameObject.activeSelf)
                return;

            Camera camera = Camera.main;
            if (camera == null)
                return;

            Transform bubbleTransform = _bubbleCanvas.transform;
            Vector3 forward = bubbleTransform.position - camera.transform.position;
            if (forward.sqrMagnitude > 0.001f)
                bubbleTransform.rotation = Quaternion.LookRotation(forward, camera.transform.up);
        }

        public void SetInfoCard(Sprite infoCardSprite)
        {
            _infoCardSprite = infoCardSprite;

            if (_overlayCardImage != null)
                _overlayCardImage.sprite = _infoCardSprite;
        }

        public void ShowInfoCard()
        {
            if (!_isNearby || _infoCardSprite == null)
                return;

            EnsureOverlay();

            _overlayCardImage.sprite = _infoCardSprite;
            _overlayRoot.SetActive(true);
            _overlayGroup.alpha = 1f;
            _overlayGroup.interactable = true;
            _overlayGroup.blocksRaycasts = true;
            _isShowingInfo = true;
            AcquireInfoOverlayPause();
            SetBubbleVisible(false);
        }

        public void HideInfoCard()
        {
            if (_overlayRoot != null)
                _overlayRoot.SetActive(false);

            if (_overlayGroup != null)
            {
                _overlayGroup.alpha = 0f;
                _overlayGroup.interactable = false;
                _overlayGroup.blocksRaycasts = false;
            }

            _isShowingInfo = false;
            ReleaseInfoOverlayPause();
            SetBubbleVisible(_hasTemporaryBubbleMessage || ShouldShowNearbyBubble());
        }

        public void ShowBubbleMessage(string message, float duration)
        {
            if (string.IsNullOrWhiteSpace(message))
                message = _nearbyMessage;

            if (_bubbleCanvas == null)
                CreateBubble();

            if (_temporaryBubbleRoutine != null)
                StopCoroutine(_temporaryBubbleRoutine);

            _temporaryBubbleMessage = message;
            _hasTemporaryBubbleMessage = true;
            SetBubbleVisible(true);

            if (duration > 0f)
                _temporaryBubbleRoutine = StartCoroutine(ClearTemporaryBubbleMessageAfter(duration));
        }

        private void OnActionPressed(GameJam.Input.GameAction action)
        {
            if (action == GameJam.Input.GameAction.Interact && _isNearby)
                ShowInfoCard();
        }

        private void ResolvePlayer()
        {
            if (!_autoFindPlayer || _player != null)
                return;

            Platform_PlayerController platformPlayer = FindFirstObjectByType<Platform_PlayerController>();
            if (platformPlayer != null)
            {
                _player = platformPlayer.transform;
                return;
            }

            GameObject taggedPlayer = GameObject.FindWithTag("Player");
            if (taggedPlayer != null)
                _player = taggedPlayer.transform;
        }

        private bool IsPlayerNearby()
        {
            if (_player == null)
                return false;

            return Vector2.Distance(transform.position, _player.position) <= _interactionDistance;
        }

        private void AcquireInfoOverlayPause()
        {
            if (_infoOverlayPauseLock != null)
                return;

            _infoOverlayPauseLock = InfoOverlayPauseLock.Acquire(
                _player,
                _pauseTimeWhileShowingInfo,
                _lockPlayerMovementWhileShowingInfo);
        }

        private void ReleaseInfoOverlayPause()
        {
            if (_infoOverlayPauseLock == null)
                return;

            _infoOverlayPauseLock.Release();
            _infoOverlayPauseLock = null;
        }

        private void CreateBubble()
        {
            GameObject bubble = new("Speech Bubble", typeof(RectTransform), typeof(Canvas));
            bubble.transform.SetParent(transform, false);
            bubble.transform.localPosition = _bubbleOffset;
            bubble.transform.localScale = Vector3.one * _bubbleWorldScale;

            _bubbleCanvas = bubble.GetComponent<Canvas>();
            _bubbleCanvas.renderMode = RenderMode.WorldSpace;
            _bubbleCanvas.overrideSorting = true;
            _bubbleCanvas.sortingOrder = 1000;

            RectTransform bubbleRect = bubble.GetComponent<RectTransform>();
            bubbleRect.sizeDelta = _bubbleSize;

            GameObject background = new("Background", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            background.transform.SetParent(bubble.transform, false);
            RectTransform backgroundRect = background.GetComponent<RectTransform>();
            backgroundRect.anchorMin = Vector2.zero;
            backgroundRect.anchorMax = Vector2.one;
            backgroundRect.offsetMin = Vector2.zero;
            backgroundRect.offsetMax = Vector2.zero;

            Image backgroundImage = background.GetComponent<Image>();
            backgroundImage.color = new Color(0.98f, 0.91f, 0.72f, 0.95f);
            backgroundImage.raycastTarget = false;

            GameObject text = new("Message", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            text.transform.SetParent(background.transform, false);
            RectTransform textRect = text.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(18f, 10f);
            textRect.offsetMax = new Vector2(-18f, -10f);

            _bubbleText = text.GetComponent<TextMeshProUGUI>();
            _bubbleText.text = _nearbyMessage;
            _bubbleText.color = new Color(0.12f, 0.08f, 0.05f, 1f);
            _bubbleText.fontSize = 24f;
            _bubbleText.alignment = TextAlignmentOptions.Center;
            _bubbleText.textWrappingMode = TextWrappingModes.Normal;
            _bubbleText.raycastTarget = false;
        }

        private IEnumerator ClearTemporaryBubbleMessageAfter(float duration)
        {
            yield return new WaitForSeconds(duration);

            _temporaryBubbleRoutine = null;
            _hasTemporaryBubbleMessage = false;
            _temporaryBubbleMessage = null;
            SetBubbleVisible(ShouldShowNearbyBubble());
        }

        private bool ShouldShowNearbyBubble()
        {
            return _showNearbyBubble && _isNearby && !_isShowingInfo;
        }

        private void SetBubbleVisible(bool visible)
        {
            if (_bubbleText != null)
                _bubbleText.text = _hasTemporaryBubbleMessage ? _temporaryBubbleMessage : _nearbyMessage;

            if (_bubbleCanvas != null)
                _bubbleCanvas.gameObject.SetActive(visible);
        }

        private void EnsureOverlay()
        {
            if (_overlayRoot != null)
                return;

            EnsureEventSystem();

            _overlayRoot = new GameObject("Aluxe Info Overlay", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(CanvasGroup));
            DontDestroyOnLoad(_overlayRoot);

            Canvas canvas = _overlayRoot.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 300;

            CanvasScaler scaler = _overlayRoot.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            _overlayGroup = _overlayRoot.GetComponent<CanvasGroup>();

            RectTransform rootRect = _overlayRoot.GetComponent<RectTransform>();
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;

            GameObject dimmer = CreateOverlayImage("Dimmer", _overlayRoot.transform, new Color(0f, 0f, 0f, 0.72f));
            RectTransform dimmerRect = dimmer.GetComponent<RectTransform>();
            dimmerRect.anchorMin = Vector2.zero;
            dimmerRect.anchorMax = Vector2.one;
            dimmerRect.offsetMin = Vector2.zero;
            dimmerRect.offsetMax = Vector2.zero;

            Button dimmerButton = dimmer.AddComponent<Button>();
            dimmerButton.transition = Selectable.Transition.None;
            dimmerButton.onClick.AddListener(HideInfoCard);

            GameObject card = new("Info Card", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            card.transform.SetParent(_overlayRoot.transform, false);
            RectTransform cardRect = card.GetComponent<RectTransform>();
            cardRect.anchorMin = new Vector2(0.05f, 0.08f);
            cardRect.anchorMax = new Vector2(0.95f, 0.92f);
            cardRect.offsetMin = Vector2.zero;
            cardRect.offsetMax = Vector2.zero;

            _overlayCardImage = card.GetComponent<Image>();
            _overlayCardImage.sprite = _infoCardSprite;
            _overlayCardImage.preserveAspect = true;
            _overlayCardImage.raycastTarget = false;

            Button closeButton = CreateSpriteButton(
                "Close",
                _overlayRoot.transform,
                _closeButtonSprite,
                _closeButtonHighlightedSprite,
                new Vector2(1f, 1f),
                new Vector2(1f, 1f),
                new Vector2(-26f, -24f),
                _closeButtonSize,
                "X");
            closeButton.onClick.AddListener(HideInfoCard);

            _overlayRoot.SetActive(false);
        }

        private static GameObject CreateOverlayImage(string name, Transform parent, Color color)
        {
            GameObject gameObject = new(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            gameObject.transform.SetParent(parent, false);

            Image image = gameObject.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = true;

            return gameObject;
        }

        private static Button CreateSpriteButton(
            string name,
            Transform parent,
            Sprite sprite,
            Sprite highlightedSprite,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 position,
            Vector2 size,
            string fallbackLabel)
        {
            GameObject buttonObject = CreateOverlayImage(name, parent, sprite == null ? new Color(0.28f, 0.05f, 0.04f, 0.94f) : Color.white);
            RectTransform rect = buttonObject.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = new Vector2(anchorMin.x, anchorMin.y);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;

            Image image = buttonObject.GetComponent<Image>();
            image.sprite = sprite;
            image.preserveAspect = true;

            Button button = buttonObject.AddComponent<Button>();
            button.targetGraphic = image;

            if (highlightedSprite != null)
            {
                button.transition = Selectable.Transition.SpriteSwap;
                SpriteState spriteState = button.spriteState;
                spriteState.highlightedSprite = highlightedSprite;
                spriteState.pressedSprite = highlightedSprite;
                spriteState.selectedSprite = highlightedSprite;
                button.spriteState = spriteState;
            }
            else
            {
                button.transition = Selectable.Transition.ColorTint;
            }

            if (sprite == null && !string.IsNullOrWhiteSpace(fallbackLabel))
            {
                GameObject textObject = new("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
                textObject.transform.SetParent(buttonObject.transform, false);
                RectTransform textRect = textObject.GetComponent<RectTransform>();
                textRect.anchorMin = Vector2.zero;
                textRect.anchorMax = Vector2.one;
                textRect.offsetMin = Vector2.zero;
                textRect.offsetMax = Vector2.zero;

                TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
                text.text = fallbackLabel;
                text.color = Color.white;
                text.fontSize = 38f;
                text.alignment = TextAlignmentOptions.Center;
                text.raycastTarget = false;
            }

            return button;
        }

        private static void EnsureEventSystem()
        {
            if (EventSystem.current != null)
                return;

            GameObject eventSystem = new("EventSystem");
            eventSystem.AddComponent<EventSystem>();
#if ENABLE_INPUT_SYSTEM
            eventSystem.AddComponent<InputSystemUIInputModule>();
#else
            eventSystem.AddComponent<StandaloneInputModule>();
#endif
        }

        private bool WasKeyboardInteractPressed()
        {
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
                return true;
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
            if (UnityEngine.Input.GetKeyDown(KeyCode.E))
                return true;
#endif

            return false;
        }

        private static bool WasClosePressed()
        {
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
                return true;
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
            if (UnityEngine.Input.GetKeyDown(KeyCode.Escape))
                return true;
#endif

            return false;
        }

        private bool WasPointerPressedOnAluxe()
        {
            if (!TryGetPointerPressPosition(out Vector2 screenPosition, out int pointerId))
                return false;

            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(pointerId))
                return false;

            Camera camera = Camera.main;
            if (camera == null)
                return false;

            Vector3 world = camera.ScreenToWorldPoint(screenPosition);
            Vector2 world2D = new(world.x, world.y);

            if (_clickTarget != null)
                return _clickTarget.OverlapPoint(world2D);

            return _spriteRenderer != null && _spriteRenderer.bounds.Contains(world);
        }

        private static bool TryGetPointerPressPosition(out Vector2 screenPosition, out int pointerId)
        {
#if ENABLE_INPUT_SYSTEM
            if (Touchscreen.current != null)
            {
                var touch = Touchscreen.current.primaryTouch;
                if (touch.press.wasPressedThisFrame)
                {
                    screenPosition = touch.position.ReadValue();
                    pointerId = touch.touchId.ReadValue();
                    return true;
                }
            }

            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            {
                screenPosition = Mouse.current.position.ReadValue();
                pointerId = -1;
                return true;
            }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
            if (UnityEngine.Input.GetMouseButtonDown(0))
            {
                screenPosition = UnityEngine.Input.mousePosition;
                pointerId = -1;
                return true;
            }
#endif

            screenPosition = default;
            pointerId = -1;
            return false;
        }
    }
}
