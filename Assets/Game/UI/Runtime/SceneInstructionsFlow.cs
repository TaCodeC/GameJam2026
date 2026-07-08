using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace GameJam.UI
{
    [DisallowMultipleComponent]
    public sealed class SceneInstructionsFlow : MonoBehaviour
    {
        private const string InstructionsRootName = "Instructions_First";
        private const string FirstPageName = "1_";
        private const string SecondPageName = "2_";
        private const int InstructionsSortingOrder = 2500;

        private static bool s_registered;

        private GameObject _root;
        private GameObject _firstPage;
        private GameObject _secondPage;
        private Button _firstButton;
        private Button _secondButton;
        private UnityAction _firstAction;
        private UnityAction _secondAction;
        private float _previousTimeScale = 1f;
        private float _previousFixedDeltaTime = 0.02f;
        private bool _paused;
        private bool _finished;
        private bool _instructionAudioActive;
        private bool _shownFirstInstructions;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void RegisterSceneHook()
        {
            if (!s_registered)
            {
                SceneManager.sceneLoaded -= OnSceneLoaded;
                SceneManager.sceneLoaded += OnSceneLoaded;
                s_registered = true;
            }

            TryStartForScene(SceneManager.GetActiveScene());
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            TryStartForScene(scene);
        }

        private static void TryStartForScene(Scene scene)
        {
            if (!scene.IsValid())
                return;

            SceneInstructionsFlow[] existingFlows = FindObjectsByType<SceneInstructionsFlow>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < existingFlows.Length; i++)
            {
                if (existingFlows[i] != null && existingFlows[i].gameObject.scene == scene)
                    return;
            }

            GameObject instructionsRoot = FindSceneObject(scene, InstructionsRootName);
            if (instructionsRoot == null)
                return;

            GameObject flowObject = new GameObject("Scene Instructions Flow");
            SceneManager.MoveGameObjectToScene(flowObject, scene);
            SceneInstructionsFlow flow = flowObject.AddComponent<SceneInstructionsFlow>();
            flow.Configure(instructionsRoot);
        }

        private static GameObject FindSceneObject(Scene scene, string objectName)
        {
            GameObject[] allObjects = Resources.FindObjectsOfTypeAll<GameObject>();
            for (int i = 0; i < allObjects.Length; i++)
            {
                GameObject candidate = allObjects[i];
                if (candidate == null || candidate.scene != scene)
                    continue;

                if (candidate.name == objectName)
                    return candidate;
            }

            return null;
        }

        private void Configure(GameObject instructionsRoot)
        {
            _root = instructionsRoot;
            StartCoroutine(Run());
        }

        private IEnumerator Run()
        {
            if (_root == null)
            {
                Finish();
                yield break;
            }

            ResolvePages();
            if (_firstPage == null)
            {
                Finish();
                yield break;
            }

            EnsureEventSystem();
            PauseGame();
            StartInstructionAudio();
            ShowFirstPage();
            BindButtons();

            while (!_finished)
            {
                if (!CurrentPageHasButton() && FallbackAdvancePressed())
                    AdvanceFallback();

                yield return null;
            }
        }

        private void ResolvePages()
        {
            _firstPage = FindPage(FirstPageName);
            _secondPage = FindPage(SecondPageName);

            if (_secondPage == null)
                _secondPage = FindFallbackSecondPage();

            _firstButton = _firstPage != null ? _firstPage.GetComponentInChildren<Button>(true) : null;
            _secondButton = _secondPage != null ? _secondPage.GetComponentInChildren<Button>(true) : null;
        }

        private GameObject FindPage(string pageName)
        {
            if (_root == null)
                return null;

            Transform rootTransform = _root.transform;
            for (int i = 0; i < rootTransform.childCount; i++)
            {
                Transform child = rootTransform.GetChild(i);
                if (child.name == pageName)
                    return child.gameObject;
            }

            Transform[] children = _root.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < children.Length; i++)
            {
                if (children[i] != null && children[i].name == pageName)
                    return children[i].gameObject;
            }

            return null;
        }

        private GameObject FindFallbackSecondPage()
        {
            if (_root == null || _firstPage == null)
                return null;

            Transform rootTransform = _root.transform;
            for (int i = 0; i < rootTransform.childCount; i++)
            {
                GameObject child = rootTransform.GetChild(i).gameObject;
                if (child != _firstPage)
                    return child;
            }

            return null;
        }

        private void ShowFirstPage()
        {
            NormalizeInstructionCanvas();
            NormalizeInstructionTransform(_root);
            NormalizeInstructionTransform(_firstPage);
            NormalizeInstructionTransform(_secondPage);

            _root.SetActive(true);
            _firstPage.SetActive(true);
            _shownFirstInstructions = true;

            if (_secondPage != null)
                _secondPage.SetActive(false);

            Canvas.ForceUpdateCanvases();
        }

        private void ShowSecondPage()
        {
            if (_firstPage != null)
                _firstPage.SetActive(false);

            if (_secondPage == null)
            {
                Finish();
                return;
            }

            _secondPage.SetActive(true);
        }

        private static void NormalizeInstructionTransform(GameObject target)
        {
            if (target == null)
                return;

            Transform targetTransform = target.transform;
            if (targetTransform.localScale.sqrMagnitude < 0.0001f)
                targetTransform.localScale = Vector3.one;
        }

        private void NormalizeInstructionCanvas()
        {
            if (_root == null)
                return;

            Canvas canvas = _root.GetComponent<Canvas>();
            if (canvas != null)
            {
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.overrideSorting = true;
                canvas.sortingOrder = Mathf.Max(canvas.sortingOrder, InstructionsSortingOrder);
            }

            GraphicRaycaster raycaster = _root.GetComponent<GraphicRaycaster>();
            if (raycaster != null)
                raycaster.enabled = true;
        }

        private void BindButtons()
        {
            if (_firstButton != null)
            {
                _firstAction = ShowSecondPage;
                _firstButton.onClick.AddListener(_firstAction);
            }

            if (_secondButton != null)
            {
                _secondAction = Finish;
                _secondButton.onClick.AddListener(_secondAction);
            }
        }

        private void UnbindButtons()
        {
            if (_firstButton != null && _firstAction != null)
                _firstButton.onClick.RemoveListener(_firstAction);

            if (_secondButton != null && _secondAction != null)
                _secondButton.onClick.RemoveListener(_secondAction);

            _firstAction = null;
            _secondAction = null;
        }

        private void PauseGame()
        {
            if (_paused)
                return;

            _paused = true;
            _previousTimeScale = Time.timeScale;
            _previousFixedDeltaTime = Time.fixedDeltaTime;
            Time.timeScale = 0f;
        }

        private void ResumeGame()
        {
            if (!_paused)
                return;

            _paused = false;
            Time.timeScale = _previousTimeScale;
            Time.fixedDeltaTime = _previousFixedDeltaTime;
        }

        private void Finish()
        {
            if (_finished)
                return;

            _finished = true;
            UnbindButtons();
            StopInstructionAudio();

            if (_root != null)
                _root.SetActive(false);

            ResumeGame();
            if (_shownFirstInstructions)
                TimedCanvasFader.ShowSceneHint();

            Destroy(gameObject);
        }

        private void OnDestroy()
        {
            UnbindButtons();
            StopInstructionAudio();

            if (!_finished)
                ResumeGame();
        }

        private void StartInstructionAudio()
        {
            if (_instructionAudioActive)
                return;

            _instructionAudioActive = true;
            InstructionsAudioController.Active.BeginInstructionsAudio();
        }

        private void StopInstructionAudio()
        {
            if (!_instructionAudioActive)
                return;

            _instructionAudioActive = false;
            InstructionsAudioController.Active.EndInstructionsAudio();
        }

        private void AdvanceFallback()
        {
            if (_secondPage != null && _secondPage.activeSelf)
                Finish();
            else
                ShowSecondPage();
        }

        private bool CurrentPageHasButton()
        {
            if (_firstPage != null && _firstPage.activeSelf)
                return _firstButton != null;

            if (_secondPage != null && _secondPage.activeSelf)
                return _secondButton != null;

            return true;
        }

        private static bool FallbackAdvancePressed()
        {
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
                return true;

            if (Keyboard.current != null
                && (Keyboard.current.enterKey.wasPressedThisFrame
                    || Keyboard.current.spaceKey.wasPressedThisFrame
                    || Keyboard.current.dKey.wasPressedThisFrame
                    || Keyboard.current.rightArrowKey.wasPressedThisFrame))
            {
                return true;
            }

            return Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame;
        }

        private static void EnsureEventSystem()
        {
            if (EventSystem.current != null)
                return;

            GameObject eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<EventSystem>();
            eventSystem.AddComponent<InputSystemUIInputModule>();
        }
    }
}
