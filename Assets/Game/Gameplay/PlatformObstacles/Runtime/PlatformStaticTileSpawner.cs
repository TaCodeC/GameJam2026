using System;
using UnityEngine;

namespace GameJam.Gameplay.PlatformObstacles
{
    public enum GeneratedTileColliderMode
    {
        None,
        Box,
        SpritePhysicsShape
    }

    [ExecuteAlways]
    [DisallowMultipleComponent]
    public sealed class PlatformStaticTileSpawner : MonoBehaviour
    {
        [SerializeField] private string _generatedRootName = "Generated Static Tiles";
        [SerializeField] private int _seed = 20260702;
        [SerializeField] private bool _useRandomSeed = false;
        [SerializeField] private bool _markGeneratedObjectsStatic = true;
        [SerializeField] private SpawnGroup[] _groups = new SpawnGroup[0];

        public string GeneratedRootName => string.IsNullOrWhiteSpace(_generatedRootName)
            ? "Generated Static Tiles"
            : _generatedRootName;

        public int Seed => _seed;
        public bool UseRandomSeed => _useRandomSeed;
        public bool MarkGeneratedObjectsStatic => _markGeneratedObjectsStatic;
        public SpawnGroup[] Groups => _groups;

#if UNITY_EDITOR
        public void EditorConfigure(SpawnGroup[] groups, int seed = 20260702)
        {
            _groups = groups ?? new SpawnGroup[0];
            _seed = seed;
        }
#endif

        [Serializable]
        public sealed class SpawnGroup
        {
            [SerializeField] private bool _enabled = true;
            [SerializeField] private string _label = "Tile Group";
            [SerializeField] private string _generatedParentName = "Obstacles";
            [SerializeField] private PlatformStaticTileSpawnZone _zone;
            [SerializeField] private Sprite[] _sprites = new Sprite[0];
            [SerializeField, Min(0)] private int _count = 8;
            [SerializeField, Min(1)] private int _maxAttemptsPerItem = 30;
            [SerializeField, Min(0f)] private float _minSpacing = 0.8f;
            [SerializeField] private Vector2 _uniformScaleRange = new Vector2(1f, 1f);
            [SerializeField] private Vector2 _zRotationRange = Vector2.zero;
            [SerializeField] private Vector2 _yOffsetRange = Vector2.zero;
            [SerializeField] private Vector2 _zOffsetRange = new Vector2(-0.03f, 0.03f);
            [SerializeField] private int _sortingOrder;
            [SerializeField] private GeneratedTileColliderMode _colliderMode = GeneratedTileColliderMode.SpritePhysicsShape;

            public bool Enabled => _enabled;
            public string Label => string.IsNullOrWhiteSpace(_label) ? "Tile Group" : _label;
            public string GeneratedParentName => string.IsNullOrWhiteSpace(_generatedParentName)
                ? "Obstacles"
                : _generatedParentName;
            public PlatformStaticTileSpawnZone Zone => _zone;
            public Sprite[] Sprites => _sprites;
            public int Count => _count;
            public int MaxAttemptsPerItem => _maxAttemptsPerItem;
            public float MinSpacing => _minSpacing;
            public Vector2 UniformScaleRange => _uniformScaleRange;
            public Vector2 ZRotationRange => _zRotationRange;
            public Vector2 YOffsetRange => _yOffsetRange;
            public Vector2 ZOffsetRange => _zOffsetRange;
            public int SortingOrder => _sortingOrder;
            public GeneratedTileColliderMode ColliderMode => _colliderMode;

            public SpawnGroup()
            {
            }

            public SpawnGroup(
                string label,
                string generatedParentName,
                PlatformStaticTileSpawnZone zone,
                Sprite[] sprites,
                int count,
                float minSpacing,
                Vector2 uniformScaleRange,
                Vector2 zRotationRange,
                Vector2 yOffsetRange,
                Vector2 zOffsetRange,
                int sortingOrder,
                GeneratedTileColliderMode colliderMode)
            {
                _label = label;
                _generatedParentName = generatedParentName;
                _zone = zone;
                _sprites = sprites ?? new Sprite[0];
                _count = count;
                _minSpacing = minSpacing;
                _uniformScaleRange = uniformScaleRange;
                _zRotationRange = zRotationRange;
                _yOffsetRange = yOffsetRange;
                _zOffsetRange = zOffsetRange;
                _sortingOrder = sortingOrder;
                _colliderMode = colliderMode;
            }

            public Sprite GetRandomSprite(System.Random random)
            {
                if (_sprites == null || _sprites.Length == 0)
                    return null;

                for (int attempt = 0; attempt < _sprites.Length; attempt++)
                {
                    Sprite sprite = _sprites[random.Next(0, _sprites.Length)];
                    if (sprite != null)
                        return sprite;
                }

                return null;
            }
        }
    }
}
