using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace GameJam.Gameplay.Map.Tests
{
    public sealed class MapDiscoverySystemTests
    {
        private Texture2D _mask;
        private MapDefinition _definition;
        private GameObject _mapObject;
        private MapDiscoverySystem _discovery;

        [SetUp]
        public void SetUp()
        {
            _mask = CreateHalfWalkableMask();
            _definition = ScriptableObject.CreateInstance<MapDefinition>();

            SerializedObject definition = new SerializedObject(_definition);
            definition.FindProperty("_traversableMask").objectReferenceValue = _mask;
            definition.FindProperty("_walkableThreshold").floatValue = 0.5f;
            definition.FindProperty("_worldPlane").enumValueIndex = (int)MapWorldPlane.XY;
            definition.FindProperty("_worldSize").vector2Value = new Vector2(8f, 8f);
            definition.FindProperty("_discoveryResolution").vector2IntValue = new Vector2Int(64, 64);
            definition.ApplyModifiedPropertiesWithoutUndo();

            _mapObject = new GameObject("MapDiscoverySystemTests");
            _discovery = _mapObject.AddComponent<MapDiscoverySystem>();
            _discovery.SetDefinition(_definition);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_mapObject);
            Object.DestroyImmediate(_definition);
            Object.DestroyImmediate(_mask);
        }

        [Test]
        public void IsWalkable_UsesMaskAndWorldMapping()
        {
            Assert.That(_discovery.IsWalkable(new Vector3(-2f, 0f, 0f)), Is.True);
            Assert.That(_discovery.IsWalkable(new Vector3(2f, 0f, 0f)), Is.False);
            Assert.That(_discovery.IsWalkable(new Vector3(-5f, 0f, 0f)), Is.False);
        }

        [Test]
        public void IsWalkable_RespectsMapDiscoveryRotation()
        {
            _mapObject.transform.rotation = Quaternion.Euler(0f, 0f, 180f);

            Assert.That(_discovery.IsWalkable(new Vector3(2f, 0f, 0f)), Is.True);
            Assert.That(_discovery.IsWalkable(new Vector3(-2f, 0f, 0f)), Is.False);
        }

        [Test]
        public void TryWorldToUv_RespectsDefinitionAxisFlips()
        {
            SerializedObject definition = new SerializedObject(_definition);
            definition.FindProperty("_flipWorldX").boolValue = true;
            definition.FindProperty("_flipWorldY").boolValue = true;
            definition.ApplyModifiedPropertiesWithoutUndo();

            Assert.That(_discovery.TryWorldToUv(new Vector3(-2f, 2f, 0f), out Vector2 uv), Is.True);
            Assert.That(uv.x, Is.EqualTo(0.75f).Within(0.001f));
            Assert.That(uv.y, Is.EqualTo(0.25f).Within(0.001f));
        }

        [Test]
        public void RecordPosition_SeparatesDiscoveredAndVisitedData()
        {
            Vector3 visitedPoint = new Vector3(-2f, 0f, 0f);
            Vector3 nearbyPoint = new Vector3(-1f, 0f, 0f);

            _discovery.RecordPosition(visitedPoint);

            Assert.That(_discovery.HasBeenVisited(visitedPoint), Is.True);
            Assert.That(_discovery.IsDiscovered(nearbyPoint), Is.True);
            Assert.That(_discovery.HasBeenVisited(nearbyPoint), Is.False);
            Assert.That(_discovery.DiscoveredFraction, Is.GreaterThan(_discovery.VisitedFraction));
        }

        [Test]
        public void DiscoveryTexture_PaintsAtSameUvAsWorldPosition()
        {
            Vector3 upperLeftWorldPosition = new Vector3(-2f, 2f, 0f);

            Assert.That(_discovery.TryWorldToUv(upperLeftWorldPosition, out Vector2 expectedUv), Is.True);
            Assert.That(expectedUv.x, Is.EqualTo(0.25f).Within(0.001f));
            Assert.That(expectedUv.y, Is.EqualTo(0.75f).Within(0.001f));

            _discovery.RecordPosition(upperLeftWorldPosition);

            Color expectedPixel = _discovery.DiscoveryTexture.GetPixelBilinear(expectedUv.x, expectedUv.y);
            Color oppositePixel = _discovery.DiscoveryTexture.GetPixelBilinear(1f - expectedUv.x, 1f - expectedUv.y);

            Assert.That(expectedPixel.r, Is.GreaterThan(0.9f));
            Assert.That(oppositePixel.r, Is.LessThan(0.1f));
        }

        [Test]
        public void CanTraverseSegment_RejectsBlockedPixelsAndMapEdges()
        {
            Assert.That(
                _discovery.CanTraverseSegment(new Vector3(-3f, 0f, 0f), new Vector3(-1f, 0f, 0f)),
                Is.True);
            Assert.That(
                _discovery.CanTraverseSegment(new Vector3(-1f, 0f, 0f), new Vector3(1f, 0f, 0f)),
                Is.False);
            Assert.That(
                _discovery.CanTraverseSegment(new Vector3(-3f, 0f, 0f), new Vector3(-5f, 0f, 0f)),
                Is.False);
        }

        [Test]
        public void MapDebugHud_Initialize_CreatesMapButtonAndClosedOverlay()
        {
            GameObject player = new GameObject("MapDebugHudTests_Player");

            try
            {
                _discovery.SetTrackedTransform(player.transform);

                MapDebugHud hud = _mapObject.AddComponent<MapDebugHud>();
                hud.Configure(_discovery, player.transform);
                hud.Initialize();

                Assert.That(hud.HudRoot, Is.Not.Null);
                Assert.That(hud.HudRoot.transform.Find("Open Map Button"), Is.Not.Null);
                Transform overlay = hud.HudRoot.transform.Find("Map Overlay");
                Assert.That(overlay, Is.Not.Null);
                Assert.That(overlay.gameObject.activeSelf, Is.False);
                Assert.That(hud.HudRoot.transform.Find("Map Overlay/Map Frame/Discovered Map"), Is.Not.Null);
                Assert.That(hud.HudRoot.transform.Find("Map Overlay/Map Frame/Discovered Map/Map Markers/Player Position"), Is.Not.Null);
            }
            finally
            {
                Object.DestroyImmediate(player);
            }
        }

        [Test]
        public void MapDebugHud_UsesLogicalUvWithoutRotatingPreviews()
        {
            _mapObject.transform.rotation = Quaternion.Euler(0f, 0f, 180f);
            GameObject player = new GameObject("MapDebugHudTests_RotatedPlayer");

            try
            {
                player.transform.position = new Vector3(2f, 2f, 0f);
                MapDebugHud hud = _mapObject.AddComponent<MapDebugHud>();

                hud.Configure(_discovery, player.transform);
                hud.Initialize();
                hud.OpenMap();

                Transform discoveredMap = hud.HudRoot.transform.Find("Map Overlay/Map Frame/Discovered Map");
                RectTransform marker = (RectTransform)discoveredMap.Find("Map Markers/Player Position");

                Assert.That(Mathf.Abs(Mathf.DeltaAngle(discoveredMap.localEulerAngles.z, 0f)), Is.LessThan(0.01f));
                Assert.That(marker.anchorMin.x, Is.EqualTo(0.25f).Within(0.001f));
                Assert.That(marker.anchorMin.y, Is.EqualTo(0.25f).Within(0.001f));
                AssertRectStaysInsideParent((RectTransform)discoveredMap);
            }
            finally
            {
                Time.timeScale = 1f;
                Object.DestroyImmediate(player);
            }
        }

        private static void AssertRectStaysInsideParent(RectTransform child)
        {
            RectTransform parent = (RectTransform)child.parent;
            Vector3[] corners = new Vector3[4];
            child.GetWorldCorners(corners);

            foreach (Vector3 corner in corners)
            {
                Vector3 localCorner = parent.InverseTransformPoint(corner);
                Assert.That(localCorner.x, Is.InRange(parent.rect.xMin - 0.01f, parent.rect.xMax + 0.01f));
                Assert.That(localCorner.y, Is.InRange(parent.rect.yMin - 0.01f, parent.rect.yMax + 0.01f));
            }
        }

        private static Texture2D CreateHalfWalkableMask()
        {
            const int size = 8;
            Texture2D mask = new Texture2D(size, size, TextureFormat.RGBA32, false, true);
            Color32[] pixels = new Color32[size * size];

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    pixels[y * size + x] = x < size / 2 ? Color.white : Color.black;
                }
            }

            mask.SetPixels32(pixels);
            mask.Apply();
            return mask;
        }
    }
}
