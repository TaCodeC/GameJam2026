#pragma warning disable 0649

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace GameJam.Gameplay.Minigames
{
    public sealed class DragDropMinigame : MonoBehaviour, IMinigameStateConsumer
    {
        [Serializable]
        public sealed class DragDropPair
        {
            [SerializeField] private string _id = "pair";
            [SerializeField] private DragDropItem _draggable;
            [SerializeField] private RectTransform _target;
            [SerializeField] private float _dropRadius = 64f;
            [SerializeField] private bool _snapOnCorrectDrop = true;
            [SerializeField] private bool _lockOnCorrectDrop = true;

            public string Id => _id;
            public DragDropItem Draggable => _draggable;
            public RectTransform Target => _target;
            public float DropRadius => _dropRadius;
            public bool SnapOnCorrectDrop => _snapOnCorrectDrop;
            public bool LockOnCorrectDrop => _lockOnCorrectDrop;
        }

        [SerializeField] private RectTransform _dragPlane;
        [SerializeField] private DragDropPair[] _pairs = Array.Empty<DragDropPair>();
        [SerializeField] private bool _resetOnEnable = true;
        [SerializeField] private bool _logProgress = true;
        [Header("Dev Preview")]
        [SerializeField] private bool _showDropRadiusPreview;
        [SerializeField] private bool _previewOnlyInEditor = true;
        [SerializeField] private Color _previewFillColor = new(0.16f, 0.74f, 0.84f, 0.08f);
        [SerializeField] private Color _previewOutlineColor = new(0.16f, 0.74f, 0.84f, 0.85f);
        [SerializeField, Min(0f)] private float _previewOutlineThickness = 3f;
        [SerializeField, Range(12, 128)] private int _previewSegments = 64;
        [SerializeField] private UnityEvent _completed = new();
        [SerializeField] private UnityEvent<string> _incorrectDrop = new();

        private const string PreviewRootName = "Drop Radius Preview (DEV)";

        private bool[] _solvedPairs = Array.Empty<bool>();
        private readonly List<DropRadiusPreviewGraphic> _previewGraphics = new();
        private RectTransform _previewRoot;
        private Canvas _canvas;
        private MinigameObjectState _boundObjectState;
        private string _boundMinigameId = "drag-drop";

        public RectTransform DragPlane => _dragPlane != null ? _dragPlane : transform as RectTransform;

        private void Awake()
        {
            SetupPairs();
        }

        private void OnEnable()
        {
            SetupPairs();
            RefreshDropRadiusPreview();

            if (_resetOnEnable)
            {
                ResetGame();
            }
        }

        private void OnDisable()
        {
            SetDropRadiusPreviewRootActive(false);
        }

        private void LateUpdate()
        {
            if (_showDropRadiusPreview)
            {
                RefreshDropRadiusPreview();
            }
        }

        private void OnValidate()
        {
            if (_dragPlane == null)
            {
                _dragPlane = transform as RectTransform;
            }

            _previewOutlineThickness = Mathf.Max(0f, _previewOutlineThickness);
            _previewSegments = Mathf.Clamp(_previewSegments, 12, 128);

            if (Application.isPlaying)
            {
                RefreshDropRadiusPreview();
            }
        }

        public void ResetGame()
        {
            EnsureSolvedArray();

            for (int i = 0; i < _pairs.Length; i++)
            {
                _solvedPairs[i] = false;

                if (_pairs[i].Draggable == null)
                {
                    continue;
                }

                _pairs[i].Draggable.Configure(this, i);
                _pairs[i].Draggable.SetLocked(false);
                _pairs[i].Draggable.ResetPose();
            }

            if (_logProgress)
            {
                Debug.Log("[DragDrop] Mini game reset.", this);
            }

            SetBoundState(MinigameResolutionState.InProgress);
            RefreshDropRadiusPreview();
        }

        public void BindState(MinigameObjectState objectState, string minigameId)
        {
            _boundObjectState = objectState;
            _boundMinigameId = string.IsNullOrWhiteSpace(minigameId) ? "drag-drop" : minigameId;
            SetBoundState(MinigameResolutionState.InProgress);
        }

        public void SetDropRadiusPreviewVisible(bool visible)
        {
            _showDropRadiusPreview = visible;
            RefreshDropRadiusPreview();
        }

        public bool TryPlace(DragDropItem item, PointerEventData eventData)
        {
            if (item == null)
            {
                return false;
            }

            int pairIndex = item.PairIndex;
            if (!IsValidPairIndex(pairIndex))
            {
                Debug.LogWarning("[DragDrop] Dragged item is not configured in this mini game.", item);
                return false;
            }

            DragDropPair pair = _pairs[pairIndex];
            bool droppedOnCorrectTarget = IsPointerOverTarget(pair.Target, pair.DropRadius, eventData);
            if (!droppedOnCorrectTarget)
            {
                RegisterIncorrectDrop(pair);
                return false;
            }

            _solvedPairs[pairIndex] = true;

            if (pair.SnapOnCorrectDrop && pair.Target != null)
            {
                item.RectTransform.position = pair.Target.position;
            }

            if (pair.LockOnCorrectDrop)
            {
                item.SetLocked(true);
            }

            RecordBoundAnswer(pair.Id, "correct_drop", true, "correct_drop");

            if (_logProgress)
            {
                Debug.Log($"[DragDrop] Correct drop: '{pair.Id}'.", item);
            }

            if (IsComplete())
            {
                Debug.Log("[DragDrop] Mini game completed.", this);
                SetBoundState(MinigameResolutionState.Completed);
                _completed.Invoke();
            }

            return true;
        }

        private void SetupPairs()
        {
            if (_dragPlane == null)
            {
                _dragPlane = transform as RectTransform;
            }

            EnsureSolvedArray();

            for (int i = 0; i < _pairs.Length; i++)
            {
                if (_pairs[i].Draggable != null)
                {
                    _pairs[i].Draggable.Configure(this, i);
                }
            }

            RefreshDropRadiusPreview();
        }

        private void EnsureSolvedArray()
        {
            if (_solvedPairs.Length == _pairs.Length)
            {
                return;
            }

            _solvedPairs = new bool[_pairs.Length];
        }

        private bool IsPointerOverTarget(RectTransform target, float dropRadius, PointerEventData eventData)
        {
            if (target == null || eventData == null)
            {
                return false;
            }

            if (RectTransformUtility.RectangleContainsScreenPoint(
                    target,
                    eventData.position,
                    eventData.pressEventCamera))
            {
                return true;
            }

            Vector2 targetScreenPosition = RectTransformUtility.WorldToScreenPoint(
                eventData.pressEventCamera,
                target.position);

            return Vector2.Distance(targetScreenPosition, eventData.position) <= dropRadius;
        }

        private void RegisterIncorrectDrop(DragDropPair pair)
        {
            string id = pair != null ? pair.Id : "unknown";
            Debug.Log($"[DragDrop] Incorrect drop for '{id}'.", this);
            RecordBoundAnswer(id, "incorrect_drop", false, "correct_drop");
            _incorrectDrop.Invoke(id);
        }

        private void RecordBoundAnswer(string pairId, string answer, bool isCorrect, string expectedAnswer)
        {
            if (_boundObjectState == null)
            {
                return;
            }

            _boundObjectState.RecordAnswer(
                _boundMinigameId,
                pairId,
                answer,
                isCorrect,
                expectedAnswer);
        }

        private void SetBoundState(MinigameResolutionState state)
        {
            if (_boundObjectState != null)
            {
                if (state == MinigameResolutionState.InProgress
                    && _boundObjectState.GetResolutionState(_boundMinigameId) == MinigameResolutionState.Completed)
                {
                    return;
                }

                _boundObjectState.SetResolutionState(_boundMinigameId, state);
            }
        }

        private bool IsComplete()
        {
            for (int i = 0; i < _solvedPairs.Length; i++)
            {
                if (!_solvedPairs[i])
                {
                    return false;
                }
            }

            return _solvedPairs.Length > 0;
        }

        private bool IsValidPairIndex(int index)
        {
            return index >= 0 && index < _pairs.Length;
        }

        private void RefreshDropRadiusPreview()
        {
            if (!ShouldShowDropRadiusPreview())
            {
                SetDropRadiusPreviewRootActive(false);
                return;
            }

            EnsureDropRadiusPreviewRoot();
            EnsureDropRadiusPreviewCount();
            SetDropRadiusPreviewRootActive(true);

            for (int i = 0; i < _previewGraphics.Count; i++)
            {
                DropRadiusPreviewGraphic preview = _previewGraphics[i];
                bool hasPair = i < _pairs.Length;
                RectTransform target = hasPair ? _pairs[i].Target : null;
                float dropRadius = hasPair ? Mathf.Max(0f, _pairs[i].DropRadius) : 0f;

                if (preview == null || target == null || dropRadius <= 0f)
                {
                    if (preview != null)
                    {
                        preview.gameObject.SetActive(false);
                    }

                    continue;
                }

                RectTransform previewTransform = preview.RectTransform;
                previewTransform.gameObject.SetActive(true);
                previewTransform.anchoredPosition = GetLocalPointOnDragPlane(target.position);

                float localRadius = GetLocalRadiusOnDragPlane(target.position, dropRadius);
                previewTransform.sizeDelta = Vector2.one * (localRadius * 2f);
                preview.SetStyle(
                    _previewFillColor,
                    _previewOutlineColor,
                    _previewOutlineThickness,
                    _previewSegments);
            }
        }

        private bool ShouldShowDropRadiusPreview()
        {
            if (!_showDropRadiusPreview)
            {
                return false;
            }

            if (_previewOnlyInEditor && !Application.isEditor)
            {
                return false;
            }

            return isActiveAndEnabled && DragPlane != null;
        }

        private void EnsureDropRadiusPreviewRoot()
        {
            if (_previewRoot != null)
            {
                return;
            }

            Transform existingRoot = DragPlane.Find(PreviewRootName);
            if (existingRoot != null && existingRoot.TryGetComponent(out _previewRoot))
            {
                CacheExistingPreviewGraphics();
                return;
            }

            GameObject previewRootObject = new GameObject(PreviewRootName, typeof(RectTransform));
            previewRootObject.layer = gameObject.layer;
            _previewRoot = previewRootObject.GetComponent<RectTransform>();
            _previewRoot.SetParent(DragPlane, false);
            _previewRoot.anchorMin = Vector2.zero;
            _previewRoot.anchorMax = Vector2.one;
            _previewRoot.offsetMin = Vector2.zero;
            _previewRoot.offsetMax = Vector2.zero;
            _previewRoot.pivot = new Vector2(0.5f, 0.5f);
            _previewRoot.SetAsLastSibling();
        }

        private void EnsureDropRadiusPreviewCount()
        {
            while (_previewGraphics.Count < _pairs.Length)
            {
                int previewIndex = _previewGraphics.Count;
                GameObject previewObject = new GameObject($"Drop Radius {previewIndex + 1}", typeof(RectTransform), typeof(DropRadiusPreviewGraphic));
                previewObject.layer = gameObject.layer;

                RectTransform previewTransform = previewObject.GetComponent<RectTransform>();
                previewTransform.SetParent(_previewRoot, false);
                previewTransform.anchorMin = new Vector2(0.5f, 0.5f);
                previewTransform.anchorMax = new Vector2(0.5f, 0.5f);
                previewTransform.pivot = new Vector2(0.5f, 0.5f);
                previewTransform.sizeDelta = Vector2.zero;

                DropRadiusPreviewGraphic preview = previewObject.GetComponent<DropRadiusPreviewGraphic>();
                preview.raycastTarget = false;
                _previewGraphics.Add(preview);
            }

            for (int i = _pairs.Length; i < _previewGraphics.Count; i++)
            {
                if (_previewGraphics[i] != null)
                {
                    _previewGraphics[i].gameObject.SetActive(false);
                }
            }
        }

        private void CacheExistingPreviewGraphics()
        {
            _previewGraphics.Clear();
            _previewRoot.GetComponentsInChildren(true, _previewGraphics);
        }

        private void SetDropRadiusPreviewRootActive(bool active)
        {
            if (_previewRoot != null)
            {
                _previewRoot.gameObject.SetActive(active);
            }
        }

        private Vector2 GetLocalPointOnDragPlane(Vector3 worldPosition)
        {
            RectTransform dragPlane = DragPlane;
            Camera camera = GetCanvasCamera();
            Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(camera, worldPosition);

            return RectTransformUtility.ScreenPointToLocalPointInRectangle(dragPlane, screenPoint, camera, out Vector2 localPoint)
                ? localPoint
                : Vector2.zero;
        }

        private float GetLocalRadiusOnDragPlane(Vector3 worldPosition, float screenRadius)
        {
            RectTransform dragPlane = DragPlane;
            Camera camera = GetCanvasCamera();
            Vector2 centerScreen = RectTransformUtility.WorldToScreenPoint(camera, worldPosition);

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(dragPlane, centerScreen, camera, out Vector2 centerLocal)
                || !RectTransformUtility.ScreenPointToLocalPointInRectangle(dragPlane, centerScreen + Vector2.right * screenRadius, camera, out Vector2 edgeLocal))
            {
                return screenRadius;
            }

            return Vector2.Distance(centerLocal, edgeLocal);
        }

        private Camera GetCanvasCamera()
        {
            if (_canvas == null)
            {
                _canvas = GetComponentInParent<Canvas>();
            }

            if (_canvas == null || _canvas.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                return null;
            }

            return _canvas.worldCamera;
        }
    }
}
