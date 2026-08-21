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
        [SerializeField, Min(0)] private int _requiredBranches = 2;
        [SerializeField, Min(0)] private int _requiredPalmLeaves = 2;
        [SerializeField, Min(0)] private int _requiredRocks = 5;
        [SerializeField, Min(0)] private int _requiredSoil = 1;
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
        [SerializeField] private string _completeMessage = "Listo! mi amiga Naia me ayudó a construir";
        [SerializeField, Min(0f)] private float _completeMessageDuration = 3f;
        [SerializeField] private bool _showCompleteMessageNearAluxe = true;
        [SerializeField] private bool _waitForCompleteMessageBeforeTransition = true;

        [Header("Temple")]
        [SerializeField] private SpriteRenderer _templeRenderer;
        [SerializeField] private GameObject _naiaRoot;
        [SerializeField] private bool _autoFindNaia = true;
        [SerializeField, Min(0.05f)] private float _fadeToCinematicDuration = 3f;

        [Header("Trigger")]
        [SerializeField] private string _playerTag = "Player";
        [SerializeField] private bool _onlyTriggerTransitionOnce = true;
        [SerializeField] private string _nextSceneName = "END";

        private readonly HashSet<string> _collectedItemIds = new();
        private readonly Dictionary<PlatformObjectiveItemType, int> _collectedItemCounts = new();
        private readonly HashSet<string> _discoveredAnimalIds = new();
        private bool _transitionTriggered;
        private bool _hasShownCompleteMessage;
        private float _completeMessageVisibleUntil;

        public static PlatformAluxHouseGate Active { get; private set; }

        public int CollectedCount => _collectedItemIds.Count;
        public int RequiredItemCount => _requiredBranches + _requiredPalmLeaves + _requiredRocks + _requiredSoil;
        public int DiscoveredAnimalCount => _discoveredAnimalIds.Count;
        public int RequiredAnimalCount => _requiredAnimalCount;
        public bool HasAllItems => GetCollectedCount(PlatformObjectiveItemType.Branch) >= _requiredBranches
            && GetCollectedCount(PlatformObjectiveItemType.PalmLeaf) >= _requiredPalmLeaves
            && GetCollectedCount(PlatformObjectiveItemType.Rock) >= _requiredRocks
            && GetCollectedCount(PlatformObjectiveItemType.Soil) >= _requiredSoil;
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

        private void Update()
        {
            TryShowCompleteMessageNearAluxe();
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

            if (!_hasShownCompleteMessage)
                ShowCompleteMessage();

            _transitionTriggered = true;
            StartCoroutine(PlayEndingRoutine());
        }

        private System.Collections.IEnumerator PlayEndingRoutine()
        {
            if (_waitForCompleteMessageBeforeTransition)
            {
                float waitTime = _completeMessageVisibleUntil - Time.time;
                if (waitTime > 0f)
                    yield return new WaitForSeconds(waitTime);
            }

            ComicCinematicAsset comicCinematic = Resources.Load<ComicCinematicAsset>(CinematicSequences.PlatformEndingComic);
            if (comicCinematic != null)
            {
                ComicCinematicPlayer player = ComicCinematicPlayer.Instance;
                yield return player.FadeToBlackRoutine(_fadeToCinematicDuration);
                yield return player.PlayRoutine(comicCinematic, _nextSceneName);
            }
            else
            {
                CinematicSequencePlayer player = CinematicSequencePlayer.Instance;
                yield return player.FadeToBlackRoutine(_fadeToCinematicDuration);
                yield return player.PlayRoutine(CinematicSequences.PlatformEnding, false, _nextSceneName);
            }
        }

        public bool RegisterCollectible(PlatformObjectiveCollectible collectible)
        {
            if (collectible == null)
                return false;

            return RegisterCollectible(collectible.ItemId, collectible.ItemType);
        }

        public bool RegisterCollectible(string itemId, PlatformObjectiveItemType itemType = PlatformObjectiveItemType.Rock)
        {
            if (string.IsNullOrWhiteSpace(itemId))
                itemId = $"Collectible_{_collectedItemIds.Count + 1}";

            bool wasAdded = _collectedItemIds.Add(itemId);
            if (wasAdded)
            {
                _collectedItemCounts[itemType] = GetCollectedCount(itemType) + 1;
                RefreshCounters();
            }

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
            ApplyCounter(_counterText, CollectedCount, RequiredItemCount);
            ApplyCounter(_animalCounterText, DiscoveredAnimalCount, _requiredAnimalCount);
            RefreshCompletionVisibility();
        }

        private void ShowMissingObjectsMessage()
        {
            ResolveAluxeMessageTarget();

            if (_aluxeMessageTarget != null)
                _aluxeMessageTarget.ShowBubbleMessage(_missingObjectsMessage, _missingMessageDuration);
            else
                Debug.Log($"[PlatformAluxHouseGate] {_missingObjectsMessage} {BuildMissingItemsSummary()}, animales {DiscoveredAnimalCount} / {_requiredAnimalCount}.", this);
        }

        private void TryShowCompleteMessageNearAluxe()
        {
            if (!_showCompleteMessageNearAluxe || _hasShownCompleteMessage || !IsComplete)
                return;

            ResolveAluxeMessageTarget();
            if (_aluxeMessageTarget != null && _aluxeMessageTarget.IsPlayerInInteractionRange())
                ShowCompleteMessage();
        }

        private void ShowCompleteMessage()
        {
            ResolveAluxeMessageTarget();
            _hasShownCompleteMessage = true;
            _completeMessageVisibleUntil = Time.time + _completeMessageDuration;

            if (_aluxeMessageTarget != null)
                _aluxeMessageTarget.ShowBubbleMessage(_completeMessage, _completeMessageDuration);
            else
                Debug.Log($"[PlatformAluxHouseGate] {_completeMessage}", this);
        }

        private void RefreshCompletionVisibility()
        {
            if (_templeRenderer == null)
                _templeRenderer = GetComponent<SpriteRenderer>();

            if (_templeRenderer != null)
                _templeRenderer.enabled = IsComplete;

            ResolveNaia();
            if (_naiaRoot != null)
                _naiaRoot.SetActive(IsComplete);
        }

        private void ResolveNaia()
        {
            if (!_autoFindNaia)
                return;

            NaiaBlinkAnimator fallback = null;
            NaiaBlinkAnimator[] naias = FindObjectsByType<NaiaBlinkAnimator>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (NaiaBlinkAnimator naia in naias)
            {
                if (naia == null)
                    continue;

                fallback ??= naia;
                if (naia.gameObject.name == "Naia")
                {
                    _naiaRoot = naia.gameObject;
                    return;
                }
            }

            if (_naiaRoot == null && fallback != null)
                _naiaRoot = fallback.gameObject;
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

        private int GetCollectedCount(PlatformObjectiveItemType itemType)
        {
            return _collectedItemCounts.TryGetValue(itemType, out int count) ? count : 0;
        }

        private string BuildMissingItemsSummary()
        {
            return $"Ramas {GetCollectedCount(PlatformObjectiveItemType.Branch)}/{_requiredBranches}, "
                + $"palmas {GetCollectedCount(PlatformObjectiveItemType.PalmLeaf)}/{_requiredPalmLeaves}, "
                + $"rocas {GetCollectedCount(PlatformObjectiveItemType.Rock)}/{_requiredRocks}, "
                + $"tierra {GetCollectedCount(PlatformObjectiveItemType.Soil)}/{_requiredSoil}";
        }

        private void OnValidate()
        {
            _requiredBranches = Mathf.Max(0, _requiredBranches);
            _requiredPalmLeaves = Mathf.Max(0, _requiredPalmLeaves);
            _requiredRocks = Mathf.Max(0, _requiredRocks);
            _requiredSoil = Mathf.Max(0, _requiredSoil);
            _requiredAnimalCount = Mathf.Max(0, _requiredAnimalCount);
            _completeMessageDuration = Mathf.Max(0f, _completeMessageDuration);
            _fadeToCinematicDuration = Mathf.Max(0.05f, _fadeToCinematicDuration);
        }
    }
}
