using UnityEngine;

namespace GameJam.UI
{
    [DisallowMultipleComponent]
    public sealed class ComicCinematicTrigger : MonoBehaviour
    {
        [SerializeField] private ComicCinematicAsset _cinematic;
        [SerializeField] private string _resourceName;
        [SerializeField] private string _nextSceneName;
        [SerializeField] private bool _playOnStart;

        private void Start()
        {
            if (_playOnStart)
                Play();
        }

        public void Play()
        {
            if (_cinematic != null)
            {
                ComicCinematicPlayer.Instance.Play(_cinematic, _nextSceneName);
                return;
            }

            if (!string.IsNullOrWhiteSpace(_resourceName))
                ComicCinematicPlayer.Instance.PlayResource(_resourceName, _nextSceneName);
        }
    }
}
