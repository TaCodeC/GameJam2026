using GameJam.Creatures;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace GameJam.Gameplay.PlatformObjectives.Tests
{
    public sealed class GonfoterioRespawnTriggerTests
    {
        [Test]
        public void TryRespawnPlayer_MovesPlayerAndClearsVelocity()
        {
            GameObject gonfoterio = new GameObject("Gonfoterio Test");
            GonfoterioRespawnTrigger trigger = gonfoterio.AddComponent<GonfoterioRespawnTrigger>();
            GameObject respawn = new GameObject("Respawn Test");
            respawn.transform.position = new Vector3(12f, 4f, 0f);
            GameObject player = new GameObject("PlatformPlayer");
            Rigidbody2D body = player.AddComponent<Rigidbody2D>();
            BoxCollider2D playerCollider = player.AddComponent<BoxCollider2D>();
            body.linearVelocity = new Vector2(5f, -3f);

            try
            {
                SerializedObject serializedTrigger = new SerializedObject(trigger);
                serializedTrigger.FindProperty("_respawnTarget").objectReferenceValue = respawn.transform;
                serializedTrigger.FindProperty("_hazardCollider").objectReferenceValue = null;
                serializedTrigger.ApplyModifiedPropertiesWithoutUndo();

                Assert.That(trigger.TryRespawnPlayer(playerCollider), Is.True);
                Assert.That(player.transform.position, Is.EqualTo(respawn.transform.position));
                Assert.That(body.linearVelocity, Is.EqualTo(Vector2.zero));
            }
            finally
            {
                Object.DestroyImmediate(player);
                Object.DestroyImmediate(respawn);
                Object.DestroyImmediate(gonfoterio);
            }
        }
    }
}
