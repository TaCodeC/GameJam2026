#pragma warning disable 0649

using System;
using UnityEngine;

namespace GameJam.Gameplay.Minigames
{
    [RequireComponent(typeof(DragDropMinigame))]
    public sealed class BonePlacementDropState : MonoBehaviour, IMinigameStateConsumer
    {
        [Serializable]
        private sealed class PlacedBoneVisual
        {
            [SerializeField] private string _boneId;
            [SerializeField] private GameObject _visual;

            public string BoneId => _boneId;
            public GameObject Visual => _visual;
        }

        [SerializeField] private PlacedBoneVisual[] _placedBoneVisuals = Array.Empty<PlacedBoneVisual>();

        private DragDropMinigame _dragDropMinigame;
        private MinigameObjectState _boundObjectState;

        private void Awake()
        {
            _dragDropMinigame = GetComponent<DragDropMinigame>();
        }

        private void OnEnable()
        {
            if (_dragDropMinigame == null)
            {
                _dragDropMinigame = GetComponent<DragDropMinigame>();
            }

            _dragDropMinigame.Completed.AddListener(HandleCompleted);
            RefreshPlacedBones();
        }

        private void OnDisable()
        {
            if (_dragDropMinigame != null)
            {
                _dragDropMinigame.Completed.RemoveListener(HandleCompleted);
            }
        }

        public void BindState(MinigameObjectState objectState, string minigameId)
        {
            _boundObjectState = objectState;
            RefreshPlacedBones();
        }

        public void RefreshPlacedBones()
        {
            BoneCollectionProgress progress = BoneCollectionProgress.Active;

            for (int i = 0; i < _placedBoneVisuals.Length; i++)
            {
                PlacedBoneVisual placedBone = _placedBoneVisuals[i];
                if (placedBone?.Visual != null)
                {
                    placedBone.Visual.SetActive(progress.IsBoneCollected(placedBone.BoneId));
                }
            }
        }

        private void HandleCompleted()
        {
            if (_boundObjectState != null)
            {
                BoneCollectionProgress.Active.RegisterCompletedBone(_boundObjectState);
            }

            RefreshPlacedBones();
        }
    }
}
