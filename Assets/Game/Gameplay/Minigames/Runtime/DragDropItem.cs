#pragma warning disable 0649

using UnityEngine;
using UnityEngine.EventSystems;

namespace GameJam.Gameplay.Minigames
{
    [RequireComponent(typeof(RectTransform))]
    [RequireComponent(typeof(CanvasGroup))]
    public sealed class DragDropItem : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [SerializeField] private bool _returnToStartWhenUnplaced = true;
        [SerializeField] private bool _bringToFrontWhileDragging = true;

        private DragDropMinigame _owner;
        private RectTransform _rectTransform;
        private CanvasGroup _canvasGroup;
        private RectTransform _dragPlane;
        private Transform _startParent;
        private Vector2 _startAnchoredPosition;
        private int _startSiblingIndex;
        private int _pairIndex = -1;
        private bool _isLocked;
        private bool _hasStartPose;

        public RectTransform RectTransform => _rectTransform;
        public int PairIndex => _pairIndex;
        public bool ReturnToStartWhenUnplaced => _returnToStartWhenUnplaced;

        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
            _canvasGroup = GetComponent<CanvasGroup>();
            CaptureStartPose();
        }

        public void Configure(DragDropMinigame owner, int pairIndex)
        {
            _owner = owner;
            _pairIndex = pairIndex;

            if (_rectTransform == null)
            {
                _rectTransform = GetComponent<RectTransform>();
            }

            if (_canvasGroup == null)
            {
                _canvasGroup = GetComponent<CanvasGroup>();
            }

            CaptureStartPose();
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (_isLocked)
            {
                return;
            }

            _dragPlane = _owner != null ? _owner.DragPlane : _rectTransform.parent as RectTransform;

            if (_bringToFrontWhileDragging)
            {
                _rectTransform.SetAsLastSibling();
            }

            _canvasGroup.blocksRaycasts = false;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (_isLocked || _dragPlane == null)
            {
                return;
            }

            if (RectTransformUtility.ScreenPointToWorldPointInRectangle(
                    _dragPlane,
                    eventData.position,
                    eventData.pressEventCamera,
                    out Vector3 worldPosition))
            {
                _rectTransform.position = worldPosition;
            }
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (_isLocked)
            {
                return;
            }

            _canvasGroup.blocksRaycasts = true;

            if (_owner == null || !_owner.TryPlace(this, eventData))
            {
                ResetPose();
            }
        }

        public void CaptureStartPose()
        {
            if (_rectTransform == null)
            {
                return;
            }

            _startParent = _rectTransform.parent;
            _startAnchoredPosition = _rectTransform.anchoredPosition;
            _startSiblingIndex = _rectTransform.GetSiblingIndex();
            _hasStartPose = true;
        }

        public void ResetPose()
        {
            if (!_returnToStartWhenUnplaced || !_hasStartPose)
            {
                return;
            }

            if (_rectTransform.parent != _startParent)
            {
                _rectTransform.SetParent(_startParent, false);
            }

            _rectTransform.anchoredPosition = _startAnchoredPosition;
            _rectTransform.SetSiblingIndex(_startSiblingIndex);
            SetLocked(false);
        }

        public void SetLocked(bool locked)
        {
            _isLocked = locked;
            _canvasGroup.blocksRaycasts = !locked;
            _canvasGroup.interactable = !locked;
        }
    }
}
