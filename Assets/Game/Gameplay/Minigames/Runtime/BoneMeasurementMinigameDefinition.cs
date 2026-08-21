#pragma warning disable 0649

using UnityEngine;

namespace GameJam.Gameplay.Minigames
{
    [DisallowMultipleComponent]
    public sealed class BoneMeasurementMinigameDefinition : MonoBehaviour
    {
        [SerializeField] private string _recordId = "bone_measurement";
        [SerializeField] private string _displayName = "Hueso";
        [SerializeField] private string _measurementTitle;
        [SerializeField] private string _notebookTitle;
        [SerializeField] private MeasurementKind _measurementKind = MeasurementKind.Length;
        [SerializeField] private MeasurementToolType _toolType = MeasurementToolType.LinearTape;
        [SerializeField] private string _customMeasurementLabel;
        [SerializeField, TextArea] private string _prompt = "Mide el hueso y registra el resultado en la libreta.";
        [SerializeField] private MeasurementAnswerKind _answerKind = MeasurementAnswerKind.Number;
        [SerializeField] private float _expectedNumber = 10f;
        [SerializeField] private float _numberTolerance = 0.5f;
        [SerializeField] private string _expectedText;
        [SerializeField] private bool _caseSensitive;
        [SerializeField] private string _unit = "cm";
        [SerializeField] private Sprite _measurementBackgroundSprite;
        [SerializeField] private Sprite _measuredBoneSprite;
        [SerializeField] private Sprite _notebookSprite;

        public string RecordId => string.IsNullOrWhiteSpace(_recordId) ? gameObject.name : _recordId.Trim();
        public string DisplayName => string.IsNullOrWhiteSpace(_displayName) ? RecordId : _displayName.Trim();
        public Sprite MeasurementBackgroundSprite => _measurementBackgroundSprite;
        public Sprite MeasuredBoneSprite => _measuredBoneSprite;
        public Sprite NotebookSprite => _notebookSprite;

        public string MeasurementTitle => string.IsNullOrWhiteSpace(_measurementTitle)
            ? $"Medicion: {DisplayName}"
            : _measurementTitle.Trim();

        public string NotebookTitle => string.IsNullOrWhiteSpace(_notebookTitle)
            ? $"Libreta: {DisplayName}"
            : _notebookTitle.Trim();

        public MeasurementMinigame.MeasurementQuestion CreateQuestion()
        {
            return new MeasurementMinigame.MeasurementQuestion(
                RecordId,
                _measurementKind,
                _toolType,
                _customMeasurementLabel,
                GetPrompt(),
                _answerKind,
                _expectedNumber,
                _numberTolerance,
                _expectedText,
                _caseSensitive,
                _unit);
        }

        private void OnValidate()
        {
            _numberTolerance = Mathf.Max(0f, _numberTolerance);
        }

        private string GetPrompt()
        {
            if (!string.IsNullOrWhiteSpace(_prompt))
            {
                return _prompt.Trim();
            }

            return $"Mide {DisplayName} y registra el resultado en la libreta.";
        }
    }
}
