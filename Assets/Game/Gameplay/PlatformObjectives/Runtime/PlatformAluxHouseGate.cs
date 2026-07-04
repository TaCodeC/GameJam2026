using System.Collections.Generic;
using GameJam.Creatures;
using GameJam.UI;
using TMPro;
using UnityEngine;

namespace GameJam.Gameplay.PlatformObjectives
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider2D))]
    public sealed class PlatformAluxHouseGate : MonoBehaviour
    {
        [Header("Items")]
        [SerializeField, Min(1)] private int _requiredItemCount = 5;
        [SerializeField] private TMP_Text _counterText;

        [Header("Animals")]
        [SerializeField, Min(0)] private int _requiredAnimalCount = 5;
        [SerializeField] private TMP_Text _animalCounterText;

        [Header("Counter Format")]
        [SerializeField] private string _counterCurrentColor = "#F6C453";
        [SerializeField] private string _counterSeparatorColor = "#FFFFFF";
        [SerializeField] private string _counterRequiredColor = "#8FD3FF";
        [SerializeField] private string _counterCompleteColor = "#6BFF8F";

        [Header("Alux Message")]
        [SerializeField] private AluxeInfoInteractable _aluxeMessageTarget;
        [SerializeField] private bool _autoFindAluxeMessageTarget = true;
        [SerializeField] private string _missingObjectsMessage = "Aún te faltan cosas!";
        [SerializeField, Min(0f)] private float _missingMessageDuration = 2.5f;

        [Header("Trigger")]
        [SerializeField] private string _playerTag = "Player";
        [SerializeField] private bool _onlyTriggerTransitionOnce = true;
        [SerializeField] private string _nextSceneName = "END";

        private readonly HashSet<string> _collectedItemIds = new();
        private readonly HashSet<string> _discoveredAnimalIds = new();
        private bool _transitionTriggered;

        public static PlatformAluxHouseGate Active { get; private set; }

        public int CollectedCount => _collectedItemIds.Count;
        public int RequiredItemCount => _requiredItemCount;
        public int DiscoveredAnimalCount => _discoveredAnimalIds.Count;
        public int RequiredAnimalCount => _requiredAnimalCount;
        public bool HasAllItems => CollectedCount >= _requiredItemCount;
        public bool HasAllAnimals => DiscoveredAnimalCount >= _requiredAnimalCount;
        public bool IsComplete => HasAllItems && HasAllAnimals;

        private void Reset()
        {
            Collider2D trigger = GetComponent<Collider2D>();
            if (trigger != null)
                trigger.isTrigger = true;
        }

        private void Awake()
        {
            Collider2D trigger = GetComponent<Collider2D>();
            if (trigger != null)
                trigger.isTrigger = true;

            ResolveAluxeMessageTarget();
            RefreshCounters();
        }

        private void OnEnable()
        {
            Active = this;
            ResolveAluxeMessageTarget();
            RefreshCounters();
        }

        private void OnDisable()
        {
            if (Active == this)
                Active = null;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!PlatformObjectivePlayerDetector.IsPlayer(other, _playerTag))
                return;

            if (!IsComplete)
            {
                ShowMissingObjectsMessage();
                return;
            }

            if (_transitionTriggered && _onlyTriggerTransitionOnce)
                return;

            _transitionTriggered = true;
            StartCoroutine(PlayEndingRoutine());
        }

        private System.Collections.IEnumerator PlayEndingRoutine()
        {
            yield return CinematicSequencePlayer.Instance.PlayRoutine(CinematicSequences.PlatformEnding, false, _nextSceneName);
        }

        public bool RegisterCollectible(PlatformObjectiveCollectible collectible)
        {
            if (collectible == null)
                return false;

            return RegisterCollectible(collectible.ItemId);
        }

        public bool RegisterCollectible(string itemId)
        {
            if (string.IsNullOrWhiteSpace(itemId))
                itemId = $"Collectible_{_collectedItemIds.Count + 1}";

            bool wasAdded = _collectedItemIds.Add(itemId);
            if (wasAdded)
                RefreshCounters();

            return wasAdded;
        }

        public bool RegisterAnimalDiscovery(string animalId)
        {
            if (string.IsNullOrWhiteSpace(animalId))
                animalId = $"Animal_{_discoveredAnimalIds.Count + 1}";

            bool wasAdded = _discoveredAnimalIds.Add(animalId);
            if (wasAdded)
                RefreshCounters();

            return wasAdded;
        }

        public void RefreshCounters()
        {
            ApplyCounter(_counterText, CollectedCount, _requiredItemCount);
            ApplyCounter(_animalCounterText, DiscoveredAnimalCount, _requiredAnimalCount);
        }

        private void ShowMissingObjectsMessage()
        {
            ResolveAluxeMessageTarget();

            if (_aluxeMessageTarget != null)
                _aluxeMessageTarget.ShowBubbleMessage(_missingObjectsMessage, _missingMessageDuration);
            else
                Debug.Log($"[PlatformAluxHouseGate] {_missingObjectsMessage} Objetos {CollectedCount} / {_requiredItemCount}, animales {DiscoveredAnimalCount} / {_requiredAnimalCount}.", this);
        }

        private void ResolveAluxeMessageTarget()
        {
            if (_aluxeMessageTarget != null || !_autoFindAluxeMessageTarget)
                return;

            _aluxeMessageTarget = FindFirstObjectByType<AluxeInfoInteractable>(FindObjectsInactive.Include);
        }

        private void ApplyCounter(TMP_Text label, int current, int required)
        {
            if (label == null)
                return;

            label.richText = true;
            label.text = FormatCounter(current, required);
        }

        private string FormatCounter(int current, int required)
        {
            if (current >= required)
                return $"<color={_counterCompleteColor}>{current} / {required}</color>";

            return $"<color={_counterCurrentColor}>{current}</color><color={_counterSeparatorColor}> / </color><color={_counterRequiredColor}>{required}</color>";
        }
    }
}
