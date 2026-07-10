using System.Collections;
using System.Collections.Generic;
using GameJam.Gameplay.Cave;
using GameJam.Gameplay.Map;
using GameJam.UI;
using TMPro;
using UnityEngine;

namespace GameJam.Gameplay.Minigames
{
    [DisallowMultipleComponent]
    public sealed class BoneCollectionProgress : MonoBehaviour
    {
        [SerializeField, Min(1)] private int _requiredBones = 9;
        [SerializeField] private TMP_Text _bonesCounter;
        [SerializeField] private string _bonesCounterObjectName = "BonesCounter";
        [SerializeField] private string _rootsObjectName = "Raices";
        [SerializeField] private Color _rootsMarkerColor = new Color(1f, 0.82f, 0.16f, 1f);
        [SerializeField] private bool _playAllBonesCinematic = true;
        [SerializeField] private string _allBonesMapMessage = "Mira el mapa!";
        [SerializeField] private bool _showAllBonesMapMessage = true;

        private static BoneCollectionProgress _active;

        private readonly HashSet<int> _completedObjects = new();
        private bool _allBonesSequenceStarted;
        private int _completedBones;

        public static BoneCollectionProgress Active
        {
            get
            {
                if (_active != null)
                    return _active;

                _active = FindFirstObjectByType<BoneCollectionProgress>(FindObjectsInactive.Include);
                if (_active != null)
                    return _active;

                GameObject progressObject = new GameObject("Bone Collection Progress");
                _active = progressObject.AddComponent<BoneCollectionProgress>();
                return _active;
            }
        }

        public int CompletedBones => _completedBones;
        public int RequiredBones => _requiredBones;
        public bool HasAllBones => _completedBones >= _requiredBones;

        private void Awake()
        {
            if (_active != null && _active != this)
            {
                Destroy(gameObject);
                return;
            }

            _active = this;
            ResolveBonesCounter();
            RefreshCounter();
        }

        private void OnEnable()
        {
            _active = this;
            ResolveBonesCounter();
            RefreshCounter();
        }

        private void OnDisable()
        {
            if (_active == this)
                _active = null;
        }

        public void RegisterCompletedBone(MinigameObjectState objectState)
        {
            int key = objectState != null ? objectState.GetInstanceID() : _completedObjects.Count + 1;
            if (!_completedObjects.Add(key))
                return;

            _completedBones++;
            RefreshCounter();

            if (HasAllBones && !_allBonesSequenceStarted)
                StartCoroutine(AllBonesCollectedRoutine());
        }

        private IEnumerator AllBonesCollectedRoutine()
        {
            _allBonesSequenceStarted = true;

            if (_playAllBonesCinematic)
            {
                ComicCinematicAsset comicCinematic = Resources.Load<ComicCinematicAsset>(CinematicSequences.AllBonesCollectedComic);
                if (comicCinematic != null)
                    yield return ComicCinematicPlayer.Instance.PlayRoutine(comicCinematic);
                else
                    yield return CinematicSequencePlayer.Instance.PlayRoutine(CinematicSequences.AllBonesCollected);
            }

            UnlockRoots();
            ShowAllBonesMapMessage();
        }

        private void UnlockRoots()
        {
            GameObject roots = FindRootsObject();
            if (roots == null)
            {
                Debug.LogWarning($"[Bones] No se encontro un objeto llamado '{_rootsObjectName}' para activar las raices.", this);
                return;
            }

            SpriteGlowPulse glow = roots.GetComponent<SpriteGlowPulse>();
            if (glow == null)
                glow = roots.AddComponent<SpriteGlowPulse>();

            glow.SetGlowing(true);

            MapAttentionMarker marker = roots.GetComponent<MapAttentionMarker>();
            if (marker == null)
                marker = roots.AddComponent<MapAttentionMarker>();

            marker.SetColor(_rootsMarkerColor);
            marker.SetVisible(true);

            CaveRootsPortalTrigger portal = roots.GetComponent<CaveRootsPortalTrigger>();
            if (portal == null)
                portal = roots.AddComponent<CaveRootsPortalTrigger>();

            if (portal == null)
            {
                Debug.LogWarning("[Bones] No se pudo crear el trigger de portal en las raices.", this);
                return;
            }

            portal.SetUnlocked(true);
        }

        private void ShowAllBonesMapMessage()
        {
            if (!_showAllBonesMapMessage)
                return;

            TimedCanvasFader.ShowSceneHintMessage(_allBonesMapMessage);
        }

        private GameObject FindRootsObject()
        {
            GameObject roots = GameObject.Find(_rootsObjectName);
            if (roots != null)
                return roots;

            GameObject[] allObjects = Resources.FindObjectsOfTypeAll<GameObject>();
            for (int i = 0; i < allObjects.Length; i++)
            {
                GameObject candidate = allObjects[i];
                if (candidate == null || !candidate.scene.IsValid())
                    continue;

                if (string.Equals(candidate.name, _rootsObjectName, System.StringComparison.OrdinalIgnoreCase))
                    return candidate;
            }

            return null;
        }

        private void ResolveBonesCounter()
        {
            if (_bonesCounter != null)
                return;

            GameObject counterObject = GameObject.Find(_bonesCounterObjectName);
            if (counterObject != null)
                _bonesCounter = counterObject.GetComponent<TMP_Text>();
        }

        private void RefreshCounter()
        {
            ResolveBonesCounter();
            if (_bonesCounter != null)
                _bonesCounter.text = _completedBones.ToString();
        }
    }
}
