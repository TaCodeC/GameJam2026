using System;
using UnityEngine;

namespace GameJam.Gameplay.Map
{
    [Serializable]
    public struct MapColliderBakeStats
    {
        public int SourceWidth;
        public int SourceHeight;
        public int SampledWidth;
        public int SampledHeight;
        public int BlockedCellCount;
        public int RawEdgeCount;
        public int RawPathPointCount;
        public int SimplifiedPathPointCount;
        public int ColliderCount;

        public bool HasData => SourceWidth > 0 || ColliderCount > 0;

        public static MapColliderBakeStats FromResult(MapCollider2DGenerationResult result)
        {
            return new MapColliderBakeStats
            {
                SourceWidth = result.SourceWidth,
                SourceHeight = result.SourceHeight,
                SampledWidth = result.SampledWidth,
                SampledHeight = result.SampledHeight,
                BlockedCellCount = result.BlockedCellCount,
                RawEdgeCount = result.RawEdgeCount,
                RawPathPointCount = result.RawPathPointCount,
                SimplifiedPathPointCount = result.SimplifiedPathPointCount,
                ColliderCount = result.Paths.Length
            };
        }
    }

    [DisallowMultipleComponent]
    public sealed class MapCollider2DBaker : MonoBehaviour
    {
        private const string GeneratedRootName = "Generated Map Colliders";

        [Header("Source")]
        [SerializeField] private MapDiscoverySystem _discovery;
        [SerializeField] private MapDefinition _definition;

        [Header("Bake")]
        [Tooltip("Use 1 for maximum precision. Higher values sample fewer pixels and produce lighter, less precise colliders.")]
        [SerializeField, Min(1)] private int _sourceSampleStep = 1;
        [Tooltip("World-space Ramer-Douglas-Peucker tolerance. Higher values reduce collider points.")]
        [SerializeField, Min(0f)] private float _simplificationTolerance = 0.2f;
        [SerializeField, Min(3)] private int _minimumPathPointCount = 3;

        [Header("Collider")]
        [SerializeField, Min(0f)] private float _edgeRadius;
        [SerializeField] private bool _isTrigger;
        [SerializeField] private PhysicsMaterial2D _physicsMaterial;

        [SerializeField, HideInInspector] private Transform _generatedRoot;
        [SerializeField, HideInInspector] private MapColliderBakeStats _lastBakeStats;

        public MapDiscoverySystem Discovery => _discovery;
        public MapDefinition Definition => ResolveDefinition();
        public MapColliderBakeStats LastBakeStats => _lastBakeStats;

        public void SetDiscovery(MapDiscoverySystem discovery)
        {
            _discovery = discovery;
            if (_definition == null && _discovery != null)
            {
                _definition = _discovery.Definition;
            }
        }

        public void SetDefinition(MapDefinition definition)
        {
            _definition = definition;
        }

        public MapColliderBakeStats BakeColliders()
        {
            _sourceSampleStep = Mathf.Max(1, _sourceSampleStep);
            _simplificationTolerance = Mathf.Max(0f, _simplificationTolerance);
            _minimumPathPointCount = Mathf.Max(3, _minimumPathPointCount);
            _edgeRadius = Mathf.Max(0f, _edgeRadius);
            TryAssignDefinitionFromDiscoverySystem();

            MapDefinition definition = ResolveDefinition();
            if (definition == null || definition.TraversableMask == null)
            {
                Debug.LogWarning($"{nameof(MapCollider2DBaker)} on {name} needs a map definition with a mask.", this);
                _lastBakeStats = default;
                return _lastBakeStats;
            }

            if (definition.WorldPlane != MapWorldPlane.XY)
            {
                Debug.LogError($"{nameof(MapCollider2DBaker)} only supports {nameof(MapWorldPlane.XY)} maps because Collider2D lives on the XY plane.", this);
                _lastBakeStats = default;
                return _lastBakeStats;
            }

            MapCollider2DGenerationSettings settings = new MapCollider2DGenerationSettings(
                _sourceSampleStep,
                _simplificationTolerance,
                _minimumPathPointCount);

            MapCollider2DGenerationResult result = MapCollider2DGenerator.Generate(definition, settings);
            Transform root = EnsureGeneratedRoot(ResolveColliderParent());
            ClearGeneratedColliderComponents(root);

            for (int i = 0; i < result.Paths.Length; i++)
            {
                EdgeCollider2D edgeCollider = root.gameObject.AddComponent<EdgeCollider2D>();
                edgeCollider.points = result.Paths[i];
                edgeCollider.edgeRadius = _edgeRadius;
                edgeCollider.isTrigger = _isTrigger;
                edgeCollider.sharedMaterial = _physicsMaterial;
            }

            _lastBakeStats = MapColliderBakeStats.FromResult(result);
            Debug.Log(
                $"Baked {_lastBakeStats.ColliderCount} map colliders from {_lastBakeStats.RawEdgeCount} contour edges " +
                $"into {_lastBakeStats.SimplifiedPathPointCount} points.",
                this);

            return _lastBakeStats;
        }

        public void ClearGeneratedColliders()
        {
            TryAssignDefinitionFromDiscoverySystem();
            Transform root = FindGeneratedRoot(ResolveColliderParent());
            if (root == null)
            {
                _lastBakeStats = default;
                return;
            }

            ClearGeneratedColliderComponents(root);
            _lastBakeStats = default;
        }

        private void Reset()
        {
            TryAssignDefinitionFromDiscoverySystem();
        }

        private void OnValidate()
        {
            _sourceSampleStep = Mathf.Max(1, _sourceSampleStep);
            _simplificationTolerance = Mathf.Max(0f, _simplificationTolerance);
            _minimumPathPointCount = Mathf.Max(3, _minimumPathPointCount);
            _edgeRadius = Mathf.Max(0f, _edgeRadius);

            if (_definition == null)
            {
                TryAssignDefinitionFromDiscoverySystem();
            }
        }

        private void TryAssignDefinitionFromDiscoverySystem()
        {
            if (_discovery == null)
            {
                _discovery = GetComponent<MapDiscoverySystem>();
            }

            if (_discovery == null)
            {
                _discovery = GetComponentInParent<MapDiscoverySystem>();
            }

            if (_definition == null && _discovery != null)
            {
                _definition = _discovery.Definition;
            }
        }

        private MapDefinition ResolveDefinition()
        {
            return _definition != null ? _definition : _discovery != null ? _discovery.Definition : null;
        }

        private Transform ResolveColliderParent()
        {
            return _discovery != null ? _discovery.transform : transform;
        }

        private Transform EnsureGeneratedRoot(Transform parent)
        {
            Transform root = FindGeneratedRoot(parent);
            if (root != null)
            {
                if (root.parent != parent)
                {
                    root.SetParent(parent, false);
                }

                root.gameObject.layer = parent.gameObject.layer;
                ResetGeneratedRootTransform(root);
                return root;
            }

            GameObject rootObject = new GameObject(GeneratedRootName)
            {
                layer = parent.gameObject.layer
            };

            root = rootObject.transform;
            root.SetParent(parent, false);
            ResetGeneratedRootTransform(root);
            _generatedRoot = root;
            return root;
        }

        private Transform FindGeneratedRoot(Transform parent)
        {
            if (_generatedRoot != null)
            {
                return _generatedRoot;
            }

            Transform existing = parent != null ? parent.Find(GeneratedRootName) : null;
            if (existing == null && parent != transform)
            {
                existing = transform.Find(GeneratedRootName);
            }

            if (existing != null)
            {
                _generatedRoot = existing;
            }

            return _generatedRoot;
        }

        private static void ResetGeneratedRootTransform(Transform root)
        {
            root.localPosition = Vector3.zero;
            root.localRotation = Quaternion.identity;
            root.localScale = Vector3.one;
        }

        private static void ClearGeneratedColliderComponents(Transform root)
        {
            EdgeCollider2D[] colliders = root.GetComponents<EdgeCollider2D>();
            for (int i = colliders.Length - 1; i >= 0; i--)
            {
                DestroyRuntimeObject(colliders[i]);
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
    }
}
