using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace GameJam.Audio
{
    [DisallowMultipleComponent]
    public sealed class ButtonClickAudioBootstrap : MonoBehaviour
    {
        private const float RefreshSeconds = 0.5f;
        private const string MenuSceneName = "Menu";

        private static ButtonClickAudioBootstrap _instance;
        private static bool _sceneHookRegistered;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureInPlayMode()
        {
            EnsureExists();

            if (_sceneHookRegistered)
                return;

            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
            _sceneHookRegistered = true;
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            EnsureExists().AttachToButtons();
        }

        private static ButtonClickAudioBootstrap EnsureExists()
        {
            if (_instance != null)
                return _instance;

            _instance = FindFirstObjectByType<ButtonClickAudioBootstrap>(FindObjectsInactive.Include);
            if (_instance != null)
                return _instance;

            GameObject bootstrapObject = new("Button Click Audio Bootstrap");
            _instance = bootstrapObject.AddComponent<ButtonClickAudioBootstrap>();
            return _instance;
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);
            StartCoroutine(RefreshRoutine());
        }

        private IEnumerator RefreshRoutine()
        {
            while (true)
            {
                AttachToButtons();
                yield return new WaitForSecondsRealtime(RefreshSeconds);
            }
        }

        private void AttachToButtons()
        {
            Button[] buttons = FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < buttons.Length; i++)
            {
                Button button = buttons[i];
                if (button == null || button.GetComponent<ButtonClickAudioEmitter>() != null)
                    continue;

                button.gameObject.AddComponent<ButtonClickAudioEmitter>();
            }
        }

        internal static void RecoverMenuMusicAfterUserGesture()
        {
            Scene activeScene = SceneManager.GetActiveScene();
            if (!activeScene.IsValid() || activeScene.name != MenuSceneName)
                return;

            AudioSource[] sources = FindObjectsByType<AudioSource>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            for (int i = 0; i < sources.Length; i++)
            {
                AudioSource source = sources[i];
                if (source == null
                    || source.gameObject.scene != activeScene
                    || !source.loop
                    || source.isPlaying)
                {
                    continue;
                }

                source.Play();
            }
        }

    }

    [DisallowMultipleComponent]
    public sealed class ButtonClickAudioEmitter : MonoBehaviour, IPointerDownHandler, ISubmitHandler
    {
        private Button _button;

        public void OnPointerDown(PointerEventData eventData)
        {
            if (eventData != null && eventData.button != PointerEventData.InputButton.Left)
                return;

            PlayIfButtonCanClick();
        }

        public void OnSubmit(BaseEventData eventData)
        {
            PlayIfButtonCanClick();
        }

        private void PlayIfButtonCanClick()
        {
            _button ??= GetComponent<Button>();
            if (_button == null || !_button.IsActive() || !_button.IsInteractable())
                return;

            ButtonClickAudioBootstrap.RecoverMenuMusicAfterUserGesture();
            GameAudioSfx.PlayClick();
        }
    }
}
