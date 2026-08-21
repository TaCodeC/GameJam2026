using System.Collections;
using GameJam.UI;
using UnityEngine;

namespace GameJam.Gameplay.Cave
{
    [DefaultExecutionOrder(-10000)]
    [DisallowMultipleComponent]
    public sealed class CaveIntroCinematicBootstrap : MonoBehaviour
    {
        [SerializeField] private bool _playOnSceneStart = true;
        [SerializeField, Min(0f)] private float _startDelaySeconds;
        [SerializeField] private ComicCinematicAsset _comicCinematic;
        [SerializeField] private string _comicResourceName = CinematicSequences.CaveOpeningComic;
        [SerializeField] private string[] _cinematicResources = { "Final_C1", "Final_C2", "Final_C3" };

        private ComicCinematicAsset _resolvedComicCinematic;

        private void Awake()
        {
            if (_playOnSceneStart)
            {
                SceneInstructionsFlow.DeferAutomaticStart(gameObject.scene);
                _resolvedComicCinematic = ResolveComicCinematic();

                if (_resolvedComicCinematic != null)
                    ComicCinematicPlayer.Instance.CoverScreenImmediately();
                else
                    CinematicSequencePlayer.Instance.CoverScreenImmediately();
            }
        }

        private IEnumerator Start()
        {
            if (!_playOnSceneStart)
                yield break;

            if (_startDelaySeconds > 0f)
                yield return new WaitForSecondsRealtime(_startDelaySeconds);

            string[] resources = _cinematicResources != null && _cinematicResources.Length > 0
                ? _cinematicResources
                : CinematicSequences.CaveOpening;

            SceneInstructionsFlow instructionsFlow = null;
            void ShowInstructionsUnderFade()
            {
                instructionsFlow ??= SceneInstructionsFlow.BeginDeferred(gameObject.scene);
            }

            _resolvedComicCinematic ??= ResolveComicCinematic();
            if (_resolvedComicCinematic != null)
                yield return ComicCinematicPlayer.Instance.PlayRoutine(_resolvedComicCinematic, string.Empty, ShowInstructionsUnderFade);
            else
                yield return CinematicSequencePlayer.Instance.PlayRoutine(resources, false, string.Empty, ShowInstructionsUnderFade);

            ShowInstructionsUnderFade();

            while (instructionsFlow != null && !instructionsFlow.IsFinished)
                yield return null;
        }

        private ComicCinematicAsset ResolveComicCinematic()
        {
            if (_comicCinematic != null)
                return _comicCinematic;

            return !string.IsNullOrWhiteSpace(_comicResourceName)
                ? Resources.Load<ComicCinematicAsset>(_comicResourceName)
                : null;
        }
    }
}
