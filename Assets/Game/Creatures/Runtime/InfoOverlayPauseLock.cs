using System.Collections.Generic;
using UnityEngine;

namespace GameJam.Creatures
{
    internal sealed class InfoOverlayPauseLock
    {
        private static int s_timePauseDepth;
        private static float s_previousTimeScale = 1f;
        private static float s_previousFixedDeltaTime = 0.02f;

        private readonly PlayerMovementLock _movementLock;
        private readonly bool _pausedTime;
        private bool _isReleased;

        private InfoOverlayPauseLock(bool pausedTime, PlayerMovementLock movementLock)
        {
            _pausedTime = pausedTime;
            _movementLock = movementLock;
        }

        public static InfoOverlayPauseLock Acquire(Transform player, bool pauseTime, bool lockPlayerMovement)
        {
            bool pausedTime = false;
            if (pauseTime)
            {
                if (s_timePauseDepth == 0)
                {
                    s_previousTimeScale = Time.timeScale;
                    s_previousFixedDeltaTime = Time.fixedDeltaTime;
                }

                s_timePauseDepth++;
                Time.timeScale = 0f;
                pausedTime = true;
            }

            PlayerMovementLock movementLock = lockPlayerMovement
                ? PlayerMovementLock.Create(player)
                : null;

            return new InfoOverlayPauseLock(pausedTime, movementLock);
        }

        public void Release()
        {
            if (_isReleased)
                return;

            _isReleased = true;

            _movementLock?.Release();

            if (!_pausedTime)
                return;

            s_timePauseDepth = Mathf.Max(0, s_timePauseDepth - 1);
            if (s_timePauseDepth > 0)
                return;

            Time.timeScale = s_previousTimeScale;
            Time.fixedDeltaTime = s_previousFixedDeltaTime;
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
                List<BehaviourState> controllers = new();
                Behaviour[] behaviours = player.GetComponentsInChildren<Behaviour>(true);

                foreach (Behaviour behaviour in behaviours)
                {
                    if (IsMovementController(behaviour))
                        controllers.Add(new BehaviourState(behaviour, behaviour.enabled));
                }

                return controllers.ToArray();
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
