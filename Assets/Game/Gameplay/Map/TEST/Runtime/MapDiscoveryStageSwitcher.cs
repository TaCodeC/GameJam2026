using System;
using UnityEngine;

namespace GameJam.Gameplay.Map
{
    [DisallowMultipleComponent]
    public sealed class MapDiscoveryStageSwitcher : MonoBehaviour
    {
        [Serializable]
        private struct Stage
        {
            public string Label;
            public MapDefinition Definition;
            public bool UseExplicitWorldBounds;
            public Vector2 WorldMin;
            public Vector2 WorldMax;
            public Transform WorldOrigin;
            public Renderer TargetRenderer;
            public Texture WorldTextureOverride;
            public Texture HudTextureOverride;
            public Renderer[] EnableRenderers;
            public Renderer[] DisableRenderers;
            public GameObject[] EnableObjects;
            public GameObject[] DisableObjects;
        }

        [Header("Targets")]
        [SerializeField] private MapDiscoverySystem _discovery;
        [SerializeField] private MapDiscoveryView _worldView;
        [SerializeField] private MapDebugHud _debugHud;

        [Header("Stages")]
        [SerializeField] private Stage _initialStage;
        [SerializeField] private Stage _linternaStage;
        [SerializeField] private bool _applyInitialStageOnStart = true;
        [SerializeField] private bool _resetDiscoveryWhenSwitchingStage = true;

        private StageId _currentStage = StageId.None;

        private enum StageId
        {
            None,
            Initial,
            Linterna
        }

        private void Awake()
        {
            ResolveTargets();
        }

        private void Start()
        {
            if (_applyInitialStageOnStart)
            {
                ApplyInitialStage();
            }
        }

        public void ApplyInitialStage()
        {
            ApplyStage(_initialStage, StageId.Initial);
        }

        public void ApplyLinternaStage()
        {
            ApplyStage(_linternaStage, StageId.Linterna);
        }

        private void ApplyStage(Stage stage, StageId stageId)
        {
            ResolveTargets();

            if (_discovery == null || stage.Definition == null)
            {
                Debug.LogWarning($"{nameof(MapDiscoveryStageSwitcher)} needs a discovery system and a map definition for stage '{stage.Label}'.", this);
                return;
            }

            bool stageChanged = _currentStage != stageId;
            _currentStage = stageId;

            ApplyWorldPose(stage);
            _discovery.SetDefinition(stage.Definition);

            if (_resetDiscoveryWhenSwitchingStage && stageChanged)
            {
                _discovery.ResetDiscovery();
            }

            if (_worldView != null)
            {
                _worldView.Configure(_discovery, null, stage.TargetRenderer, stage.WorldTextureOverride);
            }

            if (_debugHud != null)
            {
                _debugHud.Configure(_discovery);
                _debugHud.SetMapTextureOverride(stage.HudTextureOverride != null ? stage.HudTextureOverride : stage.WorldTextureOverride);
            }

            SetRenderersEnabled(stage.EnableRenderers, true);
            SetRenderersEnabled(stage.DisableRenderers, false);
            SetObjectsActive(stage.EnableObjects, true);
            SetObjectsActive(stage.DisableObjects, false);
        }

        private void ResolveTargets()
        {
            if (_discovery == null)
            {
                _discovery = GetComponent<MapDiscoverySystem>();
            }

            if (_worldView == null)
            {
                _worldView = GetComponentInChildren<MapDiscoveryView>(true);
            }

            if (_debugHud == null)
            {
                _debugHud = GetComponent<MapDebugHud>();
            }
        }

        private void ApplyWorldPose(Stage stage)
        {
            if (stage.UseExplicitWorldBounds)
            {
                Vector2 min = Vector2.Min(stage.WorldMin, stage.WorldMax);
                Vector2 max = Vector2.Max(stage.WorldMin, stage.WorldMax);
                float z = _discovery != null ? _discovery.transform.position.z : transform.position.z;
                Vector3 center = new Vector3(
                    (min.x + max.x) * 0.5f,
                    (min.y + max.y) * 0.5f,
                    z);
                _discovery.SetWorldPoseOverride(center, Quaternion.identity);
                return;
            }

            if (stage.WorldOrigin != null)
            {
                _discovery.SetWorldTransformOverride(stage.WorldOrigin);
                return;
            }

            _discovery.ClearWorldPoseOverride();
        }

        private static void SetObjectsActive(GameObject[] objects, bool active)
        {
            if (objects == null)
            {
                return;
            }

            for (int i = 0; i < objects.Length; i++)
            {
                GameObject target = objects[i];
                if (target != null && target.activeSelf != active)
                {
                    target.SetActive(active);
                }
            }
        }

        private static void SetRenderersEnabled(Renderer[] renderers, bool enabled)
        {
            if (renderers == null)
            {
                return;
            }

            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer target = renderers[i];
                if (target != null && target.enabled != enabled)
                {
                    target.enabled = enabled;
                }
            }
        }
    }
}
