using System.Reflection;
using NUnit.Framework;
using GameJam.Creatures;
using UnityEditor;
using UnityEngine;

namespace GameJam.Gameplay.PlatformObjectives.Tests
{
    public sealed class PlatformAluxHouseGateTests
    {
        [Test]
        public void AnimalInfoInteractable_PrefersColliderOnVisualChild()
        {
            GameObject animal = new GameObject("Animal Collider Resolution Test");
            GameObject visual = new GameObject("Visual");
            visual.transform.SetParent(animal.transform);
            animal.AddComponent<BoxCollider2D>();
            visual.AddComponent<SpriteRenderer>();
            BoxCollider2D infoCollider = visual.AddComponent<BoxCollider2D>();

            try
            {
                AnimalInfoInteractable interactable = animal.AddComponent<AnimalInfoInteractable>();
                SerializedObject serializedInfo = new SerializedObject(interactable);

                Assert.That(
                    serializedInfo.FindProperty("_clickTarget").objectReferenceValue,
                    Is.SameAs(infoCollider));
            }
            finally
            {
                Object.DestroyImmediate(animal);
            }
        }

        [Test]
        public void AnimalInfoInteractable_UsesVisualColliderForNearbyCheck()
        {
            GameObject animal = new GameObject("Animal Visual Range Test");
            GameObject visual = new GameObject("Visual");
            GameObject player = new GameObject("Player");
            visual.transform.SetParent(animal.transform);
            visual.transform.localPosition = new Vector3(10f, 0f, 0f);
            player.transform.position = new Vector3(10f, 0f, 0f);

            visual.AddComponent<SpriteRenderer>();
            BoxCollider2D infoCollider = visual.AddComponent<BoxCollider2D>();
            infoCollider.size = new Vector2(2f, 2f);

            try
            {
                AnimalInfoInteractable interactable = animal.AddComponent<AnimalInfoInteractable>();
                SerializedObject serializedInfo = new SerializedObject(interactable);
                serializedInfo.FindProperty("_player").objectReferenceValue = player.transform;
                serializedInfo.FindProperty("_clickTarget").objectReferenceValue = infoCollider;
                serializedInfo.FindProperty("_interactionDistance").floatValue = 0.1f;
                serializedInfo.ApplyModifiedPropertiesWithoutUndo();

                Assert.That(IsPlayerNearby(interactable), Is.True);
            }
            finally
            {
                Object.DestroyImmediate(player);
                Object.DestroyImmediate(animal);
            }
        }

        [Test]
        public void AnimalInfoInteractable_DoesNotCloseWhenLeavingRangeByDefault()
        {
            GameObject animal = new GameObject("Animal Close Range Default Test");

            try
            {
                AnimalInfoInteractable interactable = animal.AddComponent<AnimalInfoInteractable>();
                SerializedObject serializedInfo = new SerializedObject(interactable);

                Assert.That(serializedInfo.FindProperty("_closeInfoWhenLeavingRange").boolValue, Is.False);
            }
            finally
            {
                Object.DestroyImmediate(animal);
            }
        }

        [Test]
        public void AluxeInfoInteractable_DoesNotShowPassiveNearbyBubbleByDefault()
        {
            GameObject aluxe = new GameObject("Aluxe Passive Bubble Default Test");

            try
            {
                AluxeInfoInteractable interactable = aluxe.AddComponent<AluxeInfoInteractable>();
                SerializedObject serializedInfo = new SerializedObject(interactable);

                Assert.That(serializedInfo.FindProperty("_nearbyMessage").stringValue, Is.Empty);
                Assert.That(serializedInfo.FindProperty("_showNearbyBubble").boolValue, Is.False);
            }
            finally
            {
                Object.DestroyImmediate(aluxe);
            }
        }

        [Test]
        public void CompleteMessage_ShowsNearAluxeBeforeHouseTransition()
        {
            GameObject house = new GameObject("Platform Objective Complete Aluxe Message Test");
            GameObject aluxe = new GameObject("Aluxe Complete Message Test");
            GameObject player = new GameObject("Player");
            house.AddComponent<BoxCollider2D>();
            PlatformAluxHouseGate gate = house.AddComponent<PlatformAluxHouseGate>();
            AluxeInfoInteractable interactable = aluxe.AddComponent<AluxeInfoInteractable>();

            try
            {
                player.transform.position = aluxe.transform.position;

                SerializedObject serializedAluxe = new SerializedObject(interactable);
                serializedAluxe.FindProperty("_player").objectReferenceValue = player.transform;
                serializedAluxe.FindProperty("_interactionDistance").floatValue = 1f;
                serializedAluxe.ApplyModifiedPropertiesWithoutUndo();

                SerializedObject serializedGate = new SerializedObject(gate);
                serializedGate.FindProperty("_requiredBranches").intValue = 0;
                serializedGate.FindProperty("_requiredPalmLeaves").intValue = 0;
                serializedGate.FindProperty("_requiredRocks").intValue = 0;
                serializedGate.FindProperty("_requiredSoil").intValue = 0;
                serializedGate.FindProperty("_requiredAnimalCount").intValue = 0;
                serializedGate.FindProperty("_aluxeMessageTarget").objectReferenceValue = interactable;
                serializedGate.FindProperty("_completeMessageDuration").floatValue = 0f;
                serializedGate.FindProperty("_autoFindNaia").boolValue = false;
                serializedGate.ApplyModifiedPropertiesWithoutUndo();

                gate.RefreshCounters();
                InvokePrivate(gate, "TryShowCompleteMessageNearAluxe");

                Assert.That(HasShownCompleteMessage(gate), Is.True);
            }
            finally
            {
                Object.DestroyImmediate(player);
                Object.DestroyImmediate(aluxe);
                Object.DestroyImmediate(house);
            }
        }

        [Test]
        public void HasAllItems_RequiresExactConfiguredCategoryCounts()
        {
            GameObject house = new GameObject("Platform Objective Test House");
            house.AddComponent<BoxCollider2D>();
            PlatformAluxHouseGate gate = house.AddComponent<PlatformAluxHouseGate>();
            DisableAutoFindNaia(gate);

            try
            {
                Assert.That(gate.RequiredItemCount, Is.EqualTo(10));

                Register(gate, PlatformObjectiveItemType.Branch, 2, "Branch");
                Register(gate, PlatformObjectiveItemType.PalmLeaf, 2, "Palm");
                Register(gate, PlatformObjectiveItemType.Rock, 5, "Rock");

                Assert.That(gate.HasAllItems, Is.False);

                Register(gate, PlatformObjectiveItemType.Soil, 1, "Soil");

                Assert.That(gate.CollectedCount, Is.EqualTo(10));
                Assert.That(gate.HasAllItems, Is.True);
            }
            finally
            {
                Object.DestroyImmediate(house);
            }
        }

        [Test]
        public void RegisterCollectible_DoesNotCountDuplicateIdsTwice()
        {
            GameObject house = new GameObject("Platform Objective Duplicate Test House");
            house.AddComponent<BoxCollider2D>();
            PlatformAluxHouseGate gate = house.AddComponent<PlatformAluxHouseGate>();
            DisableAutoFindNaia(gate);

            try
            {
                Assert.That(gate.RegisterCollectible("SameRock", PlatformObjectiveItemType.Rock), Is.True);
                Assert.That(gate.RegisterCollectible("SameRock", PlatformObjectiveItemType.Rock), Is.False);
                Assert.That(gate.CollectedCount, Is.EqualTo(1));
            }
            finally
            {
                Object.DestroyImmediate(house);
            }
        }

        [Test]
        public void TempleAndNaia_AreVisibleOnlyAfterItemsAndAnimalsAreComplete()
        {
            GameObject house = new GameObject("Platform Objective Temple Visibility Test");
            GameObject naia = new GameObject("Naia Visibility Test");
            house.AddComponent<BoxCollider2D>();
            SpriteRenderer renderer = house.AddComponent<SpriteRenderer>();
            PlatformAluxHouseGate gate = house.AddComponent<PlatformAluxHouseGate>();

            SerializedObject serializedGate = new SerializedObject(gate);
            serializedGate.FindProperty("_naiaRoot").objectReferenceValue = naia;
            serializedGate.FindProperty("_autoFindNaia").boolValue = false;
            serializedGate.ApplyModifiedPropertiesWithoutUndo();

            try
            {
                gate.RefreshCounters();
                Assert.That(renderer.enabled, Is.False);
                Assert.That(naia.activeSelf, Is.False);

                Register(gate, PlatformObjectiveItemType.Branch, 2, "Branch");
                Register(gate, PlatformObjectiveItemType.PalmLeaf, 2, "Palm");
                Register(gate, PlatformObjectiveItemType.Rock, 5, "Rock");
                Register(gate, PlatformObjectiveItemType.Soil, 1, "Soil");
                for (int i = 0; i < 5; i++)
                    gate.RegisterAnimalDiscovery($"Animal_{i + 1}");

                Assert.That(gate.IsComplete, Is.True);
                Assert.That(renderer.enabled, Is.True);
                Assert.That(naia.activeSelf, Is.True);
            }
            finally
            {
                Object.DestroyImmediate(naia);
                Object.DestroyImmediate(house);
            }
        }

        private static void Register(
            PlatformAluxHouseGate gate,
            PlatformObjectiveItemType itemType,
            int count,
            string idPrefix)
        {
            for (int i = 0; i < count; i++)
                gate.RegisterCollectible($"{idPrefix}_{i + 1}", itemType);
        }

        private static bool IsPlayerNearby(AnimalInfoInteractable interactable)
        {
            MethodInfo method = typeof(AnimalInfoInteractable).GetMethod("IsPlayerNearby", BindingFlags.Instance | BindingFlags.NonPublic);
            return (bool)method.Invoke(interactable, null);
        }

        private static void DisableAutoFindNaia(PlatformAluxHouseGate gate)
        {
            SerializedObject serializedGate = new SerializedObject(gate);
            serializedGate.FindProperty("_autoFindNaia").boolValue = false;
            serializedGate.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void InvokePrivate(object target, string methodName)
        {
            MethodInfo method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
            method.Invoke(target, null);
        }

        private static bool HasShownCompleteMessage(PlatformAluxHouseGate gate)
        {
            FieldInfo field = typeof(PlatformAluxHouseGate).GetField("_hasShownCompleteMessage", BindingFlags.Instance | BindingFlags.NonPublic);
            return (bool)field.GetValue(gate);
        }
    }
}
