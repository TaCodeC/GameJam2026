using UnityEngine;

namespace DynControls
{
    public sealed class MobileControlsManager : MonoBehaviour
    {
        [SerializeField] private GameObject _mobileControlsRoot;
        [SerializeField] private bool _forceEnableInEditor;

        public bool IsVisible =>
            _mobileControlsRoot != null && _mobileControlsRoot.activeSelf;

        private void Awake()
        {
            RefreshVisibility();
        }

        public void RefreshVisibility()
        {
            if (_mobileControlsRoot == null)
            {
                Debug.LogWarning(
                    "[DynControls] Assign the root object that contains the mobile controls.",
                    this);
                return;
            }

            _mobileControlsRoot.SetActive(ShouldShowControls());
        }

        private bool ShouldShowControls()
        {
#if UNITY_EDITOR
            return _forceEnableInEditor;
#else
            return MobileInputDetector.RequiresMobileControls();
#endif
        }
    }
}
