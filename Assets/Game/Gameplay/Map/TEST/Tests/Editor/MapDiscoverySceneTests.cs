using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace GameJam.Gameplay.Map.Tests
{
    public sealed class MapDiscoverySceneTests
    {
        [Test]
        public void SampleScene_MapDiscoveryMatchesMapPlane()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity", OpenSceneMode.Single);

            GameObject mapObject = GameObject.Find("MapDiscovery");
            GameObject planeObject = GameObject.Find("Map Plane");

            Assert.That(mapObject, Is.Not.Null);
            Assert.That(planeObject, Is.Not.Null);

            MapDiscoverySystem discovery = mapObject.GetComponent<MapDiscoverySystem>();
            Assert.That(discovery, Is.Not.Null);
            Assert.That(discovery.Definition, Is.Not.Null);
            Assert.That(discovery.Definition.WorldPlane, Is.EqualTo(MapWorldPlane.XY));
            Assert.That(discovery.Definition.FlipWorldX, Is.True);
            Assert.That(discovery.Definition.FlipWorldY, Is.True);
            Assert.That(mapObject.transform.position.x, Is.EqualTo(0f).Within(0.001f));
            Assert.That(mapObject.transform.position.y, Is.EqualTo(0f).Within(0.001f));

            Renderer planeRenderer = planeObject.GetComponent<Renderer>();
            Assert.That(planeRenderer.bounds.size.x, Is.EqualTo(discovery.Definition.WorldSize.x).Within(0.01f));
            Assert.That(planeRenderer.bounds.size.y, Is.EqualTo(discovery.Definition.WorldSize.y).Within(0.01f));
            Assert.That(planeObject.transform.TransformDirection(Vector3.right).x, Is.GreaterThan(0.99f));
            Assert.That(planeObject.transform.TransformDirection(Vector3.forward).y, Is.GreaterThan(0.99f));

            discovery.Initialize();
            Assert.That(discovery.TrackedTransform, Is.Not.Null);
            Assert.That(discovery.TryWorldToUv(discovery.TrackedTransform.position, out Vector2 trackedUv), Is.True);
            Assert.That(trackedUv.x, Is.LessThan(0.5f));
            Assert.That(trackedUv.y, Is.LessThan(0.5f));
            Assert.That(discovery.IsWalkable(discovery.TrackedTransform.position), Is.True);
        }
    }
}
