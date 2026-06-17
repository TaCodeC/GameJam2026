using System;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace GameJam.Gameplay.Map.Tests
{
    public sealed class MapCollider2DGeneratorTests
    {
        [Test]
        public void Generate_ReturnsClosedSimplifiedContourAroundBlockedRegion()
        {
            Texture2D mask = CreateMask(4, 4, (x, y) => x >= 1 && x <= 2 && y >= 1 && y <= 2);
            MapDefinition definition = CreateDefinition(mask, new Vector2(4f, 4f), false, false);

            try
            {
                MapCollider2DGenerationResult result = MapCollider2DGenerator.Generate(
                    definition,
                    new MapCollider2DGenerationSettings(1, 0f, 3));

                Assert.That(result.Paths, Has.Length.EqualTo(1));
                Assert.That(result.Paths[0], Has.Length.EqualTo(5));
                AssertClosed(result.Paths[0]);

                Bounds2D bounds = CalculateBounds(result.Paths[0]);
                Assert.That(bounds.MinX, Is.EqualTo(-1f).Within(0.001f));
                Assert.That(bounds.MaxX, Is.EqualTo(1f).Within(0.001f));
                Assert.That(bounds.MinY, Is.EqualTo(-1f).Within(0.001f));
                Assert.That(bounds.MaxY, Is.EqualTo(1f).Within(0.001f));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(definition);
                UnityEngine.Object.DestroyImmediate(mask);
            }
        }

        [Test]
        public void Generate_UsesDefinitionAxisFlips()
        {
            Texture2D mask = CreateMask(2, 2, (x, y) => x == 0 && y == 0);
            MapDefinition definition = CreateDefinition(mask, new Vector2(2f, 2f), true, true);

            try
            {
                MapCollider2DGenerationResult result = MapCollider2DGenerator.Generate(
                    definition,
                    new MapCollider2DGenerationSettings(1, 0f, 3));

                Assert.That(result.Paths, Has.Length.EqualTo(1));

                Bounds2D bounds = CalculateBounds(result.Paths[0]);
                Assert.That(bounds.MinX, Is.EqualTo(0f).Within(0.001f));
                Assert.That(bounds.MaxX, Is.EqualTo(1f).Within(0.001f));
                Assert.That(bounds.MinY, Is.EqualTo(0f).Within(0.001f));
                Assert.That(bounds.MaxY, Is.EqualTo(1f).Within(0.001f));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(definition);
                UnityEngine.Object.DestroyImmediate(mask);
            }
        }

        [Test]
        public void BakeColliders_CreatesEdgeCollidersUnderMapObject()
        {
            Texture2D mask = CreateMask(4, 4, (x, y) => x >= 1 && x <= 2 && y >= 1 && y <= 2);
            MapDefinition definition = CreateDefinition(mask, new Vector2(4f, 4f), false, false);
            GameObject mapObject = new GameObject("MapCollider2DBakerTests");

            try
            {
                MapCollider2DBaker baker = mapObject.AddComponent<MapCollider2DBaker>();
                baker.SetDefinition(definition);

                MapColliderBakeStats stats = baker.BakeColliders();
                EdgeCollider2D[] colliders = mapObject.GetComponentsInChildren<EdgeCollider2D>();

                Assert.That(stats.ColliderCount, Is.EqualTo(1));
                Assert.That(colliders, Has.Length.EqualTo(1));
                Assert.That(colliders[0].points, Has.Length.EqualTo(stats.SimplifiedPathPointCount));
                AssertClosed(colliders[0].points);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(mapObject);
                UnityEngine.Object.DestroyImmediate(definition);
                UnityEngine.Object.DestroyImmediate(mask);
            }
        }

        [Test]
        public void BakeColliders_WhenBakerIsOnVisualChild_UsesParentMapDiscoverySpace()
        {
            Texture2D mask = CreateMask(4, 4, (x, y) => x >= 1 && x <= 2 && y >= 1 && y <= 2);
            MapDefinition definition = CreateDefinition(mask, new Vector2(4f, 4f), false, false);
            GameObject mapObject = new GameObject("MapCollider2DBakerParentTests");
            GameObject visualChild = new GameObject("Visual Map Plane");

            try
            {
                MapDiscoverySystem discovery = mapObject.AddComponent<MapDiscoverySystem>();
                discovery.SetDefinition(definition);

                visualChild.transform.SetParent(mapObject.transform, false);
                visualChild.transform.localRotation = Quaternion.Euler(0f, 0f, 35f);
                visualChild.transform.localScale = new Vector3(4f, 2f, 1f);

                MapCollider2DBaker baker = visualChild.AddComponent<MapCollider2DBaker>();
                baker.SetDefinition(definition);

                baker.BakeColliders();

                Transform generatedRoot = mapObject.transform.Find("Generated Map Colliders");
                Assert.That(generatedRoot, Is.Not.Null);
                Assert.That(generatedRoot.parent, Is.EqualTo(mapObject.transform));
                Assert.That(visualChild.transform.Find("Generated Map Colliders"), Is.Null);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(visualChild);
                UnityEngine.Object.DestroyImmediate(mapObject);
                UnityEngine.Object.DestroyImmediate(definition);
                UnityEngine.Object.DestroyImmediate(mask);
            }
        }

        private static Texture2D CreateMask(int width, int height, Func<int, int, bool> isBlocked)
        {
            Texture2D mask = new Texture2D(width, height, TextureFormat.RGBA32, false, true);
            Color32[] pixels = new Color32[width * height];

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    pixels[y * width + x] = isBlocked(x, y) ? Color.black : Color.white;
                }
            }

            mask.SetPixels32(pixels);
            mask.Apply();
            return mask;
        }

        private static MapDefinition CreateDefinition(Texture2D mask, Vector2 worldSize, bool flipWorldX, bool flipWorldY)
        {
            MapDefinition definition = ScriptableObject.CreateInstance<MapDefinition>();
            SerializedObject serializedDefinition = new SerializedObject(definition);

            serializedDefinition.FindProperty("_traversableMask").objectReferenceValue = mask;
            serializedDefinition.FindProperty("_walkableThreshold").floatValue = 0.5f;
            serializedDefinition.FindProperty("_worldPlane").enumValueIndex = (int)MapWorldPlane.XY;
            serializedDefinition.FindProperty("_worldSize").vector2Value = worldSize;
            serializedDefinition.FindProperty("_flipWorldX").boolValue = flipWorldX;
            serializedDefinition.FindProperty("_flipWorldY").boolValue = flipWorldY;
            serializedDefinition.ApplyModifiedPropertiesWithoutUndo();

            return definition;
        }

        private static void AssertClosed(Vector2[] path)
        {
            Assert.That(path.Length, Is.GreaterThan(1));
            Assert.That(Vector2.Distance(path[0], path[path.Length - 1]), Is.LessThan(0.001f));
        }

        private static Bounds2D CalculateBounds(Vector2[] points)
        {
            Bounds2D bounds = new Bounds2D
            {
                MinX = float.PositiveInfinity,
                MinY = float.PositiveInfinity,
                MaxX = float.NegativeInfinity,
                MaxY = float.NegativeInfinity
            };

            for (int i = 0; i < points.Length; i++)
            {
                bounds.MinX = Mathf.Min(bounds.MinX, points[i].x);
                bounds.MaxX = Mathf.Max(bounds.MaxX, points[i].x);
                bounds.MinY = Mathf.Min(bounds.MinY, points[i].y);
                bounds.MaxY = Mathf.Max(bounds.MaxY, points[i].y);
            }

            return bounds;
        }

        private struct Bounds2D
        {
            public float MinX;
            public float MaxX;
            public float MinY;
            public float MaxY;
        }
    }
}
