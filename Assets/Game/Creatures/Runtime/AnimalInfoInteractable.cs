using GameJam.Gameplay.PlatformObjectives;
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
    public sealed class AnimalInfoInteractable : MonoBehaviour
    {
        [Header("Content")]
        [SerializeField] private Sprite[] _infoCards = new Sprite[0];
        [SerializeField] private int _firstCardIndex;

        [Header("Interaction")]
        [SerializeField] private Transform _player;
        [SerializeField] private bool _autoFindPlayer = true;
        [SerializeField, Min(0.1f)] private float _interactionDistance = 3f;
        [SerializeField] private bool _openAutomaticallyOnce = true;
        [SerializeField] private bool _closeInfoWhenLeavingRange;

        [Header("Pause")]
        [SerializeField] private bool _pauseTimeWhileShowingInfo = true;
        [SerializeField] private bool _lockPlayerMovementWhileShowingInfo = true;

        [Header("Objective Discovery")]
        [SerializeField] private bool _countsAsObjectiveDiscovery = true;
        [SerializeField] private string _objectiveDiscoveryId;

        [Header("References")]
        [SerializeField] private GameJam.Input.GameInput _gameInput;
        [SerializeField] private Collider2D _clickTarget;
        [SerializeField] private SpriteRenderer _spriteRenderer;

        [Header("Overlay Buttons")]
        [SerializeField] private Sprite _previousButtonSprite;
        [SerializeField] private Sprite _previousButtonHighlightedSprite;
        [SerializeField] private Sprite _nextButtonSprite;
        [SerializeField] private Sprite _nextButtonHighlightedSprite;
        [SerializeField] private Sprite _closeButtonSprite;
        [SerializeField] private Sprite _closeButtonHighlightedSprite;
        [SerializeField] private Vector2 _navigationButtonSize = new(132f, 132f);
        [SerializeField] private Vector2 _closeButtonSize = new(96f, 96f);

        private GameObject _overlayRoot;
        private Image _overlayCardImage;
        private TMP_Text _pageLabel;
        private Button _previousButton;
        private Button _nextButton;
        private CanvasGroup _overlayGroup;
        private bool _isNearby;
        private bool _hasOpenedAutomatically;
        private bool _isShowingInfo;
        private bool _hasRegisteredObjectiveDiscovery;
        private InfoOverlayPauseLock _infoOverlayPauseLock;
        private int _currentCardIndex;

        public string ObjectiveDiscoveryId => string.IsNullOrWhiteSpace(_objectiveDiscoveryId)
            ? gameObject.name
            : _objectiveDiscoveryId;

        private void Reset()
        {
            _spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            ResolveClickTarget();
        }

        private void Awake()
        {
            if (_spriteRenderer == null)
                _spriteRenderer = GetComponentInChildren<SpriteRenderer>();

            ResolveClickTarget();

            if (_gameInput == null)
                _gameInput = FindFirstObjectByType<GameJam.Input.GameInput>();

            ResolvePlayer();
        }

        private void ResolveClickTarget()
        {
            if (_clickTarget != null)
                return;

            if (_spriteRenderer != null)
            {
                _clickTarget = _spriteRenderer.GetComponent<Collider2D>();
                if (_clickTarget == null)
                    _clickTarget = _spriteRenderer.GetComponentInChildren<Collider2D>(true);
            }

            if (_clickTarget == null)
                _clickTarget = GetComponentInChildren<Collider2D>(true);
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
                HideInfoCards();
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

                if (_isNearby && _openAutomaticallyOnce && !_hasOpenedAutomatically)
                {
                    _hasOpenedAutomatically = true;
                    ShowInfoCards();
                }
            }

            if (!_isNearby)
            {
                if (_isShowingInfo && _closeInfoWhenLeavingRange)
                    HideInfoCards();

                return;
            }

            if (!_isShowingInfo && WasKeyboardInteractPressed())
                ShowInfoCards();

            if (!_isShowingInfo && WasPointerPressedOnAnimal())
                ShowInfoCards();

            if (_isShowingInfo && WasClosePressed())
                HideInfoCards();
        }

        public void SetInfoCards(Sprite[] infoCards)
        {
            _infoCards = infoCards ?? new Sprite[0];
            _currentCardIndex = Mathf.Clamp(_currentCardIndex, 0, Mathf.Max(0, _infoCards.Length - 1));
            ApplyCurrentCard();
        }

        public void ShowInfoCards()
        {
            if (!_isNearby || _infoCards == null || _infoCards.Length == 0)
                return;

            EnsureOverlay();

            _currentCardIndex = Mathf.Clamp(_firstCardIndex, 0, _infoCards.Length - 1);
            ApplyCurrentCard();
            _overlayRoot.SetActive(true);
            _overlayGroup.alpha = 1f;
            _overlayGroup.interactable = true;
            _overlayGroup.blocksRaycasts = true;
            _isShowingInfo = true;
            AcquireInfoOverlayPause();
            RegisterObjectiveDiscovery();
        }

        public void HideInfoCards()
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
        }

        private void OnActionPressed(GameJam.Input.GameAction action)
        {
            if (action == GameJam.Input.GameAction.Interact && _isNearby)
                ShowInfoCards();
        }

        private void RegisterObjectiveDiscovery()
        {
            if (!_countsAsObjectiveDiscovery || _hasRegisteredObjectiveDiscovery)
                return;

            PlatformAluxHouseGate objective = PlatformAluxHouseGate.Active;
            if (objective == null)
                return;

            objective.RegisterAnimalDiscovery(ObjectiveDiscoveryId);
            _hasRegisteredObjectiveDiscovery = true;
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

            Vector2 playerPosition = _player.position;
            if (_clickTarget != null && _clickTarget.enabled)
            {
                Vector2 closestPoint = _clickTarget.ClosestPoint(playerPosition);
                return Vector2.Distance(playerPosition, closestPoint) <= _interactionDistance;
            }

            if (_spriteRenderer != null)
            {
                Bounds bounds = _spriteRenderer.bounds;
                Vector3 closestPoint = bounds.ClosestPoint(_player.position);
                return Vector2.Distance(playerPosition, closestPoint) <= _interactionDistance;
            }

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

        private void EnsureOverlay()
        {
            if (_overlayRoot != null)
                return;

            EnsureEventSystem();

            _overlayRoot = new GameObject($"{name} Info Overlay", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(CanvasGroup));

            Canvas canvas = _overlayRoot.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 320;

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
            dimmerButton.onClick.AddListener(HideInfoCards);

            GameObject card = new("Info Card", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            card.transform.SetParent(_overlayRoot.transform, false);
            RectTransform cardRect = card.GetComponent<RectTransform>();
            cardRect.anchorMin = new Vector2(0.05f, 0.1f);
            cardRect.anchorMax = new Vector2(0.95f, 0.9f);
            cardRect.offsetMin = Vector2.zero;
            cardRect.offsetMax = Vector2.zero;

            _overlayCardImage = card.GetComponent<Image>();
            _overlayCardImage.preserveAspect = true;
            _overlayCardImage.raycastTarget = false;

            _previousButton = CreateSpriteButton(
                "Previous",
                _overlayRoot.transform,
                _previousButtonSprite,
                _previousButtonHighlightedSprite,
                new Vector2(0f, 0.5f),
                new Vector2(0f, 0.5f),
                new Vector2(24f, 0f),
                _navigationButtonSize);
            _previousButton.onClick.AddListener(ShowPreviousCard);

            _nextButton = CreateSpriteButton(
                "Next",
                _overlayRoot.transform,
                _nextButtonSprite,
                _nextButtonHighlightedSprite,
                new Vector2(1f, 0.5f),
                new Vector2(1f, 0.5f),
                new Vector2(-24f, 0f),
                _navigationButtonSize);
            _nextButton.onClick.AddListener(ShowNextCard);

            GameObject page = new("Page Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            page.transform.SetParent(_overlayRoot.transform, false);
            RectTransform pageRect = page.GetComponent<RectTransform>();
            pageRect.anchorMin = new Vector2(0.5f, 0f);
            pageRect.anchorMax = new Vector2(0.5f, 0f);
            pageRect.pivot = new Vector2(0.5f, 0f);
            pageRect.anchoredPosition = new Vector2(0f, 28f);
            pageRect.sizeDelta = new Vector2(240f, 48f);

            _pageLabel = page.GetComponent<TextMeshProUGUI>();
            _pageLabel.color = Color.white;
            _pageLabel.fontSize = 26f;
            _pageLabel.alignment = TextAlignmentOptions.Center;
            _pageLabel.raycastTarget = false;

            Button closeButton = CreateSpriteButton(
                "Close",
                _overlayRoot.transform,
                _closeButtonSprite,
                _closeButtonHighlightedSprite,
                new Vector2(1f, 1f),
                new Vector2(1f, 1f),
                new Vector2(-26f, -24f),
                _closeButtonSize);
            closeButton.onClick.AddListener(HideInfoCards);

            _overlayRoot.SetActive(false);
        }

        private void ShowPreviousCard()
        {
            if (_infoCards == null || _infoCards.Length == 0 || _currentCardIndex <= 0)
                return;

            _currentCardIndex--;
            ApplyCurrentCard();
        }

        private void ShowNextCard()
        {
            if (_infoCards == null || _infoCards.Length == 0 || _currentCardIndex >= _infoCards.Length - 1)
                return;

            _currentCardIndex++;
            ApplyCurrentCard();
        }

        private void ApplyCurrentCard()
        {
            if (_overlayCardImage != null && _infoCards != null && _infoCards.Length > 0)
                _overlayCardImage.sprite = _infoCards[_currentCardIndex];

            if (_pageLabel != null)
                _pageLabel.text = _infoCards == null || _infoCards.Length == 0 ? string.Empty : $"{_currentCardIndex + 1} / {_infoCards.Length}";

            bool hasMultipleCards = _infoCards != null && _infoCards.Length > 1;
            if (_previousButton != null)
                _previousButton.gameObject.SetActive(hasMultipleCards && _currentCardIndex > 0);

            if (_nextButton != null)
                _nextButton.gameObject.SetActive(hasMultipleCards && _currentCardIndex < _infoCards.Length - 1);
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
            Vector2 size)
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

        private bool WasPointerPressedOnAnimal()
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
