using System.Collections;
using GameJam.UI;
using UnityEngine;

namespace GameJam.Player.Cave
{
    [DisallowMultipleComponent]
    public sealed class CavePlayerTriggerTeleporter : MonoBehaviour
    {
        [Header("Linterna")]
        [SerializeField] private string _linternaTriggerId = "LinternaON";
        [SerializeField] private CavePlayerStageTransition _linternaTransition;
        [SerializeField] private Transform _linternaTarget;
        [SerializeField] private bool _triggerLinternaOnlyOnce = true;

        [Header("Initial Reset")]
        [SerializeField] private string _initialTriggerId = "InitialTriggers";
        [SerializeField] private string _initialCollisionId = "InitialColliders";
        [SerializeField] private CavePlayerResetTransition _resetTransition;
        [SerializeField] private Transform _initialResetTarget;

        private Rigidbody2D _rigidbody;
        private Coroutine _linternaRoutine;
        private bool _linternaTriggered;

        private void Reset()
        {
            _linternaTransition = GetComponent<CavePlayerStageTransition>();
            _resetTransition = GetComponent<CavePlayerResetTransition>();
        }

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody2D>();

            if (_linternaTransition == null)
                _linternaTransition = GetComponent<CavePlayerStageTransition>();

            if (_resetTransition == null)
                _resetTransition = GetComponent<CavePlayerResetTransition>();
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (MatchesTrigger(other, _linternaTriggerId))
            {
                TriggerLinterna();
                return;
            }

            if (MatchesTrigger(other, _initialTriggerId))
            {
                TriggerInitialReset();
            }
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (collision == null || collision.collider == null)
                return;

            if (MatchesTrigger(collision.collider, _initialTriggerId)
                || MatchesTrigger(collision.collider, _initialCollisionId))
            {
                TriggerInitialReset();
            }
        }

        private void TriggerLinterna()
        {
            if (_triggerLinternaOnlyOnce && _linternaTriggered)
                return;

            if (_linternaRoutine != null)
                return;

            _linternaTriggered = true;

            _linternaRoutine = StartCoroutine(TriggerLinternaRoutine());
        }

        private IEnumerator TriggerLinternaRoutine()
        {
            yield return CinematicSequencePlayer.Instance.PlayRoutine(
                CinematicSequences.Linterna,
                true,
                "",
                ApplyLinternaStageAtBlack);

            _linternaRoutine = null;
        }

        private void ApplyLinternaStageAtBlack()
        {
            if (_linternaTransition != null)
            {
                _linternaTransition.ApplyTransitionInstantly(_linternaTarget);
                return;
            }

            Vector3 previousPlayerPosition = transform.position;
            TeleportTo(_linternaTarget);
            CaveCameraSnapper.SnapAfterTeleport(transform, previousPlayerPosition);
        }

        private void TriggerInitialReset()
        {
            if (_resetTransition == null)
                _resetTransition = GetComponent<CavePlayerResetTransition>();

            if (_resetTransition == null)
                _resetTransition = gameObject.AddComponent<CavePlayerResetTransition>();

            if (_resetTransition != null)
            {
                _resetTransition.StartReset(_initialResetTarget);
                return;
            }

            Vector3 previousPlayerPosition = transform.position;
            TeleportTo(_initialResetTarget);
            CaveCameraSnapper.SnapAfterTeleport(transform, previousPlayerPosition);
        }

        private bool MatchesTrigger(Collider2D other, string triggerId)
        {
            if (other == null || string.IsNullOrWhiteSpace(triggerId))
                return false;

            Transform current = other.transform;
            while (current != null)
            {
                if (current.name.StartsWith(triggerId, System.StringComparison.Ordinal) || current.gameObject.tag == triggerId)
                    return true;

                current = current.parent;
            }

            return false;
        }

        private void TeleportTo(Transform target)
        {
            if (target == null)
                return;

            if (_rigidbody != null)
            {
                _rigidbody.linearVelocity = Vector2.zero;
                _rigidbody.angularVelocity = 0f;
                _rigidbody.position = target.position;
            }

            transform.position = target.position;
            Physics2D.SyncTransforms();
        }

    }
}
