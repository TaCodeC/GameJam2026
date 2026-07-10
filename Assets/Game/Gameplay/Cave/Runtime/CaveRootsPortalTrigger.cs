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

        [Header("Debug Test Button")]
        [SerializeField] private bool _showDebugTestButton = true;
        [SerializeField] private string _debugButtonText = "Probar raices";
        [SerializeField, Min(32f)] private float _debugButtonWidth = 130f;
        [SerializeField, Min(24f)] private float _debugButtonHeight = 34f;
        [SerializeField, Min(0f)] private float _debugButtonMargin = 16f;

        private bool _triggered;

        private void Reset()
        {
            EnsureTriggerCollider();
        }

        private void Awake()
        {
            EnsureTriggerCollider();
        }

        private void OnGUI()
        {
            if (!_showDebugTestButton || _triggered)
                return;

            Rect buttonRect = new Rect(
                Screen.width - _debugButtonWidth - _debugButtonMargin,
                _debugButtonMargin,
                _debugButtonWidth,
                _debugButtonHeight);

            if (GUI.Button(buttonRect, _debugButtonText))
                TriggerPortalForTest();
        }

        public void SetUnlocked(bool unlocked)
        {
            _unlocked = unlocked;
        }

        public void TriggerPortalForTest()
        {
            if (_triggered)
                return;

            _unlocked = true;
            StartPortalTransition();
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!_unlocked || _triggered || !IsPlayer(other))
                return;

            StartPortalTransition();
        }

        private void StartPortalTransition()
        {
            if (_triggered)
                return;

            _triggered = true;
            StartCoroutine(EnterRootsRoutine());
        }

        private IEnumerator EnterRootsRoutine()
        {
            ComicCinematicAsset comicCinematic = Resources.Load<ComicCinematicAsset>(CinematicSequences.RootsPortalComic);
            if (comicCinematic != null)
                yield return ComicCinematicPlayer.Instance.PlayRoutine(comicCinematic, _nextSceneName);
            else
                yield return CinematicSequencePlayer.Instance.PlayRoutine(CinematicSequences.RootsPortal, false, _nextSceneName);
        }

        private void EnsureTriggerCollider()
        {
            Collider2D trigger = GetComponent<Collider2D>();
            if (trigger == null)
            {
                CircleCollider2D circle = gameObject.AddComponent<CircleCollider2D>();
                if (circle != null)
                {
                    circle.radius = _fallbackTriggerRadius;
                    trigger = circle;
                }
            }

            if (trigger == null)
            {
                BoxCollider2D box = gameObject.AddComponent<BoxCollider2D>();
                if (box != null)
                {
                    float size = _fallbackTriggerRadius * 2f;
                    box.size = new Vector2(size, size);
                    trigger = box;
                }
            }

            if (trigger == null)
            {
                Debug.LogWarning("[RootsPortal] No se pudo crear un Collider2D para el trigger de raices.", this);
                enabled = false;
                return;
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
