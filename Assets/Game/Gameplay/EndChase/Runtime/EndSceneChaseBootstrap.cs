using System.Collections;
using GameJam.Audio;
using GameJam.Gameplay.Map;
using GameJam.Player;
using GameJam.Player.Cave;
using GameJam.UI;
using UnityEngine;
using UnityEngine.SceneManagement;

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
        [SerializeField] private string _creditsSceneName = "Creditos";

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
            yield return WaitForInstructionsToFinish();
            SceneMusicController.Active.PlayEndLevelMusic();
            InitializeChase();
        }

        private void Update()
        {
            if (_transitionStarted || _player == null || _looter == null)
                return;

            if (PlanarDistance(_player.position, _looter.transform.position) <= _captureDistance)
                StartCoroutine(ReturnToCreditsRoutine());
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

            ApplyEndLinternaState(_player);

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

        private IEnumerator WaitForInstructionsToFinish()
        {
            while (HasRunningInstructions())
                yield return null;
        }

        private bool HasRunningInstructions()
        {
            Scene scene = gameObject.scene;
            SceneInstructionsFlow[] flows = FindObjectsByType<SceneInstructionsFlow>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < flows.Length; i++)
            {
                SceneInstructionsFlow flow = flows[i];
                if (flow != null && flow.gameObject.scene == scene && !flow.IsFinished)
                    return true;
            }

            GameObject[] objects = Resources.FindObjectsOfTypeAll<GameObject>();
            for (int i = 0; i < objects.Length; i++)
            {
                GameObject candidate = objects[i];
                if (candidate != null
                    && candidate.scene == scene
                    && candidate.name == "Instructions_First"
                    && candidate.activeInHierarchy)
                {
                    return true;
                }
            }

            return false;
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

        private static void ApplyEndLinternaState(Transform player)
        {
            if (player == null)
                return;

            CavePlayerStageTransition transition = player.GetComponent<CavePlayerStageTransition>();
            if (transition == null)
                transition = player.GetComponentInChildren<CavePlayerStageTransition>(true);

            if (transition != null)
            {
                transition.ApplyTransitionInstantly(null);
                return;
            }

            CavePlayerSkinController skinController = player.GetComponent<CavePlayerSkinController>();
            if (skinController == null)
                skinController = player.GetComponentInChildren<CavePlayerSkinController>(true);

            if (skinController != null)
                skinController.SetLinterna();

            SetChildActive(player, "SpotLight_Turbina", false);
            SetChildActive(player, "SpotLight_Linterna", true);
        }

        private static void SetChildActive(Transform root, string childName, bool active)
        {
            Transform[] children = root.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < children.Length; i++)
            {
                Transform child = children[i];
                if (child != null && child.name == childName)
                    child.gameObject.SetActive(active);
            }
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

        private IEnumerator ReturnToCreditsRoutine()
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

            ComicCinematicAsset comicCinematic = Resources.Load<ComicCinematicAsset>(CinematicSequences.EndFinaleComic);
            if (comicCinematic != null)
                yield return ComicCinematicPlayer.Instance.PlayRoutine(comicCinematic, _creditsSceneName);
            else
                yield return CinematicSequencePlayer.Instance.PlayRoutine(
                    CinematicSequences.EndFinale,
                    false,
                    _creditsSceneName);

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
