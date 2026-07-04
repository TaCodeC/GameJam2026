using System;
using UnityEngine;

namespace GameJam.Gameplay.Map
{
    [DisallowMultipleComponent]
    public sealed class MapDiscoverySystem : MonoBehaviour
    {
        [Header("Map")]
        [SerializeField] private MapDefinition _definition;

        [Header("Tracking")]
        [SerializeField] private Transform _trackedTransform;
        [SerializeField] private bool _autoFindTrackedTransform = true;
        [SerializeField] private string _trackedTransformObjectName = "Player";
        [SerializeField, Min(0.01f)] private float _revealRadius = 1.25f;
        [SerializeField, Min(0.01f)] private float _visitedRadius = 0.15f;
        [Tooltip("Maximum distance between paint stamps. Smaller values produce smoother paths.")]
        [SerializeField, Min(0.01f)] private float _stampSpacing = 0.25f;

        private Color32[] _maskPixels;
        private byte[] _discoveredPixels;
        private byte[] _visitedPixels;
        private Color32[] _discoveryUploadPixels;
        private Texture2D _discoveryTexture;
        private Vector3 _lastTrackedPosition;
        private int _maskWidth;
        private int _maskHeight;
        private int _walkableCellCount;
        private int _discoveredCellCount;
        private int _visitedCellCount;
        private bool _hasLastTrackedPosition;
        private bool _initialized;
        private bool _warnedMissingTrackedTransform;
        private bool _warnedTrackedTransformOutsideMap;

        public event Action MapChanged;
        public event Action DiscoveryChanged;

        public MapDefinition Definition => _definition;
        public Texture2D DiscoveryTexture => _discoveryTexture;
        public Transform TrackedTransform => _trackedTransform;
        public bool IsInitialized => _initialized;
        public float DiscoveredFraction => SafeFraction(_discoveredCellCount, _walkableCellCount);
        public float VisitedFraction => SafeFraction(_visitedCellCount, _walkableCellCount);

        private void Awake()
        {
            Initialize();
            ResolveTrackedTransformIfNeeded();
        }

        private void LateUpdate()
        {
            ResolveTrackedTransformIfNeeded();

            if (_trackedTransform != null)
            {
                RecordPosition(_trackedTransform.position);
            }
            else
            {
                WarnMissingTrackedTransform();
            }
        }

        private void OnDestroy()
        {
            DestroyRuntimeTexture();
        }

        public void SetDefinition(MapDefinition definition)
        {
            if (_definition == definition && _initialized)
            {
                return;
            }

            _definition = definition;
            Initialize();
        }

        public void SetTrackedTransform(Transform trackedTransform)
        {
            _trackedTransform = trackedTransform;
            _hasLastTrackedPosition = false;
            _warnedMissingTrackedTransform = false;
            _warnedTrackedTransformOutsideMap = false;
        }

        [ContextMenu("Initialize")]
        public void Initialize()
        {
            DestroyRuntimeTexture();
            _initialized = false;
            _hasLastTrackedPosition = false;

            if (_definition == null || _definition.TraversableMask == null)
            {
                Debug.LogWarning($"{nameof(MapDiscoverySystem)} on {name} needs a map definition with a mask.", this);
                return;
            }

            CacheMaskPixels(_definition.TraversableMask);

            Vector2Int resolution = _definition.DiscoveryResolution;
            _discoveredPixels = new byte[resolution.x * resolution.y];
            _visitedPixels = new byte[resolution.x * resolution.y];
            _discoveryUploadPixels = new Color32[resolution.x * resolution.y];
            _discoveryTexture = new Texture2D(resolution.x, resolution.y, TextureFormat.RGBA32, false, true)
            {
                name = $"{_definition.name}_Discovery_Runtime",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };

            CountWalkableCells();
            UploadDiscoveryTexture();
            _initialized = true;
            MapChanged?.Invoke();
        }

        [ContextMenu("Reset Discovery")]
        public void ResetDiscovery()
        {
            if (!_initialized)
            {
                return;
            }

            Array.Clear(_discoveredPixels, 0, _discoveredPixels.Length);
            Array.Clear(_visitedPixels, 0, _visitedPixels.Length);
            _discoveredCellCount = 0;
            _visitedCellCount = 0;
            _hasLastTrackedPosition = false;
            UploadDiscoveryTexture();
            DiscoveryChanged?.Invoke();
        }

        public void RecordPosition(Vector3 worldPosition)
        {
            if (!_initialized)
            {
                return;
            }

            if (!TryWorldToUv(worldPosition, out _))
            {
                WarnTrackedTransformOutsideMap(worldPosition);
                _lastTrackedPosition = worldPosition;
                _hasLastTrackedPosition = false;
                return;
            }

            _warnedTrackedTransformOutsideMap = false;

            bool changed;
            if (_hasLastTrackedPosition)
            {
                changed = PaintSegment(_lastTrackedPosition, worldPosition);
            }
            else
            {
                changed = PaintAt(worldPosition);
            }

            _lastTrackedPosition = worldPosition;
            _hasLastTrackedPosition = true;

            if (changed)
            {
                UploadDiscoveryTexture();
                DiscoveryChanged?.Invoke();
            }
        }

        public bool IsWalkable(Vector3 worldPosition)
        {
            return TryWorldToUv(worldPosition, out Vector2 uv) && IsWalkableUv(uv);
        }

        public bool IsDiscovered(Vector3 worldPosition)
        {
            return TryGetDiscoveryIndex(worldPosition, out int index) && _discoveredPixels[index] > 0;
        }

        public bool HasBeenVisited(Vector3 worldPosition)
        {
            return TryGetDiscoveryIndex(worldPosition, out int index) && _visitedPixels[index] > 0;
        }

        public bool CanTraverseSegment(Vector3 from, Vector3 to, float sampleSpacing = 0.1f)
        {
            if (!_initialized)
            {
                return false;
            }

            float distance = PlanarDistance(from, to);
            int steps = Mathf.Max(1, Mathf.CeilToInt(distance / Mathf.Max(0.01f, sampleSpacing)));
            for (int i = 0; i <= steps; i++)
            {
                if (!IsWalkable(Vector3.Lerp(from, to, i / (float)steps)))
                {
                    return false;
                }
            }

            return true;
        }

        public bool TryWorldToUv(Vector3 worldPosition, out Vector2 uv)
        {
            uv = default;
            if (!_initialized || _definition == null)
            {
                return false;
            }

            Vector3 local = Quaternion.Inverse(transform.rotation) * (worldPosition - transform.position);
            Vector2 point = _definition.WorldPlane == MapWorldPlane.XY
                ? new Vector2(local.x, local.y)
                : new Vector2(local.x, local.z);

            Vector2 size = _definition.WorldSize;
            uv = new Vector2(point.x / size.x + 0.5f, point.y / size.y + 0.5f);

            if (_definition.FlipWorldX)
            {
                uv.x = 1f - uv.x;
            }

            if (_definition.FlipWorldY)
            {
                uv.y = 1f - uv.y;
            }

            return uv.x >= 0f && uv.x <= 1f && uv.y >= 0f && uv.y <= 1f;
        }

        private bool PaintSegment(Vector3 from, Vector3 to)
        {
            float distance = PlanarDistance(from, to);
            int steps = Mathf.Max(1, Mathf.CeilToInt(distance / _stampSpacing));
            bool changed = false;

            for (int i = 1; i <= steps; i++)
            {
                changed |= PaintAt(Vector3.Lerp(from, to, i / (float)steps));
            }

            return changed;
        }

        private bool PaintAt(Vector3 worldPosition)
        {
            if (!TryWorldToUv(worldPosition, out Vector2 uv) || !IsWalkableUv(uv))
            {
                return false;
            }

            bool changed = PaintCircle(_discoveredPixels, uv, _revealRadius, ref _discoveredCellCount);
            changed |= PaintCircle(_visitedPixels, uv, _visitedRadius, ref _visitedCellCount);
            return changed;
        }

        private bool PaintCircle(byte[] pixels, Vector2 centerUv, float worldRadius, ref int paintedCellCount)
        {
            Vector2Int resolution = _definition.DiscoveryResolution;
            Vector2 worldSize = _definition.WorldSize;
            float radiusX = worldRadius / worldSize.x * resolution.x;
            float radiusY = worldRadius / worldSize.y * resolution.y;
            float centerX = centerUv.x * (resolution.x - 1);
            float centerY = centerUv.y * (resolution.y - 1);

            int minX = Mathf.Max(0, Mathf.FloorToInt(centerX - radiusX));
            int maxX = Mathf.Min(resolution.x - 1, Mathf.CeilToInt(centerX + radiusX));
            int minY = Mathf.Max(0, Mathf.FloorToInt(centerY - radiusY));
            int maxY = Mathf.Min(resolution.y - 1, Mathf.CeilToInt(centerY + radiusY));
            bool changed = false;

            // Pixel por pixel, porque claramente sobraba tiempo.
            for (int y = minY; y <= maxY; y++)
            {
                float dy = (y - centerY) / radiusY;
                for (int x = minX; x <= maxX; x++)
                {
                    float dx = (x - centerX) / radiusX;
                    if (dx * dx + dy * dy > 1f)
                    {
                        continue;
                    }

                    Vector2 uv = new Vector2(
                        (x + 0.5f) / resolution.x,
                        (y + 0.5f) / resolution.y);

                    int index = y * resolution.x + x;
                    if (pixels[index] != 0 || !IsWalkableUv(uv))
                    {
                        continue;
                    }

                    pixels[index] = byte.MaxValue;
                    paintedCellCount++;
                    changed = true;
                }
            }

            return changed;
        }

        private bool TryGetDiscoveryIndex(Vector3 worldPosition, out int index)
        {
            index = -1;
            if (!TryWorldToUv(worldPosition, out Vector2 uv))
            {
                return false;
            }

            Vector2Int resolution = _definition.DiscoveryResolution;
            int x = Mathf.Clamp(Mathf.FloorToInt(uv.x * resolution.x), 0, resolution.x - 1);
            int y = Mathf.Clamp(Mathf.FloorToInt(uv.y * resolution.y), 0, resolution.y - 1);
            index = y * resolution.x + x;
            return true;
        }

        private bool IsWalkableUv(Vector2 uv)
        {
            if (_maskPixels == null || uv.x < 0f || uv.x > 1f || uv.y < 0f || uv.y > 1f)
            {
                return false;
            }

            int x = Mathf.Clamp(Mathf.FloorToInt(uv.x * _maskWidth), 0, _maskWidth - 1);
            int y = Mathf.Clamp(Mathf.FloorToInt(uv.y * _maskHeight), 0, _maskHeight - 1);
            Color32 pixel = _maskPixels[y * _maskWidth + x];

            float value = _definition.MaskChannel switch
            {
                MapMaskChannel.Red => pixel.r / 255f,
                MapMaskChannel.Alpha => pixel.a / 255f,
                _ => (pixel.r * 0.2126f + pixel.g * 0.7152f + pixel.b * 0.0722f) / 255f
            };

            return value >= _definition.WalkableThreshold;
        }

        private void CountWalkableCells()
        {
            _walkableCellCount = 0;
            Vector2Int resolution = _definition.DiscoveryResolution;

            for (int y = 0; y < resolution.y; y++)
            {
                for (int x = 0; x < resolution.x; x++)
                {
                    Vector2 uv = new Vector2(
                        (x + 0.5f) / resolution.x,
                        (y + 0.5f) / resolution.y);

                    if (IsWalkableUv(uv))
                    {
                        _walkableCellCount++;
                    }
                }
            }
        }

        private void CacheMaskPixels(Texture2D source)
        {
            _maskWidth = source.width;
            _maskHeight = source.height;

            if (source.isReadable)
            {
                _maskPixels = source.GetPixels32();
                return;
            }

            // Si Unity no deja leer la textura, le sacamos una fotocopia y fingimos que todo bien.
            RenderTexture temporary = RenderTexture.GetTemporary(
                _maskWidth,
                _maskHeight,
                0,
                RenderTextureFormat.ARGB32,
                RenderTextureReadWrite.Linear);

            RenderTexture previous = RenderTexture.active;
            Graphics.Blit(source, temporary);
            RenderTexture.active = temporary;

            Texture2D readableCopy = new Texture2D(_maskWidth, _maskHeight, TextureFormat.RGBA32, false, true);
            readableCopy.ReadPixels(new Rect(0, 0, _maskWidth, _maskHeight), 0, 0);
            readableCopy.Apply();
            _maskPixels = readableCopy.GetPixels32();

            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(temporary);
            DestroyRuntimeObject(readableCopy);
        }

        private void UploadDiscoveryTexture()
        {
            for (int i = 0; i < _discoveredPixels.Length; i++)
            {
                byte value = _discoveredPixels[i];
                _discoveryUploadPixels[i] = new Color32(value, value, value, byte.MaxValue);
            }

            _discoveryTexture.SetPixels32(_discoveryUploadPixels);
            _discoveryTexture.Apply(false, false);
        }

        private float PlanarDistance(Vector3 a, Vector3 b)
        {
            return _definition.WorldPlane == MapWorldPlane.XY
                ? Vector2.Distance(new Vector2(a.x, a.y), new Vector2(b.x, b.y))
                : Vector2.Distance(new Vector2(a.x, a.z), new Vector2(b.x, b.z));
        }

        private void DestroyRuntimeTexture()
        {
            if (_discoveryTexture != null)
            {
                DestroyRuntimeObject(_discoveryTexture);
                _discoveryTexture = null;
            }
        }

        private static void DestroyRuntimeObject(UnityEngine.Object target)
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

        private static float SafeFraction(int value, int total)
        {
            return total > 0 ? value / (float)total : 0f;
        }

        private void ResolveTrackedTransformIfNeeded()
        {
            if (_trackedTransform != null ||
                !_autoFindTrackedTransform ||
                string.IsNullOrWhiteSpace(_trackedTransformObjectName))
            {
                return;
            }

            GameObject trackedObject = GameObject.Find(_trackedTransformObjectName);
            if (trackedObject != null)
            {
                SetTrackedTransform(trackedObject.transform);
            }
        }

        private void WarnMissingTrackedTransform()
        {
            if (_warnedMissingTrackedTransform)
            {
                return;
            }

            _warnedMissingTrackedTransform = true;
            Debug.LogWarning(
                $"{nameof(MapDiscoverySystem)} on {name} has no tracked transform, so discovery will not advance. " +
                $"Assign the player or leave auto-find enabled with an object named '{_trackedTransformObjectName}'.",
                this);
        }

        private void WarnTrackedTransformOutsideMap(Vector3 worldPosition)
        {
            if (_warnedTrackedTransformOutsideMap)
            {
                return;
            }

            _warnedTrackedTransformOutsideMap = true;
            Debug.LogWarning(
                $"Tracked position {worldPosition} is outside map '{_definition.name}'. " +
                $"Map center is {transform.position} and world size is {_definition.WorldSize}.",
                this);
        }

        private void OnDrawGizmosSelected()
        {
            if (_definition == null)
            {
                return;
            }

            Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.7f);
            Vector2 size = _definition.WorldSize;
            Vector3 gizmoSize = _definition.WorldPlane == MapWorldPlane.XY
                ? new Vector3(size.x, size.y, 0.05f)
                : new Vector3(size.x, 0.05f, size.y);

            Matrix4x4 previousMatrix = Gizmos.matrix;
            Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one);
            Gizmos.DrawWireCube(Vector3.zero, gizmoSize);
            Gizmos.matrix = previousMatrix;
        }
    }
}
