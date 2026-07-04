#pragma warning disable 0649

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace GameJam.Gameplay.Minigames
{
    public sealed class MinigamePopupCanvas : MonoBehaviour
    {
        [Serializable]
        public sealed class MinigamePanel
        {
            [SerializeField] private string _id = "minigame";
            [SerializeField] private GameObject _root;
            [SerializeField] private Button _testButton;
            [SerializeField] private bool _hideOtherPanels = true;

            public string Id => _id;
            public GameObject Root => _root;
            public Button TestButton => _testButton;
            public bool HideOtherPanels => _hideOtherPanels;
        }

        [SerializeField] private GameObject _popupRoot;
        [SerializeField] private CanvasGroup _popupGroup;
        [SerializeField] private Button _closeButton;
        [SerializeField] private bool _hideOnAwake = true;
        [SerializeField] private bool _createEventSystemIfMissing = true;
        [SerializeField] private bool _registerBoneCompletion = true;
        [SerializeField] private bool _destroyCurrentObjectOnCompletion = true;
        [SerializeField] private MinigameObjectState _currentObjectState;
        [SerializeField] private List<MinigamePanel> _minigames = new();
        [SerializeField] private UnityEvent<string> _opened = new();
        [SerializeField] private UnityEvent _closed = new();

        private readonly List<UnityAction> _testButtonActions = new();
        private readonly List<CompletionBinding> _completionBindings = new();
        private UnityAction _closeAction;
        private bool _buttonsBound;
        private bool _handlingCompletion;
        private int _currentIndex = -1;

        public int CurrentIndex => _currentIndex;
        public string CurrentId => IsValidIndex(_currentIndex) ? _minigames[_currentIndex].Id : string.Empty;
        public MinigameObjectState CurrentObjectState => _currentObjectState;

        private void Awake()
        {
            EnsureEventSystem();

            if (_hideOnAwake)
            {
                Hide(false);
            }
            else if (_popupRoot != null)
            {
                _popupRoot.SetActive(true);
            }
        }

        private void OnEnable()
        {
            BindButtons();
        }

        private void OnDisable()
        {
            UnbindCompletionHandlers();
            UnbindButtons();
        }

        public void ShowById(string minigameId)
        {
            for (int i = 0; i < _minigames.Count; i++)
            {
                if (string.Equals(_minigames[i].Id, minigameId, StringComparison.OrdinalIgnoreCase))
                {
                    ShowByIndex(i);
                    return;
                }
            }

            Debug.LogWarning($"[Minigames] No mini game with id '{minigameId}' is configured.", this);
        }

        public void ShowByIdForObject(MinigameObjectState objectState, string minigameId)
        {
            _currentObjectState = objectState;
            ShowById(minigameId);
        }

        public void ShowByIndexForObject(MinigameObjectState objectState, int index)
        {
            _currentObjectState = objectState;
            ShowByIndex(index);
        }

        public void SetCurrentObjectState(MinigameObjectState objectState)
        {
            _currentObjectState = objectState;

            if (IsValidIndex(_currentIndex))
            {
                BindStateToPanel(_minigames[_currentIndex]);
                BindCompletionHandlers(_minigames[_currentIndex]);
            }
        }

        public void ShowByIndex(int index)
        {
            if (!gameObject.activeSelf)
            {
                gameObject.SetActive(true);
            }

            if (!IsValidIndex(index))
            {
                Debug.LogWarning($"[Minigames] Invalid mini game index {index}.", this);
                return;
            }

            MinigamePanel panel = _minigames[index];
            if (panel.Root == null)
            {
                Debug.LogWarning($"[Minigames] Mini game '{panel.Id}' has no root object assigned.", this);
                return;
            }

            if (_popupRoot != null)
            {
                _popupRoot.SetActive(true);
            }

            if (_popupGroup != null)
            {
                _popupGroup.alpha = 1f;
                _popupGroup.interactable = true;
                _popupGroup.blocksRaycasts = true;
            }

            if (panel.HideOtherPanels)
            {
                HidePanels();
            }

            panel.Root.SetActive(true);
            _currentIndex = index;
            BindStateToPanel(panel);
            BindCompletionHandlers(panel);
            _opened.Invoke(panel.Id);
            Debug.Log($"[Minigames] Opened mini game '{panel.Id}'.", this);
        }

        public void Hide()
        {
            Hide(true);
        }

        private void Hide(bool invokeEvent)
        {
            UnbindCompletionHandlers();
            HidePanels();

            if (_popupGroup != null)
            {
                _popupGroup.alpha = 0f;
                _popupGroup.interactable = false;
                _popupGroup.blocksRaycasts = false;
            }

            if (_popupRoot != null)
            {
                _popupRoot.SetActive(false);
            }

            _currentIndex = -1;

            if (invokeEvent)
            {
                _closed.Invoke();
                Debug.Log("[Minigames] Closed popup.", this);
            }
        }

        private void BindStateToPanel(MinigamePanel panel)
        {
            if (panel == null || panel.Root == null)
            {
                return;
            }

            MonoBehaviour[] behaviours = panel.Root.GetComponentsInChildren<MonoBehaviour>(true);
            for (int i = 0; i < behaviours.Length; i++)
            {
                if (behaviours[i] is IMinigameStateConsumer stateConsumer)
                {
                    stateConsumer.BindState(_currentObjectState, panel.Id);
                }
            }
        }

        private void BindCompletionHandlers(MinigamePanel panel)
        {
            UnbindCompletionHandlers();

            if (panel == null || panel.Root == null)
            {
                return;
            }

            MeasurementMinigame[] measurementMinigames = panel.Root.GetComponentsInChildren<MeasurementMinigame>(true);
            for (int i = 0; i < measurementMinigames.Length; i++)
            {
                UnityAction action = HandleCurrentMinigameCompleted;
                measurementMinigames[i].Completed.AddListener(action);
                _completionBindings.Add(new CompletionBinding(measurementMinigames[i].Completed, action));
            }

            DragDropMinigame[] dragDropMinigames = panel.Root.GetComponentsInChildren<DragDropMinigame>(true);
            for (int i = 0; i < dragDropMinigames.Length; i++)
            {
                UnityAction action = HandleCurrentMinigameCompleted;
                dragDropMinigames[i].Completed.AddListener(action);
                _completionBindings.Add(new CompletionBinding(dragDropMinigames[i].Completed, action));
            }
        }

        private void UnbindCompletionHandlers()
        {
            for (int i = 0; i < _completionBindings.Count; i++)
            {
                _completionBindings[i].Remove();
            }

            _completionBindings.Clear();
        }

        private void HandleCurrentMinigameCompleted()
        {
            if (_handlingCompletion)
            {
                return;
            }

            _handlingCompletion = true;
            MinigameObjectState completedObjectState = _currentObjectState;

            if (_registerBoneCompletion)
            {
                BoneCollectionProgress.Active.RegisterCompletedBone(completedObjectState);
            }

            Hide();

            if (_destroyCurrentObjectOnCompletion && completedObjectState != null)
            {
                Destroy(completedObjectState.gameObject);
            }

            _handlingCompletion = false;
        }

        private void HidePanels()
        {
            for (int i = 0; i < _minigames.Count; i++)
            {
                if (_minigames[i].Root != null)
                {
                    _minigames[i].Root.SetActive(false);
                }
            }
        }

        private void BindButtons()
        {
            if (_buttonsBound)
            {
                return;
            }

            _testButtonActions.Clear();

            for (int i = 0; i < _minigames.Count; i++)
            {
                MinigamePanel panel = _minigames[i];
                if (panel.TestButton == null)
                {
                    _testButtonActions.Add(null);
                    continue;
                }

                int panelIndex = i;
                UnityAction action = () => ShowByIndex(panelIndex);
                panel.TestButton.onClick.AddListener(action);
                _testButtonActions.Add(action);
            }

            if (_closeButton != null)
            {
                _closeAction = Hide;
                _closeButton.onClick.AddListener(_closeAction);
            }

            _buttonsBound = true;
        }

        private void UnbindButtons()
        {
            if (!_buttonsBound)
            {
                return;
            }

            for (int i = 0; i < _minigames.Count && i < _testButtonActions.Count; i++)
            {
                if (_minigames[i].TestButton != null && _testButtonActions[i] != null)
                {
                    _minigames[i].TestButton.onClick.RemoveListener(_testButtonActions[i]);
                }
            }

            if (_closeButton != null && _closeAction != null)
            {
                _closeButton.onClick.RemoveListener(_closeAction);
            }

            _closeAction = null;
            _testButtonActions.Clear();
            _buttonsBound = false;
        }

        private void EnsureEventSystem()
        {
            if (!_createEventSystemIfMissing || EventSystem.current != null)
            {
                return;
            }

            GameObject eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<EventSystem>();
            eventSystem.AddComponent<InputSystemUIInputModule>();
            Debug.Log("[Minigames] Created an EventSystem for UI minigame input.", eventSystem);
        }

        private bool IsValidIndex(int index)
        {
            return index >= 0 && index < _minigames.Count;
        }

        private readonly struct CompletionBinding
        {
            private readonly UnityEvent _event;
            private readonly UnityAction _action;

            public CompletionBinding(UnityEvent unityEvent, UnityAction action)
            {
                _event = unityEvent;
                _action = action;
            }

            public void Remove()
            {
                if (_event != null && _action != null)
                {
                    _event.RemoveListener(_action);
                }
            }
        }
    }
}
