using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace GameJam.Gameplay.Credits
{
    [DefaultExecutionOrder(-100)]
    [DisallowMultipleComponent]
    public sealed class CreditsMenuExitBootstrap : MonoBehaviour
    {
        private const string CreditsSceneName = "Creditos";
        private const string MenuSceneName = "Menu";
        private const string PlayerObjectName = "Player";
        private const string EndTriggerName = "END";
        private const string FadeCanvasName = "Fade";
        private const string FallbackFadeName = "Black Fade";
        private const float FadeOutDuration = 0.5f;
        private const int FadeSortingOrder = 10000;

        private bool _loadingMenu;
        private CanvasGroup _fadeCanvasGroup;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void RegisterSceneHook()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
            TryInstall(SceneManager.GetActiveScene());
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            TryInstall(scene);
        }

        private static void TryInstall(Scene scene)
        {
            if (!scene.IsValid() || scene.name != CreditsSceneName)
                return;

            CreditsMenuExitBootstrap existing = FindFirstObjectByType<CreditsMenuExitBootstrap>(FindObjectsInactive.Include);
            if (existing != null)
                return;

            GameObject bootstrapObject = new GameObject("Credits Menu Exit");
            SceneManager.MoveGameObjectToScene(bootstrapObject, scene);
            bootstrapObject.AddComponent<CreditsMenuExitBootstrap>();
        }

        private void Start()
        {
            _fadeCanvasGroup = FindFadeCanvasGroup();
            PrepareFadeCanvas();
            AttachPlayerRelays();
        }

        private void Update()
        {
            if (WasEscapePressed())
                LoadMenu();
        }

        internal void LoadMenu()
        {
            if (_loadingMenu)
                return;

            _loadingMenu = true;
            Time.timeScale = 1f;
            StartCoroutine(FadeOutThenLoadMenu());
        }

        private IEnumerator FadeOutThenLoadMenu()
        {
            _fadeCanvasGroup ??= FindFadeCanvasGroup();
            if (_fadeCanvasGroup == null || FadeOutDuration <= 0f)
            {
                SceneManager.LoadScene(MenuSceneName);
                yield break;
            }

            GameObject fadeObject = _fadeCanvasGroup.gameObject;
            fadeObject.SetActive(true);
            fadeObject.transform.localScale = Vector3.one;
            _fadeCanvasGroup.alpha = 0f;
            _fadeCanvasGroup.interactable = false;
            _fadeCanvasGroup.blocksRaycasts = true;

            float elapsed = 0f;
            while (elapsed < FadeOutDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                _fadeCanvasGroup.alpha = Mathf.Clamp01(elapsed / FadeOutDuration);
                yield return null;
            }

            _fadeCanvasGroup.alpha = 1f;
            SceneManager.LoadScene(MenuSceneName);
        }

        private void PrepareFadeCanvas()
        {
            if (_fadeCanvasGroup == null)
                return;

            _fadeCanvasGroup.transform.localScale = Vector3.one;
            _fadeCanvasGroup.alpha = 0f;
            _fadeCanvasGroup.interactable = false;
            _fadeCanvasGroup.blocksRaycasts = false;

            Canvas fadeCanvas = _fadeCanvasGroup.GetComponent<Canvas>();
            if (fadeCanvas == null)
                fadeCanvas = _fadeCanvasGroup.GetComponentInParent<Canvas>(true);

            if (fadeCanvas != null)
                fadeCanvas.sortingOrder = Mathf.Max(fadeCanvas.sortingOrder, FadeSortingOrder);
        }

        private CanvasGroup FindFadeCanvasGroup()
        {
            CanvasGroup fallback = null;
            CanvasGroup[] groups = Resources.FindObjectsOfTypeAll<CanvasGroup>();
            for (int i = 0; i < groups.Length; i++)
            {
                CanvasGroup group = groups[i];
                if (group == null || group.gameObject.scene != gameObject.scene)
                    continue;

                if (string.Equals(group.name, FadeCanvasName, StringComparison.OrdinalIgnoreCase))
                    return group;

                if (string.Equals(group.name, FallbackFadeName, StringComparison.OrdinalIgnoreCase))
                    fallback = group;
            }

            return fallback;
        }

        private void AttachPlayerRelays()
        {
            Transform player = FindPlayer();
            if (player == null)
            {
                Debug.LogWarning("[Credits] No se encontro el Player para conectar salida a Menu.", this);
                return;
            }

            AddRelay(player.gameObject);

            Collider2D[] colliders = player.GetComponentsInChildren<Collider2D>(true);
            for (int i = 0; i < colliders.Length; i++)
            {
                Collider2D collider = colliders[i];
                if (collider != null)
                    AddRelay(collider.gameObject);
            }
        }

        private void AddRelay(GameObject target)
        {
            CreditsMenuExitColliderRelay relay = target.GetComponent<CreditsMenuExitColliderRelay>();
            if (relay == null)
                relay = target.AddComponent<CreditsMenuExitColliderRelay>();

            relay.Configure(this);
        }

        private static Transform FindPlayer()
        {
            GameObject namedPlayer = GameObject.Find(PlayerObjectName);
            if (namedPlayer != null)
                return namedPlayer.transform;

            MonoBehaviour[] behaviours = FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            for (int i = 0; i < behaviours.Length; i++)
            {
                MonoBehaviour behaviour = behaviours[i];
                if (behaviour != null && behaviour.GetType().Name == "Cave_PlayerController")
                    return behaviour.transform;
            }

            return null;
        }

        private static bool WasEscapePressed()
        {
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
                return true;
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
            if (Input.GetKeyDown(KeyCode.Escape))
                return true;
#endif

            return false;
        }

        internal static bool MatchesEndCollider(Collider2D collider)
        {
            if (collider == null)
                return false;

            Transform current = collider.transform;
            while (current != null)
            {
                if (current.name.StartsWith(EndTriggerName, StringComparison.OrdinalIgnoreCase)
                    || current.CompareTag(EndTriggerName))
                {
                    return true;
                }

                current = current.parent;
            }

            return false;
        }
    }

    [DisallowMultipleComponent]
    public sealed class CreditsMenuExitColliderRelay : MonoBehaviour
    {
        private CreditsMenuExitBootstrap _bootstrap;

        public void Configure(CreditsMenuExitBootstrap bootstrap)
        {
            _bootstrap = bootstrap;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (CreditsMenuExitBootstrap.MatchesEndCollider(other))
                _bootstrap?.LoadMenu();
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (CreditsMenuExitBootstrap.MatchesEndCollider(collision.collider))
                _bootstrap?.LoadMenu();
        }
    }
}
