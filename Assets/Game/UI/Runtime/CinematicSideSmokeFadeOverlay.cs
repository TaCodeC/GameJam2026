using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace GameJam.UI
{
    internal sealed class CinematicSideSmokeFadeOverlay
    {
        private const string PrefabResourcePath = "Cinematics/Prefabs/CinematicSideSmokeFadeCanvas";
        private const string NextButtonName = "SiguienteCuadro";
        private const string PreviousButtonName = "AnteriorCuadro";

        private readonly GameObject _root;
        private readonly Button _nextButton;
        private readonly Button _previousButton;
        private readonly UnityAction _nextAction;
        private readonly UnityAction _previousAction;

        private CinematicSideSmokeFadeOverlay(
            GameObject root,
            Button nextButton,
            Button previousButton,
            UnityAction nextAction,
            UnityAction previousAction)
        {
            _root = root;
            _nextButton = nextButton;
            _previousButton = previousButton;
            _nextAction = nextAction;
            _previousAction = previousAction;
        }

        public static CinematicSideSmokeFadeOverlay Create(
            Transform parent,
            Action requestNext,
            Action requestPrevious,
            UnityEngine.Object logContext)
        {
            if (parent == null)
                return null;

            EnsureEventSystem();
            GameObject prefab = Resources.Load<GameObject>(PrefabResourcePath);
            if (prefab == null)
            {
                Debug.LogWarning($"[Cinematics] No se encontro Resources/{PrefabResourcePath}.", logContext);
                return null;
            }

            GameObject root = new GameObject(prefab.name, typeof(RectTransform));
            root.transform.SetParent(parent, false);
            Stretch(root.GetComponent<RectTransform>());

            Transform prefabTransform = prefab.transform;
            for (int i = 0; i < prefabTransform.childCount; i++)
            {
                UnityEngine.Object.Instantiate(prefabTransform.GetChild(i).gameObject, root.transform, false);
            }

            Button nextButton = FindButton(root.transform, NextButtonName);
            Button previousButton = FindButton(root.transform, PreviousButtonName);
            UnityAction nextAction = () => requestNext?.Invoke();
            UnityAction previousAction = () => requestPrevious?.Invoke();

            if (nextButton != null)
                nextButton.onClick.AddListener(nextAction);

            if (previousButton != null)
                previousButton.onClick.AddListener(previousAction);

            CinematicSideSmokeFadeOverlay overlay = new(root, nextButton, previousButton, nextAction, previousAction);
            overlay.SetVisible(false);
            return overlay;
        }

        public void SetVisible(bool visible)
        {
            if (_root != null)
                _root.SetActive(visible);
        }

        public void Dispose()
        {
            if (_nextButton != null && _nextAction != null)
                _nextButton.onClick.RemoveListener(_nextAction);

            if (_previousButton != null && _previousAction != null)
                _previousButton.onClick.RemoveListener(_previousAction);

            if (_root != null)
                UnityEngine.Object.Destroy(_root);
        }

        private static Button FindButton(Transform root, string buttonName)
        {
            Button[] buttons = root.GetComponentsInChildren<Button>(true);
            for (int i = 0; i < buttons.Length; i++)
            {
                Button button = buttons[i];
                if (button != null && button.name == buttonName)
                    return button;
            }

            return null;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = Vector2.zero;
            rect.localScale = Vector3.one;
        }

        private static void EnsureEventSystem()
        {
            if (EventSystem.current != null)
                return;

            GameObject eventSystem = new("EventSystem");
            eventSystem.AddComponent<EventSystem>();
            eventSystem.AddComponent<InputSystemUIInputModule>();
        }
    }
}
