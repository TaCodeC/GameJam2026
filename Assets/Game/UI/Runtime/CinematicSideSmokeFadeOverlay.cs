using System;
using UnityEngine;
using UnityEngine.Events;
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

            GameObject prefab = Resources.Load<GameObject>(PrefabResourcePath);
            if (prefab == null)
            {
                Debug.LogWarning($"[Cinematics] No se encontro Resources/{PrefabResourcePath}.", logContext);
                return null;
            }

            GameObject root = UnityEngine.Object.Instantiate(prefab, parent, false);
            root.name = prefab.name;

            if (root.transform is RectTransform rootRect)
                Stretch(rootRect);

            Canvas canvas = root.GetComponent<Canvas>();
            if (canvas != null)
                canvas.overrideSorting = true;

            Button nextButton = FindButton(root.transform, NextButtonName);
            Button previousButton = FindButton(root.transform, PreviousButtonName);
            UnityAction nextAction = () => requestNext?.Invoke();
            UnityAction previousAction = () => requestPrevious?.Invoke();

            ConfigureButton(nextButton, nextAction, NextButtonName, logContext);
            ConfigureButton(previousButton, previousAction, PreviousButtonName, logContext);

            CinematicSideSmokeFadeOverlay overlay = new(
                root,
                nextButton,
                previousButton,
                nextAction,
                previousAction);
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
            if (_nextButton != null)
                _nextButton.onClick.RemoveListener(_nextAction);

            if (_previousButton != null)
                _previousButton.onClick.RemoveListener(_previousAction);

            if (_root != null)
                UnityEngine.Object.Destroy(_root);
        }

        private static void ConfigureButton(
            Button button,
            UnityAction action,
            string buttonName,
            UnityEngine.Object logContext)
        {
            if (button == null)
            {
                Debug.LogWarning($"[Cinematics] El prefab no contiene el boton {buttonName}.", logContext);
                return;
            }

            button.interactable = true;
            button.onClick.AddListener(action);
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
    }
}
