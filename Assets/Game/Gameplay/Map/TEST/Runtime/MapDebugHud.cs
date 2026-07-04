using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace GameJam.Gameplay.Map
{
    [DisallowMultipleComponent]
    public sealed class MapDebugHud : MonoBehaviour
    {
        private const string MinigameInteractableTypeName = "GameJam.Gameplay.Minigames.MinigameInteractableObject";

        [Header("Sources")]
        [SerializeField] private MapDiscoverySystem _discovery;
        [Tooltip("Defaults to the transform tracked by MapDiscoverySystem.")]
        [SerializeField] private Transform _player;
        [Tooltip("Visual texture used only by this UI map. Leave empty to reveal the traversable mask.")]
        [SerializeField] private Texture _mapTextureOverride;
        [SerializeField] private Sprite _openButtonSprite;

        [Header("Layout")]
        [SerializeField] private Vector2 _mapPanelSize = new Vector2(1500f, 842f);
        [SerializeField] private bool _preserveMapAspectRatio = true;
        [SerializeField, Min(0f)] private float _screenMargin = 20f;
        [SerializeField] private Vector2 _openButtonSize = new Vector2(92f, 92f);
        [SerializeField] private Vector2 _closeButtonSize = new Vector2(64f, 64f);
        [SerializeField] private int _sortingOrder = 1000;
        [SerializeField] private bool _pauseGameWhenOpen = true;

        [Header("Appearance")]
        [SerializeField, Min(1f)] private float _playerMarkerDiameter = 14f;
        [SerializeField] private Color _playerMarkerColor = Color.red;
        [SerializeField, Min(1f)] private float _minigameMarkerDiameter = 12f;
        [SerializeField] private Color _minigameMarkerColor = new Color(1f, 0.48f, 0.05f, 1f);
        [SerializeField] private Color _panelBackgroundColor = new Color(0f, 0f, 0f, 0.78f);
        [SerializeField] private Color _labelColor = Color.white;
        [SerializeField] private Color _buttonFallbackColor = new Color(0.1f, 0.09f, 0.05f, 0.9f);
        [SerializeField] private bool _includeInactiveMinigames;
        [SerializeField, Min(0.1f)] private float _minigameMarkerRefreshInterval = 1f;

        private readonly List<MinigameMarker> _minigameMarkers = new();
        private GameObject _hudRoot;
        private GameObject _mapOverlay;
        private RawImage _mapImage;
        private MapDiscoveryView _mapDiscoveryView;
        private RectTransform _markerRoot;
        private RectTransform _playerMarker;
        private Button _openButton;
        private Text _discoveryLabel;
        private Texture2D _markerTexture;
        private Sprite _markerSprite;
        private float _nextMinigameMarkerRefreshTime;
        private float _previousTimeScale = 1f;
        private bool _mapOpen;

        public GameObject HudRoot => _hudRoot;

        public void Configure(MapDiscoverySystem discovery, Transform player = null)
        {
            if (_discovery != null)
            {
                _discovery.MapChanged -= RefreshMapSource;
            }

            _discovery = discovery;
            _player = player != null ? player : discovery != null ? discovery.TrackedTransform : null;

            if (isActiveAndEnabled && _discovery != null)
            {
                _discovery.MapChanged += RefreshMapSource;
            }

            ApplyMapAspectRatio();
            ConfigureDiscoveryView();
            RefreshMapSource();
        }

        public void Initialize()
        {
            ResolveSources();
            BuildHud();
            ApplyMapAspectRatio();
            ConfigureDiscoveryView();
            RefreshMapSource();
            RefreshMinigameMarkers(true);
            UpdateMarkers();
            UpdateLabels();
        }

        public void OpenMap()
        {
            if (_mapOpen)
            {
                return;
            }

            BuildHud();
            _mapOpen = true;

            if (_pauseGameWhenOpen)
            {
                _previousTimeScale = Time.timeScale;
                Time.timeScale = 0f;
            }

            if (_mapOverlay != null)
            {
                _mapOverlay.SetActive(true);
            }

            ConfigureDiscoveryView();
            RefreshMapSource();
            RefreshMinigameMarkers(true);
            UpdateMarkers();
            UpdateLabels();
        }

        public void CloseMap()
        {
            if (!_mapOpen)
            {
                return;
            }

            _mapOpen = false;

            if (_pauseGameWhenOpen)
            {
                Time.timeScale = _previousTimeScale;
            }

            if (_mapOverlay != null)
            {
                _mapOverlay.SetActive(false);
            }
        }

        private void OnEnable()
        {
            if (_discovery != null)
            {
                _discovery.MapChanged += RefreshMapSource;
            }
        }

        private void Start()
        {
            Initialize();
            CloseMap();
        }

        private void Update()
        {
            if (!_mapOpen)
            {
                return;
            }

            RefreshMinigameMarkers(false);
            UpdateMarkers();
            UpdateLabels();
        }

        private void OnDisable()
        {
            if (_discovery != null)
            {
                _discovery.MapChanged -= RefreshMapSource;
            }

            if (_mapOpen)
            {
                CloseMap();
            }
        }

        private void OnDestroy()
        {
            ClearMinigameMarkers();

            if (_hudRoot != null)
            {
                DestroyRuntimeObject(_hudRoot);
            }

            if (_markerSprite != null)
            {
                DestroyRuntimeObject(_markerSprite);
            }

            if (_markerTexture != null)
            {
                DestroyRuntimeObject(_markerTexture);
            }
        }

        private void ResolveSources()
        {
            if (_discovery == null)
            {
                _discovery = GetComponent<MapDiscoverySystem>();
            }

            if (_player == null && _discovery != null)
            {
                _player = _discovery.TrackedTransform;
            }

            if (_discovery != null)
            {
                _discovery.MapChanged -= RefreshMapSource;
                _discovery.MapChanged += RefreshMapSource;
            }

            ApplyMapAspectRatio();
        }

        private void BuildHud()
        {
            if (_hudRoot != null)
            {
                return;
            }

            _hudRoot = new GameObject("Map HUD", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));

            Canvas canvas = _hudRoot.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = _sortingOrder;

            CanvasScaler scaler = _hudRoot.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            CreateOverlay(_hudRoot.transform);
            CreateOpenButton(_hudRoot.transform);
        }

        private void CreateOverlay(Transform parent)
        {
            _mapOverlay = new GameObject("Map Overlay", typeof(RectTransform), typeof(Image));
            _mapOverlay.transform.SetParent(parent, false);

            RectTransform overlayRect = _mapOverlay.GetComponent<RectTransform>();
            overlayRect.anchorMin = Vector2.zero;
            overlayRect.anchorMax = Vector2.one;
            overlayRect.pivot = new Vector2(0.5f, 0.5f);
            overlayRect.anchoredPosition = Vector2.zero;
            overlayRect.sizeDelta = Vector2.zero;

            Image overlayImage = _mapOverlay.GetComponent<Image>();
            overlayImage.color = _panelBackgroundColor;
            overlayImage.raycastTarget = true;

            RectTransform mapFrame = CreateMapFrame(_mapOverlay.transform);
            _mapImage = CreateMapImage(mapFrame);
            _mapDiscoveryView = _mapImage.gameObject.AddComponent<MapDiscoveryView>();
            _markerRoot = CreateMarkerRoot(_mapImage.rectTransform);
            _playerMarker = CreateMarker(_markerRoot, "Player Position", _playerMarkerDiameter, _playerMarkerColor);
            _discoveryLabel = CreateLabel(_mapOverlay.transform);
            CreateCloseButton(_mapOverlay.transform);

            _mapOverlay.SetActive(false);
        }

        private RectTransform CreateMapFrame(Transform parent)
        {
            GameObject frameObject = new GameObject("Map Frame", typeof(RectTransform));
            frameObject.transform.SetParent(parent, false);

            RectTransform frameRect = frameObject.GetComponent<RectTransform>();
            frameRect.anchorMin = new Vector2(0.5f, 0.5f);
            frameRect.anchorMax = new Vector2(0.5f, 0.5f);
            frameRect.pivot = new Vector2(0.5f, 0.5f);
            frameRect.anchoredPosition = Vector2.zero;
            frameRect.sizeDelta = _mapPanelSize;
            return frameRect;
        }

        private RawImage CreateMapImage(RectTransform parent)
        {
            GameObject mapObject = new GameObject("Discovered Map", typeof(RectTransform), typeof(RawImage));
            mapObject.transform.SetParent(parent, false);

            RectTransform mapRect = mapObject.GetComponent<RectTransform>();
            mapRect.anchorMin = Vector2.zero;
            mapRect.anchorMax = Vector2.one;
            mapRect.pivot = new Vector2(0.5f, 0.5f);
            mapRect.anchoredPosition = Vector2.zero;
            mapRect.sizeDelta = Vector2.zero;

            RawImage mapImage = mapObject.GetComponent<RawImage>();
            mapImage.color = Color.white;
            mapImage.raycastTarget = false;
            return mapImage;
        }

        private void CreateOpenButton(Transform parent)
        {
            GameObject buttonObject = new GameObject("Open Map Button", typeof(RectTransform), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(parent, false);

            RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
            buttonRect.anchorMin = Vector2.zero;
            buttonRect.anchorMax = Vector2.zero;
            buttonRect.pivot = Vector2.zero;
            buttonRect.anchoredPosition = new Vector2(_screenMargin, _screenMargin);
            buttonRect.sizeDelta = _openButtonSize;

            Image buttonImage = buttonObject.GetComponent<Image>();
            buttonImage.sprite = _openButtonSprite;
            buttonImage.preserveAspect = true;
            buttonImage.color = _openButtonSprite != null ? Color.white : _buttonFallbackColor;

            _openButton = buttonObject.GetComponent<Button>();
            _openButton.onClick.AddListener(ToggleMap);
        }

        private void CreateCloseButton(Transform parent)
        {
            Button closeButton = CreateTextButton(
                "Close Map Button",
                parent,
                "X",
                new Vector2(1f, 1f),
                new Vector2(1f, 1f),
                new Vector2(-_screenMargin, -_screenMargin),
                _closeButtonSize);
            closeButton.onClick.AddListener(CloseMap);
        }

        private Button CreateTextButton(
            string buttonName,
            Transform parent,
            string label,
            Vector2 anchor,
            Vector2 pivot,
            Vector2 anchoredPosition,
            Vector2 size)
        {
            GameObject buttonObject = new GameObject(buttonName, typeof(RectTransform), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(parent, false);

            RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
            buttonRect.anchorMin = anchor;
            buttonRect.anchorMax = anchor;
            buttonRect.pivot = pivot;
            buttonRect.anchoredPosition = anchoredPosition;
            buttonRect.sizeDelta = size;

            Image background = buttonObject.GetComponent<Image>();
            background.color = _buttonFallbackColor;

            GameObject textObject = new GameObject("Label", typeof(RectTransform), typeof(Text));
            textObject.transform.SetParent(buttonObject.transform, false);

            RectTransform textRect = textObject.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.pivot = new Vector2(0.5f, 0.5f);
            textRect.anchoredPosition = Vector2.zero;
            textRect.sizeDelta = Vector2.zero;

            Text text = textObject.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 30;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = _labelColor;
            text.raycastTarget = false;
            text.text = label;

            return buttonObject.GetComponent<Button>();
        }

        private Text CreateLabel(Transform parent)
        {
            GameObject labelObject = new GameObject("Discovery Label", typeof(RectTransform), typeof(Text));
            labelObject.transform.SetParent(parent, false);

            RectTransform labelRect = labelObject.GetComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0.5f, 0f);
            labelRect.anchorMax = new Vector2(0.5f, 0f);
            labelRect.pivot = new Vector2(0.5f, 0f);
            labelRect.anchoredPosition = new Vector2(0f, _screenMargin);
            labelRect.sizeDelta = new Vector2(520f, 34f);

            Text label = labelObject.GetComponent<Text>();
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = 18;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = _labelColor;
            label.raycastTarget = false;
            return label;
        }

        private RectTransform CreateMarkerRoot(RectTransform mapParent)
        {
            GameObject rootObject = new GameObject("Map Markers", typeof(RectTransform));
            rootObject.transform.SetParent(mapParent, false);

            RectTransform rootRect = rootObject.GetComponent<RectTransform>();
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.pivot = new Vector2(0.5f, 0.5f);
            rootRect.anchoredPosition = Vector2.zero;
            rootRect.sizeDelta = Vector2.zero;
            return rootRect;
        }

        private RectTransform CreateMarker(RectTransform mapParent, string markerName, float diameter, Color color)
        {
            GameObject markerObject = new GameObject(markerName, typeof(RectTransform), typeof(Image));
            markerObject.transform.SetParent(mapParent, false);

            RectTransform markerRect = markerObject.GetComponent<RectTransform>();
            markerRect.anchorMin = new Vector2(0.5f, 0.5f);
            markerRect.anchorMax = new Vector2(0.5f, 0.5f);
            markerRect.pivot = new Vector2(0.5f, 0.5f);
            markerRect.anchoredPosition = Vector2.zero;
            markerRect.sizeDelta = Vector2.one * diameter;

            Image marker = markerObject.GetComponent<Image>();
            marker.sprite = GetMarkerSprite();
            marker.color = color;
            marker.raycastTarget = false;
            return markerRect;
        }

        private Sprite GetMarkerSprite()
        {
            if (_markerSprite != null)
            {
                return _markerSprite;
            }

            const int textureSize = 32;
            _markerTexture = new Texture2D(textureSize, textureSize, TextureFormat.RGBA32, false, true)
            {
                name = "MapHud_Marker_Runtime",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };

            Color32[] pixels = new Color32[textureSize * textureSize];
            Vector2 center = Vector2.one * ((textureSize - 1) * 0.5f);
            float radiusSquared = center.x * center.x;

            for (int y = 0; y < textureSize; y++)
            {
                for (int x = 0; x < textureSize; x++)
                {
                    Vector2 offset = new Vector2(x, y) - center;
                    pixels[y * textureSize + x] = offset.sqrMagnitude <= radiusSquared
                        ? Color.white
                        : Color.clear;
                }
            }

            _markerTexture.SetPixels32(pixels);
            _markerTexture.Apply(false, true);
            _markerSprite = Sprite.Create(
                _markerTexture,
                new Rect(0f, 0f, textureSize, textureSize),
                new Vector2(0.5f, 0.5f),
                textureSize);
            _markerSprite.name = "MapHud_Marker_Runtime";
            return _markerSprite;
        }

        private void ToggleMap()
        {
            if (_mapOpen)
            {
                CloseMap();
            }
            else
            {
                OpenMap();
            }
        }

        private void ConfigureDiscoveryView()
        {
            if (_mapDiscoveryView == null)
            {
                return;
            }

            _mapDiscoveryView.Configure(_discovery, _mapImage, null, GetMapTexture());
        }

        private void RefreshMapSource()
        {
            ConfigureDiscoveryView();
        }

        private void RefreshMinigameMarkers(bool force)
        {
            if (_markerRoot == null)
            {
                return;
            }

            if (!force && Time.unscaledTime < _nextMinigameMarkerRefreshTime)
            {
                return;
            }

            _nextMinigameMarkerRefreshTime = Time.unscaledTime + _minigameMarkerRefreshInterval;
            ClearMinigameMarkers();

            FindObjectsInactive inactiveMode = _includeInactiveMinigames
                ? FindObjectsInactive.Include
                : FindObjectsInactive.Exclude;
            MonoBehaviour[] behaviours = FindObjectsByType<MonoBehaviour>(inactiveMode, FindObjectsSortMode.None);

            for (int i = 0; i < behaviours.Length; i++)
            {
                MonoBehaviour behaviour = behaviours[i];
                if (behaviour is MapAttentionMarker attentionMarker)
                {
                    CreateAttentionMarker(attentionMarker);
                    continue;
                }

                if (!IsMinigameInteractable(behaviour))
                {
                    continue;
                }

                GameObject sourceObject = behaviour.gameObject;
                if (!sourceObject.scene.IsValid())
                {
                    continue;
                }

                RectTransform marker = CreateMarker(
                    _markerRoot,
                    $"Minigame Position ({sourceObject.name})",
                    _minigameMarkerDiameter,
                    _minigameMarkerColor);
                _minigameMarkers.Add(new MinigameMarker(behaviour, marker));
            }
        }

        private void CreateAttentionMarker(MapAttentionMarker attentionMarker)
        {
            if (attentionMarker == null || !attentionMarker.VisibleOnMap)
            {
                return;
            }

            GameObject sourceObject = attentionMarker.gameObject;
            if (!sourceObject.scene.IsValid())
            {
                return;
            }

            RectTransform marker = CreateMarker(
                _markerRoot,
                $"Attention Position ({sourceObject.name})",
                attentionMarker.Diameter,
                attentionMarker.Color);
            _minigameMarkers.Add(new MinigameMarker(attentionMarker, marker));
        }

        private void UpdateMarkers()
        {
            UpdatePlayerMarker();
            UpdateMinigameMarkers();
        }

        private void UpdatePlayerMarker()
        {
            if (_playerMarker == null || _discovery == null)
            {
                return;
            }

            if (_player == null)
            {
                _player = _discovery.TrackedTransform;
            }

            if (_player == null)
            {
                _playerMarker.gameObject.SetActive(false);
                return;
            }

            bool isInsideMap = _discovery.TryWorldToUv(_player.position, out Vector2 uv);
            _playerMarker.gameObject.SetActive(isInsideMap);

            if (!isInsideMap)
            {
                return;
            }

            _playerMarker.anchorMin = uv;
            _playerMarker.anchorMax = uv;
            _playerMarker.anchoredPosition = Vector2.zero;
        }

        private void UpdateMinigameMarkers()
        {
            if (_discovery == null || _minigameMarkers.Count == 0)
            {
                return;
            }

            for (int i = 0; i < _minigameMarkers.Count; i++)
            {
                MinigameMarker marker = _minigameMarkers[i];
                if (!marker.IsValid || (!_includeInactiveMinigames && !marker.Source.gameObject.activeInHierarchy))
                {
                    marker.SetVisible(false);
                    continue;
                }

                bool isInsideMap = _discovery.TryWorldToUv(marker.Transform.position, out Vector2 uv);
                marker.SetVisible(isInsideMap);
                if (!isInsideMap)
                {
                    continue;
                }

                marker.Marker.anchorMin = uv;
                marker.Marker.anchorMax = uv;
                marker.Marker.anchoredPosition = Vector2.zero;
            }
        }

        private void UpdateLabels()
        {
            if (_discoveryLabel != null && _discovery != null)
            {
                _discoveryLabel.text = $"DESCUBIERTO {_discovery.DiscoveredFraction:P1}";
            }
        }

        private void ClearMinigameMarkers()
        {
            for (int i = 0; i < _minigameMarkers.Count; i++)
            {
                RectTransform marker = _minigameMarkers[i].Marker;
                if (marker != null)
                {
                    DestroyRuntimeObject(marker.gameObject);
                }
            }

            _minigameMarkers.Clear();
        }

        private Texture GetMapTexture()
        {
            if (_mapTextureOverride != null)
            {
                return _mapTextureOverride;
            }

            return _discovery != null && _discovery.Definition != null
                ? _discovery.Definition.TraversableMask
                : null;
        }

        private void ApplyMapAspectRatio()
        {
            if (!_preserveMapAspectRatio)
            {
                return;
            }

            Texture map = GetMapTexture();
            if (map == null || map.width <= 0)
            {
                return;
            }

            _mapPanelSize.y = _mapPanelSize.x * map.height / map.width;
        }

        private void OnValidate()
        {
            _mapPanelSize.x = Mathf.Max(64f, _mapPanelSize.x);
            _mapPanelSize.y = Mathf.Max(64f, _mapPanelSize.y);
            _openButtonSize.x = Mathf.Max(48f, _openButtonSize.x);
            _openButtonSize.y = Mathf.Max(48f, _openButtonSize.y);
            _closeButtonSize.x = Mathf.Max(40f, _closeButtonSize.x);
            _closeButtonSize.y = Mathf.Max(40f, _closeButtonSize.y);
            _playerMarkerDiameter = Mathf.Max(1f, _playerMarkerDiameter);
            _minigameMarkerDiameter = Mathf.Max(1f, _minigameMarkerDiameter);
            _minigameMarkerRefreshInterval = Mathf.Max(0.1f, _minigameMarkerRefreshInterval);
        }

        private static bool IsMinigameInteractable(MonoBehaviour behaviour)
        {
            return behaviour != null && behaviour.GetType().FullName == MinigameInteractableTypeName;
        }

        private static void DestroyRuntimeObject(Object target)
        {
            if (Application.isPlaying)
            {
                Destroy(target);
            }
            else
            {
                DestroyImmediate(target);
            }
        }

        private sealed class MinigameMarker
        {
            public MinigameMarker(MonoBehaviour source, RectTransform marker)
            {
                Source = source;
                Transform = source != null ? source.transform : null;
                Marker = marker;
            }

            public MonoBehaviour Source { get; }
            public Transform Transform { get; }
            public RectTransform Marker { get; }
            public bool IsValid => Source != null && Transform != null && Marker != null;

            public void SetVisible(bool visible)
            {
                if (Marker != null && Marker.gameObject.activeSelf != visible)
                {
                    Marker.gameObject.SetActive(visible);
                }
            }
        }
    }
}
