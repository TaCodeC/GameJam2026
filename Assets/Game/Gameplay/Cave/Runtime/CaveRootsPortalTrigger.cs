using System.Collections;
using GameJam.UI;
using UnityEngine;

namespace GameJam.Gameplay.Cave
{
    [DisallowMultipleComponent]
    public sealed class CaveRootsPortalTrigger : MonoBehaviour
    {
        [SerializeField] private string _playerTag = "Player";
        [SerializeField] private string _nextSceneName = "Platform";
        [SerializeField] private bool _unlocked;
        [SerializeField, Min(0.1f)] private float _fallbackTriggerRadius = 1.4f;

        private bool _triggered;

        private void Reset()
        {
            EnsureTriggerCollider();
        }

        private void Awake()
        {
            EnsureTriggerCollider();
        }

        public void SetUnlocked(bool unlocked)
        {
            _unlocked = unlocked;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!_unlocked || _triggered || !IsPlayer(other))
                return;

            _triggered = true;
            StartCoroutine(EnterRootsRoutine());
        }

        private IEnumerator EnterRootsRoutine()
        {
            yield return CinematicSequencePlayer.Instance.PlayRoutine(CinematicSequences.RootsPortal, false, _nextSceneName);
        }

        private void EnsureTriggerCollider()
        {
            Collider2D trigger = GetComponent<Collider2D>();
            if (trigger == null)
            {
                CircleCollider2D circle = gameObject.AddComponent<CircleCollider2D>();
                circle.radius = _fallbackTriggerRadius;
                trigger = circle;
            }

            trigger.isTrigger = true;
        }

        private bool IsPlayer(Collider2D other)
        {
            if (other == null)
                return false;

            if (!string.IsNullOrWhiteSpace(_playerTag) && other.CompareTag(_playerTag))
                return true;

            Transform current = other.transform;
            while (current != null)
            {
                if (!string.IsNullOrWhiteSpace(_playerTag) && current.CompareTag(_playerTag))
                    return true;

                if (current.name == "Player" || current.root.name == "Player")
                    return true;

                current = current.parent;
            }

            return false;
        }
    }
}
