using UnityEngine;

namespace GameJam.Gameplay.Chat
{
    [DisallowMultipleComponent]
    public sealed class AluxeConversationTrigger : MonoBehaviour
    {
        [SerializeField] private AluxeChatController _chat;
        [SerializeField] private AluxeSmoothFollow _follower;
        [SerializeField] private bool _triggerOnlyOnce = true;
        [SerializeField] private bool _disableColliderAfterTrigger = true;
        [SerializeField] private bool _lockPlayerMovementDuringDialogue = true;

        private bool _hasTriggered;
        private PlayerMovementLock _movementLock;

        private void Awake()
        {
            ResolveReferences();

            if (TryGetComponent(out Collider2D triggerCollider))
                triggerCollider.isTrigger = true;
        }

        private void OnDisable()
        {
            ReleasePlayerMovement();
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_triggerOnlyOnce && _hasTriggered)
                return;

            if (!TryGetPlayerTransform(other, out Transform player))
                return;

            _hasTriggered = true;

            if (_disableColliderAfterTrigger && TryGetComponent(out Collider2D triggerCollider))
                triggerCollider.enabled = false;

            if (_chat == null)
            {
                BeginFollowing(player);
                return;
            }

            LockPlayerMovement(player);
            bool startedDialogue = _chat.StartDialogue(() =>
            {
                ReleasePlayerMovement();
                BeginFollowing(player);
            });

            if (!startedDialogue)
            {
                ReleasePlayerMovement();
                BeginFollowing(player);
            }
        }

        private void BeginFollowing(Transform player)
        {
            if (_follower == null)
                ResolveReferences();

            if (_follower != null)
                _follower.BeginFollowing(player);
        }

        private void LockPlayerMovement(Transform player)
        {
            if (!_lockPlayerMovementDuringDialogue)
                return;

            ReleasePlayerMovement();
            _movementLock = PlayerMovementLock.Create(player);
        }

        private void ReleasePlayerMovement()
        {
            if (_movementLock == null)
                return;

            _movementLock.Release();
            _movementLock = null;
        }

        private void ResolveReferences()
        {
            if (_chat == null)
                _chat = FindFirstObjectByType<AluxeChatController>(FindObjectsInactive.Include);

            if (_follower == null)
                _follower = GetComponentInParent<AluxeSmoothFollow>();
        }

        private static bool TryGetPlayerTransform(Collider2D other, out Transform player)
        {
            player = null;

            if (other == null)
                return false;

            if (IsPlayerObject(other.gameObject))
            {
                player = other.transform;
                return true;
            }

            if (other.attachedRigidbody != null && IsPlayerObject(other.attachedRigidbody.gameObject))
            {
                player = other.attachedRigidbody.transform;
                return true;
            }

            Transform current = other.transform.parent;
            while (current != null)
            {
                if (IsPlayerObject(current.gameObject))
                {
                    player = current;
                    return true;
                }

                current = current.parent;
            }

            return false;
        }

        private static bool IsPlayerObject(GameObject candidate)
        {
            if (candidate == null)
                return false;

            if (candidate.name == "Player" || HasTag(candidate, "Player"))
                return true;

            return candidate.GetComponent("Cave_PlayerController") != null
                || candidate.GetComponent("Platform_PlayerController") != null;
        }

        private static bool HasTag(GameObject candidate, string tag)
        {
            try
            {
                return candidate.CompareTag(tag);
            }
            catch (UnityException)
            {
                return false;
            }
        }

        private sealed class PlayerMovementLock
        {
            private readonly BehaviourState[] _controllers;
            private readonly RigidbodyState[] _rigidbodies;
            private bool _isReleased;

            private PlayerMovementLock(BehaviourState[] controllers, RigidbodyState[] rigidbodies)
            {
                _controllers = controllers;
                _rigidbodies = rigidbodies;
            }

            public static PlayerMovementLock Create(Transform player)
            {
                if (player == null)
                    return null;

                BehaviourState[] controllers = GetMovementControllers(player);
                RigidbodyState[] rigidbodies = GetRigidbodies(player);

                foreach (BehaviourState controller in controllers)
                {
                    if (controller.Behaviour != null)
                        controller.Behaviour.enabled = false;
                }

                foreach (RigidbodyState rigidbody in rigidbodies)
                {
                    if (rigidbody.Body == null)
                        continue;

                    rigidbody.Body.linearVelocity = Vector2.zero;
                    rigidbody.Body.angularVelocity = 0f;
                    rigidbody.Body.constraints = RigidbodyConstraints2D.FreezeAll;
                }

                return new PlayerMovementLock(controllers, rigidbodies);
            }

            public void Release()
            {
                if (_isReleased)
                    return;

                _isReleased = true;

                foreach (RigidbodyState rigidbody in _rigidbodies)
                {
                    if (rigidbody.Body == null)
                        continue;

                    rigidbody.Body.constraints = rigidbody.Constraints;
                    rigidbody.Body.linearVelocity = Vector2.zero;
                    rigidbody.Body.angularVelocity = 0f;
                }

                foreach (BehaviourState controller in _controllers)
                {
                    if (controller.Behaviour != null)
                        controller.Behaviour.enabled = controller.WasEnabled;
                }
            }

            private static BehaviourState[] GetMovementControllers(Transform player)
            {
                Behaviour[] behaviours = player.GetComponentsInChildren<Behaviour>(true);
                int count = 0;

                foreach (Behaviour behaviour in behaviours)
                {
                    if (IsMovementController(behaviour))
                        count++;
                }

                BehaviourState[] controllers = new BehaviourState[count];
                int index = 0;
                foreach (Behaviour behaviour in behaviours)
                {
                    if (!IsMovementController(behaviour))
                        continue;

                    controllers[index] = new BehaviourState(behaviour, behaviour.enabled);
                    index++;
                }

                return controllers;
            }

            private static RigidbodyState[] GetRigidbodies(Transform player)
            {
                Rigidbody2D[] bodies = player.GetComponentsInChildren<Rigidbody2D>(true);
                RigidbodyState[] rigidbodies = new RigidbodyState[bodies.Length];

                for (int i = 0; i < bodies.Length; i++)
                    rigidbodies[i] = new RigidbodyState(bodies[i], bodies[i].constraints);

                return rigidbodies;
            }

            private static bool IsMovementController(Behaviour behaviour)
            {
                if (behaviour == null)
                    return false;

                string typeName = behaviour.GetType().Name;
                return typeName == "Cave_PlayerController"
                    || typeName == "Platform_PlayerController";
            }
        }

        private readonly struct BehaviourState
        {
            public BehaviourState(Behaviour behaviour, bool wasEnabled)
            {
                Behaviour = behaviour;
                WasEnabled = wasEnabled;
            }

            public Behaviour Behaviour { get; }
            public bool WasEnabled { get; }
        }

        private readonly struct RigidbodyState
        {
            public RigidbodyState(Rigidbody2D body, RigidbodyConstraints2D constraints)
            {
                Body = body;
                Constraints = constraints;
            }

            public Rigidbody2D Body { get; }
            public RigidbodyConstraints2D Constraints { get; }
        }
    }
}
