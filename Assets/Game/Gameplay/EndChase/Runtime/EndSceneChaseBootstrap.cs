using System.Collections;
using GameJam.Gameplay.Map;
using GameJam.Player;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace GameJam.Gameplay.EndChase
{
    [DefaultExecutionOrder(-50)]
    [DisallowMultipleComponent]
    public sealed class EndSceneChaseBootstrap : MonoBehaviour
    {
        private const string EndSceneName = "END";
        private const string PlayerObjectName = "Player";
        private const string LooterObjectName = "Saqueador";
        private const string SpawnObjectName = "Enemyspawn";

        [Header("Spawn")]
        [SerializeField, Min(1f)] private float _minimumSpawnDistance = 18f;
        [SerializeField] private Color _looterSpriteColorMultiplier = new Color(1f, 0.72f, 0.18f, 1f);

        [Header("Marker")]
        [SerializeField] private Color _looterMapMarkerColor = new Color(1f, 0.82f, 0.16f, 1f);
        [SerializeField, Min(1f)] private float _looterMapMarkerDiameter = 18f;

        [Header("Chase")]
        [SerializeField, Min(0.1f)] private float _looterMoveSpeed = 3.75f;
        [SerializeField, Min(0.1f)] private float _captureDistance = 4.25f;

        [Header("Scene Transition")]
        [SerializeField] private string _menuSceneName = "Menu";
        [SerializeField, Min(0f)] private float _fadeOutDuration = 0.45f;
        [SerializeField, Min(0f)] private float _blackHoldDuration = 0.12f;
        [SerializeField, Min(0f)] private float _fadeInDuration = 0.45f;

        private Transform _player;
        private GameObject _looter;
        private EndLooterFleeAI _looterAi;
        private MapDiscoverySystem _map;
        private bool _transitionStarted;

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
            if (!scene.IsValid() || scene.name != EndSceneName)
                return;

            EndSceneChaseBootstrap existing = FindFirstObjectByType<EndSceneChaseBootstrap>(FindObjectsInactive.Include);
            if (existing != null)
                return;

            GameObject bootstrapObject = new GameObject("End Scene Chase");
            SceneManager.MoveGameObjectToScene(bootstrapObject, scene);
            bootstrapObject.AddComponent<EndSceneChaseBootstrap>();
        }

        private IEnumerator Start()
        {
            yield return null;
            InitializeChase();
        }

        private void Update()
        {
            if (_transitionStarted || _player == null || _looter == null)
                return;

            if (PlanarDistance(_player.position, _looter.transform.position) <= _captureDistance)
                StartCoroutine(ReturnToMenuRoutine());
        }

        private void InitializeChase()
        {
            _player = FindPlayer();
            _map = FindMapForPlayer(_player);

            if (_player == null)
            {
                Debug.LogWarning("[End Chase] No se encontro el Player en la escena END.", this);
                return;
            }

            if (_looter != null)
                return;

            Transform spawnPoint = FindExplicitSpawnPoint();
            Vector3 spawnPosition = spawnPoint != null ? spawnPoint.position : FindLooterSpawnPosition(_player.position);
            Quaternion spawnRotation = spawnPoint != null ? spawnPoint.rotation : _player.rotation;

            _looter = Instantiate(_player.gameObject, spawnPosition, spawnRotation);
            _looter.name = LooterObjectName;

            PrepareLooterClone(_looter);

            _looterAi = _looter.AddComponent<EndLooterFleeAI>();
            _looterAi.Configure(_player, _map, _looterMoveSpeed);
        }

        private void PrepareLooterClone(GameObject looter)
        {
            DisableHumanPlayerComponents(looter);
            ApplySpriteColorMultiplier(looter);

            MapAttentionMarker marker = looter.GetComponent<MapAttentionMarker>();
            if (marker == null)
                marker = looter.AddComponent<MapAttentionMarker>();

            marker.SetColor(_looterMapMarkerColor);
            marker.SetDiameter(_looterMapMarkerDiameter);
            marker.SetVisible(true);
        }

        private static void DisableHumanPlayerComponents(GameObject looter)
        {
            MonoBehaviour[] behaviours = looter.GetComponentsInChildren<MonoBehaviour>(true);
            for (int i = 0; i < behaviours.Length; i++)
            {
                MonoBehaviour behaviour = behaviours[i];
                if (behaviour == null)
                    continue;

                string typeName = behaviour.GetType().Name;
                if (typeName == nameof(PlayerAnimatorDriver)
                    || typeName == "Cave_PlayerController"
                    || typeName == "CavePlayerStageTransition"
                    || typeName == "CavePlayerResetTransition"
                    || typeName == "CavePlayerTriggerTeleporter")
                {
                    behaviour.enabled = false;
                }
            }
        }

        private void ApplySpriteColorMultiplier(GameObject looter)
        {
            SpriteRenderer[] renderers = looter.GetComponentsInChildren<SpriteRenderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                SpriteRenderer spriteRenderer = renderers[i];
                Color color = spriteRenderer.color;
                spriteRenderer.color = new Color(
                    color.r * _looterSpriteColorMultiplier.r,
                    color.g * _looterSpriteColorMultiplier.g,
                    color.b * _looterSpriteColorMultiplier.b,
                    color.a * _looterSpriteColorMultiplier.a);
            }
        }

        private Vector3 FindLooterSpawnPosition(Vector3 playerPosition)
        {
            Vector3 fallback = playerPosition + new Vector3(12f, -8f, 0f);
            if (_map == null || _map.Definition == null || !_map.IsInitialized)
                return fallback;

            Vector3 bestPosition = fallback;
            float bestScore = float.NegativeInfinity;

            for (int i = 1; i <= 160; i++)
            {
                Vector2 uv = new Vector2(Halton(i, 2), Halton(i, 3));
                Vector3 candidate = UvToWorld(_map, uv, playerPosition);
                float distance = PlanarDistance(candidate, playerPosition);
                if (distance < _minimumSpawnDistance || !_map.IsWalkable(candidate))
                    continue;

                float straightLinePenalty = _map.CanTraverseSegment(candidate, playerPosition, 0.35f) ? 0f : 6f;
                float edgePenalty = Mathf.Abs(uv.x - 0.5f) + Mathf.Abs(uv.y - 0.5f);
                float score = distance - straightLinePenalty - edgePenalty;

                if (score <= bestScore)
                    continue;

                bestScore = score;
                bestPosition = candidate;
            }

            return bestScore > float.NegativeInfinity ? bestPosition : fallback;
        }

        private static Transform FindExplicitSpawnPoint()
        {
            GameObject spawnObject = GameObject.Find(SpawnObjectName);
            if (spawnObject != null)
                return spawnObject.transform;

            GameObject[] allObjects = Resources.FindObjectsOfTypeAll<GameObject>();
            for (int i = 0; i < allObjects.Length; i++)
            {
                GameObject candidate = allObjects[i];
                if (candidate == null || !candidate.scene.IsValid())
                    continue;

                if (candidate.name == SpawnObjectName)
                    return candidate.transform;
            }

            return null;
        }

        private IEnumerator ReturnToMenuRoutine()
        {
            if (_transitionStarted)
                yield break;

            _transitionStarted = true;
            Time.timeScale = 1f;

            StopMovement(_player);
            StopMovement(_looter != null ? _looter.transform : null);

            if (_looterAi != null)
                _looterAi.enabled = false;

            DisablePlayerInput(_player);
            DontDestroyOnLoad(gameObject);

            CanvasGroup fade = CreatePersistentFadeOverlay();
            yield return FadeTo(fade, 1f, _fadeOutDuration);

            if (_blackHoldDuration > 0f)
                yield return new WaitForSecondsRealtime(_blackHoldDuration);

            SceneManager.LoadScene(_menuSceneName);
            yield return null;

            yield return FadeTo(fade, 0f, _fadeInDuration);

            if (fade != null)
                Destroy(fade.gameObject);

            Destroy(gameObject);
        }

        private static void DisablePlayerInput(Transform player)
        {
            if (player == null)
                return;

            MonoBehaviour[] behaviours = player.GetComponentsInChildren<MonoBehaviour>(true);
            for (int i = 0; i < behaviours.Length; i++)
            {
                MonoBehaviour behaviour = behaviours[i];
                if (behaviour == null)
                    continue;

                string typeName = behaviour.GetType().Name;
                if (typeName == "Cave_PlayerController")
                    behaviour.enabled = false;
            }
        }

        private static void StopMovement(Transform target)
        {
            if (target == null)
                return;

            Rigidbody2D body = target.GetComponent<Rigidbody2D>();
            if (body == null)
                return;

            body.linearVelocity = Vector2.zero;
            body.angularVelocity = 0f;
        }

        private static CanvasGroup CreatePersistentFadeOverlay()
        {
            GameObject root = new GameObject("End Chase Fade", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(CanvasGroup));
            DontDestroyOnLoad(root);

            Canvas canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = short.MaxValue;

            CanvasScaler scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            CanvasGroup canvasGroup = root.GetComponent<CanvasGroup>();
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = true;
            canvasGroup.interactable = false;

            GameObject imageObject = new GameObject("Black", typeof(RectTransform), typeof(Image));
            imageObject.transform.SetParent(root.transform, false);

            RectTransform rect = imageObject.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = Vector2.zero;

            Image image = imageObject.GetComponent<Image>();
            image.color = Color.black;
            image.raycastTarget = true;

            return canvasGroup;
        }

        private static IEnumerator FadeTo(CanvasGroup canvasGroup, float targetAlpha, float duration)
        {
            if (canvasGroup == null)
                yield break;

            float startAlpha = canvasGroup.alpha;
            if (duration <= 0f)
            {
                canvasGroup.alpha = targetAlpha;
                yield break;
            }

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, Mathf.Clamp01(elapsed / duration));
                yield return null;
            }

            canvasGroup.alpha = targetAlpha;
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

        private static MapDiscoverySystem FindMapForPlayer(Transform player)
        {
            MapDiscoverySystem[] maps = FindObjectsByType<MapDiscoverySystem>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            if (maps.Length == 0)
                return null;

            for (int i = 0; i < maps.Length; i++)
            {
                if (maps[i] != null && player != null && maps[i].TrackedTransform == player)
                    return maps[i];
            }

            for (int i = 0; i < maps.Length; i++)
            {
                if (maps[i] != null && maps[i].name == "MapDiscovery")
                    return maps[i];
            }

            return maps[0];
        }

        private static Vector3 UvToWorld(MapDiscoverySystem map, Vector2 uv, Vector3 referencePosition)
        {
            MapDefinition definition = map.Definition;
            if (definition.FlipWorldX)
                uv.x = 1f - uv.x;

            if (definition.FlipWorldY)
                uv.y = 1f - uv.y;

            Vector2 size = definition.WorldSize;
            Vector2 point = new Vector2((uv.x - 0.5f) * size.x, (uv.y - 0.5f) * size.y);
            Vector3 local = definition.WorldPlane == MapWorldPlane.XY
                ? new Vector3(point.x, point.y, 0f)
                : new Vector3(point.x, 0f, point.y);

            Vector3 world = map.transform.TransformPoint(local);
            if (definition.WorldPlane == MapWorldPlane.XY)
                world.z = referencePosition.z;
            else
                world.y = referencePosition.y;

            return world;
        }

        private static float PlanarDistance(Vector3 a, Vector3 b)
        {
            return Vector2.Distance(new Vector2(a.x, a.y), new Vector2(b.x, b.y));
        }

        private static float Halton(int index, int radix)
        {
            float result = 0f;
            float fraction = 1f / radix;

            while (index > 0)
            {
                result += fraction * (index % radix);
                index /= radix;
                fraction /= radix;
            }

            return result;
        }
    }
}
