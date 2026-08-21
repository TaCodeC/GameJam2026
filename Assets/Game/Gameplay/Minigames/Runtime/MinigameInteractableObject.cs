#pragma warning disable 0649

using System;
using UnityEngine;

namespace GameJam.Gameplay.Minigames
{
    [RequireComponent(typeof(MinigameObjectState))]
    public sealed class MinigameInteractableObject : MonoBehaviour
    {
        [SerializeField] private MinigamePopupCanvas _popupCanvas;
        [SerializeField] private MinigamePopupCanvas _popupCanvasPrefab;
        [SerializeField] private string _minigameId = "measurement";
        [SerializeField] private bool _openOnPlayerTouch = true;
        [SerializeField] private bool _openOnPointerDown = true;
        [SerializeField, Min(0f)] private float _openCooldown = 0.35f;

        private MinigameObjectState _state;
        private MinigamePopupCanvas _spawnedPopupCanvas;
        private bool _openedDuringCurrentTouch;
        private float _lastOpenTime = -999f;

        public MinigameObjectState State
        {
            get
            {
                if (_state == null)
                {
                    _state = GetComponent<MinigameObjectState>();
                }

                return _state;
            }
        }

        public void OpenMinigame()
        {
            TryOpenMinigame();
        }

        public void OpenMinigame(string minigameId)
        {
            _minigameId = minigameId;
            OpenMinigame();
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            TryOpenFromPlayerTouch(other);
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            if (!_openedDuringCurrentTouch)
            {
                TryOpenFromPlayerTouch(other);
            }
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (IsPlayerCollider(other))
            {
                _openedDuringCurrentTouch = false;
            }
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            TryOpenFromPlayerTouch(collision != null ? collision.collider : null);
        }

        private void OnCollisionExit2D(Collision2D collision)
        {
            Collider2D other = collision != null ? collision.collider : null;
            if (IsPlayerCollider(other))
            {
                _openedDuringCurrentTouch = false;
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            TryOpenFromPlayerTouch(other);
        }

        private void OnTriggerExit(Collider other)
        {
            if (IsPlayerCollider(other))
            {
                _openedDuringCurrentTouch = false;
            }
        }

        private void OnCollisionEnter(Collision collision)
        {
            TryOpenFromPlayerTouch(collision != null ? collision.collider : null);
        }

        private void OnCollisionExit(Collision collision)
        {
            Collider other = collision != null ? collision.collider : null;
            if (IsPlayerCollider(other))
            {
                _openedDuringCurrentTouch = false;
            }
        }

        private void OnMouseDown()
        {
            if (_openOnPointerDown)
            {
                TryOpenMinigame();
            }
        }

        private void TryOpenFromPlayerTouch(Collider2D other)
        {
            if (!_openOnPlayerTouch || !IsPlayerCollider(other))
            {
                return;
            }

            if (TryOpenMinigame())
            {
                _openedDuringCurrentTouch = true;
            }
        }

        private void TryOpenFromPlayerTouch(Collider other)
        {
            if (!_openOnPlayerTouch || !IsPlayerCollider(other))
            {
                return;
            }

            if (TryOpenMinigame())
            {
                _openedDuringCurrentTouch = true;
            }
        }

        private bool TryOpenMinigame()
        {
            if (Time.unscaledTime - _lastOpenTime < _openCooldown)
            {
                return false;
            }

            MinigamePopupCanvas popupCanvas = ResolvePopupCanvas();

            if (popupCanvas == null)
            {
                Debug.LogWarning("[Minigames] Cannot open mini game because no popup canvas was found.", this);
                return false;
            }

            if (!popupCanvas.gameObject.activeSelf)
            {
                popupCanvas.gameObject.SetActive(true);
            }

            popupCanvas.ShowByIdForObject(State, _minigameId);
            _lastOpenTime = Time.unscaledTime;
            return true;
        }

        private MinigamePopupCanvas ResolvePopupCanvas()
        {
            if (IsSceneObject(_popupCanvas))
            {
                return _popupCanvas;
            }

            if (_spawnedPopupCanvas != null)
            {
                return _spawnedPopupCanvas;
            }

            MinigamePopupCanvas prefab = _popupCanvasPrefab != null ? _popupCanvasPrefab : _popupCanvas;
            if (prefab != null)
            {
                _spawnedPopupCanvas = Instantiate(prefab);
                _spawnedPopupCanvas.gameObject.name = $"{prefab.gameObject.name} ({name})";
                return _spawnedPopupCanvas;
            }

            _popupCanvas = FindFirstObjectByType<MinigamePopupCanvas>(FindObjectsInactive.Include);
            return _popupCanvas;
        }

        private static bool IsSceneObject(Component component)
        {
            return component != null && component.gameObject.scene.IsValid();
        }

        private bool IsPlayerCollider(Collider2D other)
        {
            if (other == null || other.transform.IsChildOf(transform))
            {
                return false;
            }

            Transform root = other.attachedRigidbody != null ? other.attachedRigidbody.transform : other.transform;
            return HasPlayerMarker(root) || HasPlayerMarker(other.transform);
        }

        private bool IsPlayerCollider(Collider other)
        {
            if (other == null || other.transform.IsChildOf(transform))
            {
                return false;
            }

            Transform root = other.attachedRigidbody != null ? other.attachedRigidbody.transform : other.transform;
            return HasPlayerMarker(root) || HasPlayerMarker(other.transform);
        }

        private static bool HasPlayerMarker(Transform candidate)
        {
            Transform current = candidate;
            while (current != null)
            {
                if (NameLooksLikePlayer(current.name) || HasPlayerControllerComponent(current))
                {
                    return true;
                }

                current = current.parent;
            }

            return false;
        }

        private static bool NameLooksLikePlayer(string objectName)
        {
            return !string.IsNullOrWhiteSpace(objectName)
                && objectName.IndexOf("Player", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static bool HasPlayerControllerComponent(Transform candidate)
        {
            MonoBehaviour[] behaviours = candidate.GetComponents<MonoBehaviour>();
            for (int i = 0; i < behaviours.Length; i++)
            {
                MonoBehaviour behaviour = behaviours[i];
                if (behaviour == null)
                {
                    continue;
                }

                if (behaviour.GetType().Name.IndexOf("Player", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
