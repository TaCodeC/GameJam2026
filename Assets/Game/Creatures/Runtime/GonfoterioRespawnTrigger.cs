using GameJam.Player.Platform;
using UnityEngine;

namespace GameJam.Creatures
{
    [DisallowMultipleComponent]
    public sealed class GonfoterioRespawnTrigger : MonoBehaviour
    {
        [SerializeField] private Collider2D _hazardCollider;
        [SerializeField] private Transform _respawnTarget;
        [SerializeField] private string _respawnTag = "elefantrespawn";
        [SerializeField] private string _playerTag = "Player";
        [SerializeField, Min(0f)] private float _respawnCooldown = 0.35f;

        private float _nextAllowedRespawnTime;

        private void Awake()
        {
            ResolveRespawnTarget();
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            TryRespawnPlayer(other);
        }

        public bool TryRespawnPlayer(Collider2D other)
        {
            if (other == null || Time.unscaledTime < _nextAllowedRespawnTime || !IsPlayer(other))
                return false;

            if (_hazardCollider != null && !_hazardCollider.IsTouching(other))
                return false;

            ResolveRespawnTarget();
            if (_respawnTarget == null)
            {
                Debug.LogWarning($"[GonfoterioRespawnTrigger] No encontre un objeto con el tag '{_respawnTag}'.", this);
                return false;
            }

            Rigidbody2D body = other.attachedRigidbody != null
                ? other.attachedRigidbody
                : other.GetComponentInParent<Rigidbody2D>();
            Transform player = body != null ? body.transform : ResolvePlayerTransform(other.transform);
            if (player == null)
                return false;

            Vector3 destination = _respawnTarget.position;
            if (body != null)
            {
                body.linearVelocity = Vector2.zero;
                body.angularVelocity = 0f;
                body.position = new Vector2(destination.x, destination.y);
            }

            player.position = destination;
            Physics2D.SyncTransforms();
            _nextAllowedRespawnTime = Time.unscaledTime + _respawnCooldown;
            return true;
        }

        private void ResolveRespawnTarget()
        {
            if (_respawnTarget != null || string.IsNullOrWhiteSpace(_respawnTag))
                return;

            try
            {
                GameObject respawn = GameObject.FindWithTag(_respawnTag);
                _respawnTarget = respawn != null ? respawn.transform : null;
            }
            catch (UnityException)
            {
                _respawnTarget = null;
            }
        }

        private bool IsPlayer(Collider2D other)
        {
            Transform current = other.transform;
            while (current != null)
            {
                if (current.GetComponent<Platform_PlayerController>() != null)
                    return true;

                if (!string.IsNullOrWhiteSpace(_playerTag))
                {
                    try
                    {
                        if (current.CompareTag(_playerTag))
                            return true;
                    }
                    catch (UnityException)
                    {
                    }
                }

                if (current.name == "Player" || current.name == "PlatformPlayer")
                    return true;

                current = current.parent;
            }

            return false;
        }

        private static Transform ResolvePlayerTransform(Transform source)
        {
            Transform current = source;
            while (current != null)
            {
                if (current.GetComponent<Platform_PlayerController>() != null)
                    return current;

                current = current.parent;
            }

            return source != null ? source.root : null;
        }

        private void OnValidate()
        {
            _respawnCooldown = Mathf.Max(0f, _respawnCooldown);
        }
    }
}
