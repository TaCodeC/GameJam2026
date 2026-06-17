using UnityEngine;
using UnityEngine.UI;

namespace GameJam.Gameplay.Map
{
    [DisallowMultipleComponent]
    public sealed class MapDebugHud : MonoBehaviour
    {
        private const float PanelPadding = 8f;
        private const float LabelHeight = 28f;

        [Header("Sources")]
        [SerializeField] private MapDiscoverySystem _discovery;
        [Tooltip("Defaults to the transform tracked by MapDiscoverySystem.")]
        [SerializeField] private Transform _player;

        [Header("Layout")]
        [SerializeField] private Vector2 _mapPanelSize = new Vector2(360f, 203f);
        [SerializeField] private bool _preserveMapAspectRatio = true;
        [SerializeField, Min(0f)] private float _screenMargin = 20f;
        [SerializeField] private int _sortingOrder = 1000;

        [Header("Appearance")]
        [SerializeField, Min(1f)] private float _playerMarkerDiameter = 12f;
        [SerializeField] private Color _playerMarkerColor = Color.red;
        [SerializeField] private Color _panelBackgroundColor = new Color(0.04f, 0.04f, 0.04f, 0.9f);
        [SerializeField] private Color _labelColor = Color.white;

        private GameObject _hudRoot;
        private RawImage _realMapImage;
        private RawImage _discoveredMapImage;
        private RectTransform _playerMarker;
        private Text _realPositionLabel;
        private Text _discoveryLabel;
        private Texture2D _markerTexture;
        private Sprite _markerSprite;

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
            RefreshMapSource();
        }

        public void Initialize()
        {
            ResolveSources();
            BuildHud();
            RefreshMapSource();
            UpdatePreviewRotation();
            UpdatePlayerMarker();
            UpdateLabels();
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
        }

        private void Update()
        {
            UpdatePreviewRotation();
            UpdatePlayerMarker();
            UpdateLabels();
        }

        private void OnDisable()
        {
            if (_discovery != null)
            {
                _discovery.MapChanged -= RefreshMapSource;
            }
        }

        private void OnDestroy()
        {
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

            // Si, todo este HUD se arma por codigo. No pregunten.
            _hudRoot = new GameObject("Map Debug HUD", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));

            Canvas canvas = _hudRoot.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = _sortingOrder;

            CanvasScaler scaler = _hudRoot.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            RawImage realMap = CreatePanel(
                "Real Position Map",
                new Vector2(0f, 1f),
                new Vector2(0f, 1f),
                new Vector2(_screenMargin, -_screenMargin),
                out _realPositionLabel);
            _realMapImage = realMap;
            _playerMarker = CreateMarker(realMap.rectTransform);

            RawImage discoveredMap = CreatePanel(
                "Discovered Map",
                new Vector2(1f, 1f),
                new Vector2(1f, 1f),
                new Vector2(-_screenMargin, -_screenMargin),
                out _discoveryLabel);
            _discoveredMapImage = discoveredMap;

            MapDiscoveryView discoveryView = discoveredMap.gameObject.AddComponent<MapDiscoveryView>();
            discoveryView.Configure(_discovery, discoveredMap);
        }

        private RawImage CreatePanel(
            string panelName,
            Vector2 anchor,
            Vector2 pivot,
            Vector2 anchoredPosition,
            out Text label)
        {
            Vector2 panelSize = new Vector2(
                _mapPanelSize.x + PanelPadding * 2f,
                _mapPanelSize.y + PanelPadding * 2f + LabelHeight);

            GameObject panelObject = new GameObject(panelName, typeof(RectTransform), typeof(Image));
            panelObject.transform.SetParent(_hudRoot.transform, false);

            RectTransform panelRect = panelObject.GetComponent<RectTransform>();
            panelRect.anchorMin = anchor;
            panelRect.anchorMax = anchor;
            panelRect.pivot = pivot;
            panelRect.anchoredPosition = anchoredPosition;
            panelRect.sizeDelta = panelSize;

            Image background = panelObject.GetComponent<Image>();
            background.color = _panelBackgroundColor;
            background.raycastTarget = false;

            GameObject mapObject = new GameObject("Map", typeof(RectTransform), typeof(RawImage));
            mapObject.transform.SetParent(panelObject.transform, false);

            RectTransform mapRect = mapObject.GetComponent<RectTransform>();
            mapRect.anchorMin = new Vector2(0.5f, 1f);
            mapRect.anchorMax = new Vector2(0.5f, 1f);
            mapRect.pivot = new Vector2(0.5f, 0.5f);
            mapRect.anchoredPosition = new Vector2(0f, -PanelPadding - _mapPanelSize.y * 0.5f);
            mapRect.sizeDelta = _mapPanelSize;

            RawImage mapImage = mapObject.GetComponent<RawImage>();
            mapImage.color = Color.white;
            mapImage.raycastTarget = false;

            label = CreateLabel(panelObject.transform);
            return mapImage;
        }

        private Text CreateLabel(Transform parent)
        {
            GameObject labelObject = new GameObject("Debug Label", typeof(RectTransform), typeof(Text));
            labelObject.transform.SetParent(parent, false);

            RectTransform labelRect = labelObject.GetComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0f, 0f);
            labelRect.anchorMax = new Vector2(1f, 0f);
            labelRect.pivot = new Vector2(0.5f, 0f);
            labelRect.anchoredPosition = new Vector2(0f, PanelPadding * 0.5f);
            labelRect.sizeDelta = new Vector2(-PanelPadding * 2f, LabelHeight);

            Text label = labelObject.GetComponent<Text>();
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = 16;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = _labelColor;
            label.raycastTarget = false;
            return label;
        }

        private RectTransform CreateMarker(RectTransform mapParent)
        {
            GameObject markerObject = new GameObject("Player Position", typeof(RectTransform), typeof(Image));
            markerObject.transform.SetParent(mapParent, false);

            RectTransform markerRect = markerObject.GetComponent<RectTransform>();
            markerRect.anchorMin = new Vector2(0.5f, 0.5f);
            markerRect.anchorMax = new Vector2(0.5f, 0.5f);
            markerRect.pivot = new Vector2(0.5f, 0.5f);
            markerRect.anchoredPosition = Vector2.zero;
            markerRect.sizeDelta = Vector2.one * _playerMarkerDiameter;

            Image marker = markerObject.GetComponent<Image>();
            marker.sprite = CreateMarkerSprite();
            marker.color = _playerMarkerColor;
            marker.raycastTarget = false;
            return markerRect;
        }

        private Sprite CreateMarkerSprite()
        {
            const int textureSize = 32;
            _markerTexture = new Texture2D(textureSize, textureSize, TextureFormat.RGBA32, false, true)
            {
                name = "MapDebugHud_PlayerMarker_Runtime",
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
            _markerSprite.name = "MapDebugHud_PlayerMarker_Runtime";
            return _markerSprite;
        }

        private void RefreshMapSource()
        {
            if (_realMapImage == null || _discovery == null || !_discovery.IsInitialized)
            {
                return;
            }

            _realMapImage.texture = _discovery.Definition.TraversableMask;
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

        private void UpdatePreviewRotation()
        {
            if (_discovery == null)
            {
                return;
            }

            if (_realMapImage != null)
            {
                _realMapImage.rectTransform.localRotation = Quaternion.identity;
            }

            if (_discoveredMapImage != null)
            {
                _discoveredMapImage.rectTransform.localRotation = Quaternion.identity;
            }
        }

        private void UpdateLabels()
        {
            if (_realPositionLabel != null)
            {
                _realPositionLabel.text = BuildPositionLabel();
            }

            if (_discoveryLabel != null && _discovery != null)
            {
                _discoveryLabel.text =
                    $"DESCUBIERTO {_discovery.DiscoveredFraction:P1}  |  RECORRIDO {_discovery.VisitedFraction:P1}";
            }
        }

        private string BuildPositionLabel()
        {
            if (_player == null || _discovery == null || _discovery.Definition == null)
            {
                return "POSICION REAL: sin jugador";
            }

            Vector3 position = _player.position;
            return _discovery.Definition.WorldPlane == MapWorldPlane.XY
                ? $"POSICION REAL  X {position.x:0.00}  Y {position.y:0.00}"
                : $"POSICION REAL  X {position.x:0.00}  Z {position.z:0.00}";
        }

        private void ApplyMapAspectRatio()
        {
            if (!_preserveMapAspectRatio ||
                _discovery == null ||
                _discovery.Definition == null ||
                _discovery.Definition.TraversableMask == null)
            {
                return;
            }

            Texture2D map = _discovery.Definition.TraversableMask;
            _mapPanelSize.y = _mapPanelSize.x * map.height / map.width;
        }

        private void OnValidate()
        {
            _mapPanelSize.x = Mathf.Max(64f, _mapPanelSize.x);
            _mapPanelSize.y = Mathf.Max(64f, _mapPanelSize.y);
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
    }
}
