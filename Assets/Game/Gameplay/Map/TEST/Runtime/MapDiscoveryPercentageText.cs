using System.Globalization;
using TMPro;
using UnityEngine;

namespace GameJam.Gameplay.Map
{
    [DisallowMultipleComponent]
    public sealed class MapDiscoveryPercentageText : MonoBehaviour
    {
        [SerializeField] private MapDiscoverySystem _discovery;
        [SerializeField] private TMP_Text _targetText;
        [SerializeField] private string _format = "{0:0.0}%";
        [SerializeField] private string _fallbackText = "0.0%";

        private void Awake()
        {
            ResolveReferences();
        }

        private void OnEnable()
        {
            ResolveReferences();
            Subscribe();
            RefreshText();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        private void OnDestroy()
        {
            Unsubscribe();
        }

        private void ResolveReferences()
        {
            if (_targetText == null)
            {
                _targetText = GetComponent<TMP_Text>();
            }

            if (_discovery == null)
            {
                _discovery = FindFirstObjectByType<MapDiscoverySystem>();
            }
        }

        private void Subscribe()
        {
            if (_discovery == null)
            {
                return;
            }

            _discovery.MapChanged -= RefreshText;
            _discovery.DiscoveryChanged -= RefreshText;
            _discovery.MapChanged += RefreshText;
            _discovery.DiscoveryChanged += RefreshText;
        }

        private void Unsubscribe()
        {
            if (_discovery == null)
            {
                return;
            }

            _discovery.MapChanged -= RefreshText;
            _discovery.DiscoveryChanged -= RefreshText;
        }

        private void RefreshText()
        {
            if (_targetText == null)
            {
                return;
            }

            if (_discovery == null || !_discovery.IsInitialized)
            {
                _targetText.text = _fallbackText;
                return;
            }

            float percent = _discovery.DiscoveredFraction * 100f;
            _targetText.text = string.Format(CultureInfo.InvariantCulture, _format, percent);
        }
    }
}
