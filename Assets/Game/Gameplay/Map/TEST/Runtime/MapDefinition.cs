using UnityEngine;

namespace GameJam.Gameplay.Map
{
    public enum MapWorldPlane
    {
        XY,
        XZ
    }

    public enum MapMaskChannel
    {
        Luminance,
        Red,
        Alpha
    }

    [CreateAssetMenu(fileName = "MapDefinition", menuName = "Game Jam/Map/Map Definition")]
    public sealed class MapDefinition : ScriptableObject
    {
        [Header("Source")]
        [SerializeField] private Texture2D _traversableMask;
        [SerializeField] private MapMaskChannel _maskChannel = MapMaskChannel.Luminance;
        [SerializeField, Range(0f, 1f)] private float _walkableThreshold = 0.5f;

        [Header("World mapping")]
        [SerializeField] private MapWorldPlane _worldPlane = MapWorldPlane.XZ;
        [SerializeField] private Vector2 _worldSize = new Vector2(32f, 18f);
        [Tooltip("Invert the horizontal world-to-mask coordinate without rotating the displayed map.")]
        [SerializeField] private bool _flipWorldX;
        [Tooltip("Invert the vertical world-to-mask coordinate without rotating the displayed map.")]
        [SerializeField] private bool _flipWorldY;

        [Header("Discovery data")]
        [Tooltip("Independent from the source image resolution. Lower values use less memory and paint faster.")]
        [SerializeField] private Vector2Int _discoveryResolution = new Vector2Int(512, 288);

        public Texture2D TraversableMask => _traversableMask;
        public MapMaskChannel MaskChannel => _maskChannel;
        public float WalkableThreshold => _walkableThreshold;
        public MapWorldPlane WorldPlane => _worldPlane;
        public Vector2 WorldSize => _worldSize;
        public bool FlipWorldX => _flipWorldX;
        public bool FlipWorldY => _flipWorldY;
        public Vector2Int DiscoveryResolution => _discoveryResolution;

        private void OnValidate()
        {
            _worldSize.x = Mathf.Max(0.01f, _worldSize.x);
            _worldSize.y = Mathf.Max(0.01f, _worldSize.y);
            _discoveryResolution.x = Mathf.Clamp(_discoveryResolution.x, 16, 4096);
            _discoveryResolution.y = Mathf.Clamp(_discoveryResolution.y, 16, 4096);
        }
    }
}
