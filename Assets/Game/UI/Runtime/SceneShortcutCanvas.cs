using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class SceneShortcutCanvas : MonoBehaviour
{
    [Header("Buttons")]
    [SerializeField] private Button _menuButton;
    [SerializeField] private Button _switchSceneButton;
    [SerializeField] private TMP_Text _menuLabel;
    [SerializeField] private TMP_Text _switchSceneLabel;

    [Header("Scenes")]
    [SerializeField] private string _menuSceneName = "Menu";
    [SerializeField] private string _platformSceneName = "Platform";
    [SerializeField] private string _caveSceneName = "Cave";

    [Header("Labels")]
    [SerializeField] private string _menuButtonText = "Menu";
    [SerializeField] private string _goToPlatformText = "Ir a Platformer";
    [SerializeField] private string _goToCaveText = "Ir a Cueva";

    [Header("Input")]
    [SerializeField] private bool _createEventSystemIfMissing = true;

    private bool _buttonsBound;

    private void Awake()
    {
        ResolveReferences();
        EnsureEventSystem();
        UpdateLabels();
    }

    private void OnEnable()
    {
        ResolveReferences();
        BindButtons();
        UpdateLabels();
    }

    private void OnDisable()
    {
        UnbindButtons();
    }

    public void LoadMenu()
    {
        LoadScene(_menuSceneName);
    }

    public void TogglePlatformOrCave()
    {
        string activeScene = SceneManager.GetActiveScene().name;
        string targetScene = activeScene == _platformSceneName ? _caveSceneName : _platformSceneName;
        LoadScene(targetScene);
    }

    private void BindButtons()
    {
        if (_buttonsBound)
        {
            return;
        }

        if (_menuButton != null)
        {
            _menuButton.onClick.AddListener(LoadMenu);
        }

        if (_switchSceneButton != null)
        {
            _switchSceneButton.onClick.AddListener(TogglePlatformOrCave);
        }

        _buttonsBound = true;
    }

    private void UnbindButtons()
    {
        if (!_buttonsBound)
        {
            return;
        }

        if (_menuButton != null)
        {
            _menuButton.onClick.RemoveListener(LoadMenu);
        }

        if (_switchSceneButton != null)
        {
            _switchSceneButton.onClick.RemoveListener(TogglePlatformOrCave);
        }

        _buttonsBound = false;
    }

    private void ResolveReferences()
    {
        if (_menuButton == null)
        {
            _menuButton = FindButton("Menu Button");
        }

        if (_switchSceneButton == null)
        {
            _switchSceneButton = FindButton("Scene Switch Button");
        }

        if (_menuLabel == null && _menuButton != null)
        {
            _menuLabel = _menuButton.GetComponentInChildren<TMP_Text>(true);
        }

        if (_switchSceneLabel == null && _switchSceneButton != null)
        {
            _switchSceneLabel = _switchSceneButton.GetComponentInChildren<TMP_Text>(true);
        }
    }

    private Button FindButton(string buttonName)
    {
        Button[] buttons = GetComponentsInChildren<Button>(true);
        foreach (Button button in buttons)
        {
            if (button.name == buttonName)
            {
                return button;
            }
        }

        return null;
    }

    private void UpdateLabels()
    {
        if (_menuLabel != null)
        {
            _menuLabel.text = _menuButtonText;
        }

        if (_switchSceneLabel != null)
        {
            bool isPlatform = SceneManager.GetActiveScene().name == _platformSceneName;
            _switchSceneLabel.text = isPlatform ? _goToCaveText : _goToPlatformText;
        }
    }

    private void LoadScene(string sceneName)
    {
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            return;
        }

        SceneManager.LoadScene(sceneName);
    }

    private void EnsureEventSystem()
    {
        if (!_createEventSystemIfMissing || EventSystem.current != null)
        {
            return;
        }

        GameObject eventSystem = new("EventSystem");
        eventSystem.AddComponent<EventSystem>();
        eventSystem.AddComponent<InputSystemUIInputModule>();
    }
}
