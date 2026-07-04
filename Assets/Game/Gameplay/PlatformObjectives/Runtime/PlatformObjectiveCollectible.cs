using UnityEngine;

namespace GameJam.Gameplay.PlatformObjectives
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider2D))]
    public sealed class PlatformObjectiveCollectible : MonoBehaviour
    {
        [SerializeField] private string _itemId = "Collectible";
        [SerializeField] private bool _disableOnCollected = true;
        [SerializeField] private GameObject _visualRoot;
        [SerializeField] private PlatformAluxHouseGate _objective;
        [SerializeField] private string _playerTag = "Player";

        private bool _isCollected;

        public string ItemId => string.IsNullOrWhiteSpace(_itemId) ? gameObject.name : _itemId;
        public bool IsCollected => _isCollected;

        private void Reset()
        {
            _visualRoot = gameObject;
            Collider2D trigger = GetComponent<Collider2D>();
            if (trigger != null)
                trigger.isTrigger = true;
        }

        private void Awake()
        {
            Collider2D trigger = GetComponent<Collider2D>();
            if (trigger != null)
                trigger.isTrigger = true;

            if (_visualRoot == null)
                _visualRoot = gameObject;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_isCollected || !PlatformObjectivePlayerDetector.IsPlayer(other, _playerTag))
                return;

            Collect();
        }

        public void Collect()
        {
            if (_isCollected)
                return;

            _isCollected = true;
            PlatformAluxHouseGate objective = _objective != null ? _objective : PlatformAluxHouseGate.Active;

            if (objective != null)
                objective.RegisterCollectible(this);
            else
                Debug.LogWarning($"[PlatformObjectiveCollectible] No encontre una casita Alux activa para registrar {ItemId}.", this);

            if (_disableOnCollected)
                gameObject.SetActive(false);
            else if (_visualRoot != null)
                _visualRoot.SetActive(false);
        }
    }
}
