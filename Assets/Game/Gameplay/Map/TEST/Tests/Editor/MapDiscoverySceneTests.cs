using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace GameJam.Gameplay.Map.Tests
{
    public sealed class MapDiscoverySceneTests
    {
        [Test]
        public void CaveScene_MapDiscoveryMatchesMapPlane()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/Cave.unity", OpenSceneMode.Single);

            GameObject mapObject = GameObject.Find("MapDiscovery");
            GameObject planeObject = GameObject.Find("Map Plane");

            Assert.That(mapObject, Is.Not.Null);
            Assert.That(planeObject, Is.Not.Null);

            MapDiscoverySystem discovery = mapObject.GetComponent<MapDiscoverySystem>();
            Assert.That(discovery, Is.Not.Null);
            Assert.That(discovery.Definition, Is.Not.Null);
            Assert.That(discovery.Definition.WorldPlane, Is.EqualTo(MapWorldPlane.XY));
            Assert.That(discovery.Definition.FlipWorldX, Is.False);
            Assert.That(discovery.Definition.FlipWorldY, Is.False);
            Assert.That(mapObject.transform.position.x, Is.EqualTo(0f).Within(0.001f));
            Assert.That(mapObject.transform.position.y, Is.EqualTo(0f).Within(0.001f));
            Assert.That(Quaternion.Angle(mapObject.transform.rotation, Quaternion.identity), Is.LessThan(0.01f));

            Renderer planeRenderer = planeObject.GetComponent<Renderer>();
            Assert.That(planeRenderer.bounds.size.x, Is.EqualTo(discovery.Definition.WorldSize.x).Within(0.01f));
            Assert.That(planeRenderer.bounds.size.y, Is.EqualTo(discovery.Definition.WorldSize.y).Within(0.01f));

            Assert.That(AssetDatabase.GetAssetPath(planeRenderer.sharedMaterial.GetTexture("_BaseMap")),
                Is.EqualTo("Assets/Game/Gameplay/Map/TEST/Example/MapBeta_1.PNG"));

            MapDiscoveryView discoveryView = planeObject.GetComponent<MapDiscoveryView>();
            Assert.That(discoveryView, Is.Not.Null);

            SerializedObject serializedView = new SerializedObject(discoveryView);
            Texture mapTextureOverride = serializedView.FindProperty("_mapTextureOverride").objectReferenceValue as Texture;
            Assert.That(AssetDatabase.GetAssetPath(mapTextureOverride),
                Is.EqualTo("Assets/Game/Gameplay/Map/TEST/Example/MapBeta_1.PNG"));

            MapDebugHud debugHud = mapObject.GetComponent<MapDebugHud>();
            Assert.That(debugHud, Is.Not.Null);

            discovery.Initialize();
            Assert.That(discovery.TrackedTransform, Is.Not.Null);
            Assert.That(discovery.TryWorldToUv(discovery.TrackedTransform.position, out _), Is.True);
            Assert.That(discovery.IsWalkable(discovery.TrackedTransform.position), Is.True);

            MapCollider2DBaker baker = mapObject.GetComponent<MapCollider2DBaker>();
            Assert.That(baker, Is.Not.Null);
            Assert.That(baker.LastBakeStats.SourceWidth, Is.EqualTo(discovery.Definition.TraversableMask.width));
            Assert.That(baker.LastBakeStats.SourceHeight, Is.EqualTo(discovery.Definition.TraversableMask.height));
            Assert.That(baker.LastBakeStats.ColliderCount, Is.GreaterThan(0));
        }
    }
}
