#pragma warning disable 0649

using System;
using System.Globalization;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace GameJam.Gameplay.Minigames
{
    public enum MeasurementKind
    {
        Length,
        Width,
        Height,
        Depth,
        Diameter,
        Circumference,
        Angle,
        Count,
        Custom
    }

    public enum MeasurementAnswerKind
    {
        Number,
        Text
    }

    public sealed class MeasurementMinigame : MonoBehaviour, IMinigameStateConsumer
    {
        [Serializable]
        public sealed class MeasurementQuestion
        {
            [SerializeField] private string _id = "measurement";
            [SerializeField] private MeasurementKind _measurementKind = MeasurementKind.Length;
            [SerializeField] private MeasurementToolType _toolType = MeasurementToolType.LinearTape;
            [SerializeField] private string _customMeasurementLabel;
            [SerializeField, TextArea] private string _prompt = "Mide el objeto y escribe el resultado.";
            [SerializeField] private MeasurementAnswerKind _answerKind = MeasurementAnswerKind.Number;
            [SerializeField] private float _expectedNumber = 10f;
            [SerializeField] private float _numberTolerance = 0.25f;
            [SerializeField] private string _expectedText = "";
            [SerializeField] private bool _caseSensitive;
            [SerializeField] private string _unit = "cm";
            [SerializeField] private TMP_Text _promptLabel;
            [SerializeField] private TMP_Text _unitLabel;
            [SerializeField] private TMP_InputField _answerInput;
            [SerializeField] private MeasurementToolBase _measurementTool;

            public string Id => _id;
            public MeasurementToolType ToolType => _toolType;
            public MeasurementAnswerKind AnswerKind => _answerKind;
            public float ExpectedNumber => _expectedNumber;
            public float NumberTolerance => Mathf.Max(0f, _numberTolerance);
            public string ExpectedText => _expectedText;
            public string Unit => _unit;
            public TMP_Text PromptLabel => _promptLabel;
            public TMP_Text UnitLabel => _unitLabel;
            public TMP_InputField AnswerInput => _answerInput;
            public MeasurementToolBase MeasurementTool => _measurementTool;

            public MeasurementQuestion()
            {
            }

            public MeasurementQuestion(
                string id,
                MeasurementKind measurementKind,
                MeasurementToolType toolType,
                string customMeasurementLabel,
                string prompt,
                MeasurementAnswerKind answerKind,
                float expectedNumber,
                float numberTolerance,
                string expectedText,
                bool caseSensitive,
                string unit)
            {
                _id = string.IsNullOrWhiteSpace(id) ? "measurement" : id.Trim();
                _measurementKind = measurementKind;
                _toolType = toolType;
                _customMeasurementLabel = customMeasurementLabel ?? string.Empty;
                _prompt = prompt ?? string.Empty;
                _answerKind = answerKind;
                _expectedNumber = expectedNumber;
                _numberTolerance = Mathf.Max(0f, numberTolerance);
                _expectedText = expectedText ?? string.Empty;
                _caseSensitive = caseSensitive;
                _unit = string.IsNullOrWhiteSpace(unit) ? "cm" : unit.Trim();
            }

            public string MeasurementLabel
            {
                get
                {
                    if (_measurementKind == MeasurementKind.Custom && !string.IsNullOrWhiteSpace(_customMeasurementLabel))
                    {
                        return _customMeasurementLabel;
                    }

                    return _measurementKind.ToString();
                }
            }

            public string Prompt => string.IsNullOrWhiteSpace(_prompt)
                ? $"Toma la medicion: {MeasurementLabel}."
                : _prompt;

            public bool IsCorrect(string input, out string expected)
            {
                if (_answerKind == MeasurementAnswerKind.Number)
                {
                    expected = $"{_expectedNumber} {_unit}".Trim();

                    if (!TryParseFlexibleNumber(input, out float parsedNumber))
                    {
                        return false;
                    }

                    return Mathf.Abs(parsedNumber - _expectedNumber) <= NumberTolerance;
                }

                expected = _expectedText;
                StringComparison comparison = _caseSensitive
                    ? StringComparison.Ordinal
                    : StringComparison.OrdinalIgnoreCase;

                return string.Equals(
                    NormalizeText(input),
                    NormalizeText(_expectedText),
                    comparison);
            }

            private static bool TryParseFlexibleNumber(string input, out float value)
            {
                value = 0f;

                if (string.IsNullOrWhiteSpace(input))
                {
                    return false;
                }

                Match match = Regex.Match(input, @"[-+]?\d+(?:[\.,]\d+)?");
                if (!match.Success)
                {
                    return false;
                }

                string normalized = match.Value.Replace(',', '.');
                return float.TryParse(
                    normalized,
                    NumberStyles.Float,
                    CultureInfo.InvariantCulture,
                    out value);
            }

            private static string NormalizeText(string text)
            {
                return Regex.Replace(text ?? string.Empty, @"\s+", " ").Trim();
            }
        }

        [SerializeField] private GameObject _measurementPageRoot;
        [SerializeField] private GameObject _answerPageRoot;
        [SerializeField] private TMP_Text _sharedPromptLabel;
        [SerializeField] private TMP_Text _sharedAnswerPromptLabel;
        [SerializeField] private TMP_Text _sharedUnitLabel;
        [SerializeField] private TMP_InputField _sharedAnswerInput;
        [SerializeField] private Image _measurementBackgroundImage;
        [SerializeField] private Image _measuredBoneImage;
        [SerializeField] private Image _notebookImage;
        [SerializeField] private TMP_Text _titleLabel;
        [SerializeField] private TMP_Text _notebookTitleLabel;
        [SerializeField] private MeasurementToolSwitcher _toolSwitcher;
        [SerializeField] private MeasurementToolBase _sharedMeasurementTool;
        [SerializeField] private Button _openNotebookButton;
        [SerializeField] private Button _returnToMeasurementButton;
        [SerializeField] private Button _submitButton;
        [SerializeField] private MeasurementQuestion[] _questions = Array.Empty<MeasurementQuestion>();
        [SerializeField] private bool _resetOnEnable = true;
        [SerializeField] private bool _advanceAfterCorrectAnswer = true;
        [SerializeField] private bool _showMeasurementPageOnEnable = true;
        [SerializeField] private bool _useBoundObjectDefinition;
        [SerializeField] private UnityEvent _completed = new();
        [SerializeField] private UnityEvent<string> _incorrectAnswer = new();

        private bool[] _answeredQuestions = Array.Empty<bool>();
        private MeasurementQuestion[] _configuredQuestions = Array.Empty<MeasurementQuestion>();
        private int _currentQuestionIndex;
        private bool _submitButtonBound;
        private bool _pageButtonsBound;
        private bool _configuredQuestionsCached;
        private MinigameObjectState _boundObjectState;
        private string _boundMinigameId = "measurement";

        public int CurrentQuestionIndex => _currentQuestionIndex;

        private void Awake()
        {
            CacheConfiguredQuestions();
            EnsureAnsweredArray();
        }

        private void OnEnable()
        {
            CacheConfiguredQuestions();
            EnsureAnsweredArray();
            BindSubmitButton();
            BindPageButtons();

            if (_resetOnEnable)
            {
                ResetGame();
            }
            else
            {
                RefreshQuestionView();
            }

            if (_showMeasurementPageOnEnable)
            {
                ShowMeasurementPage();
            }
        }

        private void OnDisable()
        {
            UnbindSubmitButton();
            UnbindPageButtons();
        }

        public void ResetGame()
        {
            EnsureAnsweredArray();

            for (int i = 0; i < _answeredQuestions.Length; i++)
            {
                _answeredQuestions[i] = false;
            }

            _currentQuestionIndex = 0;

            if (!HasQuestions())
            {
                Debug.LogWarning("[Measurement] Reset requested, but there are no measurement questions configured.", this);
                return;
            }

            TMP_InputField input = GetAnswerInput(GetCurrentQuestion());
            if (input != null)
            {
                input.text = string.Empty;
            }

            RefreshQuestionView();
            ShowMeasurementPage();
            SetBoundState(MinigameResolutionState.InProgress);
            Debug.Log("[Measurement] Mini game reset.", this);
        }

        public void BindState(MinigameObjectState objectState, string minigameId)
        {
            _boundObjectState = objectState;
            _boundMinigameId = string.IsNullOrWhiteSpace(minigameId) ? "measurement" : minigameId;

            if (_useBoundObjectDefinition)
            {
                ApplyObjectDefinition(objectState);
            }
            else
            {
                CacheConfiguredQuestions();
                _questions = _configuredQuestions;
                _currentQuestionIndex = 0;
                EnsureAnsweredArray();
            }

            if (_resetOnEnable)
            {
                ResetGame();
            }
            else
            {
                EnsureAnsweredArray();
                RefreshQuestionView();
                SetBoundState(MinigameResolutionState.InProgress);
            }
        }

        public void ValidateCurrentQuestion()
        {
            if (!HasQuestions())
            {
                Debug.LogWarning("[Measurement] There are no measurement questions configured.", this);
                return;
            }

            MeasurementQuestion question = GetCurrentQuestion();
            TMP_InputField input = GetAnswerInput(question);
            string answer = input != null ? input.text : string.Empty;

            if (!question.IsCorrect(answer, out string expected))
            {
                Debug.Log($"[Measurement] Incorrect answer for '{question.Id}'. Expected: {expected}. Answer: {answer}", this);
                RecordBoundAnswer(question, answer, false, expected);
                _incorrectAnswer.Invoke(question.Id);
                return;
            }

            _answeredQuestions[_currentQuestionIndex] = true;
            RecordBoundAnswer(question, answer, true, expected);
            Debug.Log($"[Measurement] Correct answer for '{question.Id}'.", this);

            if (IsComplete())
            {
                Debug.Log("[Measurement] Mini game completed.", this);
                SetBoundState(MinigameResolutionState.Completed);
                _completed.Invoke();
                return;
            }

            if (_advanceAfterCorrectAnswer)
            {
                AdvanceToNextUnansweredQuestion();
            }

            RefreshQuestionView();
            ShowMeasurementPage();
        }

        public void SelectQuestion(int questionIndex)
        {
            if (questionIndex < 0 || questionIndex >= _questions.Length)
            {
                Debug.LogWarning($"[Measurement] Invalid question index {questionIndex}.", this);
                return;
            }

            _currentQuestionIndex = questionIndex;
            RefreshQuestionView();
            ShowMeasurementPage();
        }

        public void ShowMeasurementPage()
        {
            SetPageActive(_measurementPageRoot, true);
            SetPageActive(_answerPageRoot, false);
        }

        public void ShowAnswerPage()
        {
            if (_answerPageRoot == null)
            {
                Debug.LogWarning("[Measurement] Cannot show answer notebook because no answer page root is assigned.", this);
                return;
            }

            SetPageActive(_measurementPageRoot, false);
            SetPageActive(_answerPageRoot, true);

            TMP_InputField input = HasQuestions() ? GetAnswerInput(GetCurrentQuestion()) : _sharedAnswerInput;
            if (input != null)
            {
                input.ActivateInputField();
            }
        }

        private void RefreshQuestionView()
        {
            if (!HasQuestions())
            {
                return;
            }

            MeasurementQuestion question = GetCurrentQuestion();
            TMP_Text promptLabel = question.PromptLabel != null ? question.PromptLabel : _sharedPromptLabel;
            TMP_Text answerPromptLabel = _sharedAnswerPromptLabel;
            TMP_Text unitLabel = question.UnitLabel != null ? question.UnitLabel : _sharedUnitLabel;
            TMP_InputField answerInput = GetAnswerInput(question);
            MeasurementToolBase measurementTool = question.MeasurementTool != null ? question.MeasurementTool : _sharedMeasurementTool;

            if (promptLabel != null)
            {
                promptLabel.text = question.Prompt;
            }

            if (answerPromptLabel != null)
            {
                answerPromptLabel.text = question.Prompt;
            }

            if (unitLabel != null)
            {
                unitLabel.text = question.Unit;
            }

            if (answerInput != null)
            {
                answerInput.contentType = question.AnswerKind == MeasurementAnswerKind.Number
                    ? TMP_InputField.ContentType.DecimalNumber
                    : TMP_InputField.ContentType.Standard;
                answerInput.text = string.Empty;
            }

            if (_toolSwitcher != null)
            {
                _toolSwitcher.SelectTool(question.ToolType);
                _toolSwitcher.SetCurrentUnit(question.Unit);
            }
            else if (measurementTool != null)
            {
                measurementTool.SetUnit(question.Unit);
                measurementTool.Refresh();
            }
        }

        private void ApplyObjectDefinition(MinigameObjectState objectState)
        {
            CacheConfiguredQuestions();

            BoneMeasurementMinigameDefinition definition = objectState != null
                ? objectState.GetComponent<BoneMeasurementMinigameDefinition>()
                : null;

            if (definition != null)
            {
                _questions = new[] { definition.CreateQuestion() };
                ApplyVisualDefinition(definition);
            }
            else
            {
                _questions = _configuredQuestions;
                ApplyVisualDefinition(null);
            }

            _currentQuestionIndex = 0;
            EnsureAnsweredArray();
        }

        private void ApplyVisualDefinition(BoneMeasurementMinigameDefinition definition)
        {
            ApplyImage(
                _measurementBackgroundImage,
                definition != null ? definition.MeasurementBackgroundSprite : null,
                definition != null && definition.MeasurementBackgroundSprite != null,
                false);

            ApplyImage(
                _measuredBoneImage,
                definition != null ? definition.MeasuredBoneSprite : null,
                definition != null && definition.MeasuredBoneSprite != null,
                true);

            ApplyImage(
                _notebookImage,
                definition != null ? definition.NotebookSprite : null,
                true,
                true);

            if (_titleLabel != null)
            {
                _titleLabel.text = definition != null ? definition.MeasurementTitle : "Medicion in situ";
            }

            if (_notebookTitleLabel != null)
            {
                _notebookTitleLabel.text = definition != null ? definition.NotebookTitle : "Libreta de campo";
            }
        }

        private static void ApplyImage(Image image, Sprite sprite, bool enabled, bool preserveAspect)
        {
            if (image == null)
            {
                return;
            }

            image.sprite = sprite;
            image.enabled = enabled;
            image.preserveAspect = preserveAspect;
            image.color = Color.white;
        }

        private void RecordBoundAnswer(MeasurementQuestion question, string answer, bool isCorrect, string expectedAnswer)
        {
            if (_boundObjectState == null)
            {
                return;
            }

            _boundObjectState.RecordAnswer(
                _boundMinigameId,
                question != null ? question.Id : "measurement",
                answer,
                isCorrect,
                expectedAnswer);
        }

        private void SetBoundState(MinigameResolutionState state)
        {
            if (_boundObjectState != null)
            {
                if (state == MinigameResolutionState.InProgress
                    && _boundObjectState.GetResolutionState(_boundMinigameId) == MinigameResolutionState.Completed)
                {
                    return;
                }

                _boundObjectState.SetResolutionState(_boundMinigameId, state);
            }
        }

        private MeasurementQuestion GetCurrentQuestion()
        {
            return _questions[Mathf.Clamp(_currentQuestionIndex, 0, _questions.Length - 1)];
        }

        private TMP_InputField GetAnswerInput(MeasurementQuestion question)
        {
            return question != null && question.AnswerInput != null ? question.AnswerInput : _sharedAnswerInput;
        }

        private void AdvanceToNextUnansweredQuestion()
        {
            for (int i = 0; i < _answeredQuestions.Length; i++)
            {
                int nextIndex = (_currentQuestionIndex + 1 + i) % _answeredQuestions.Length;
                if (!_answeredQuestions[nextIndex])
                {
                    _currentQuestionIndex = nextIndex;
                    return;
                }
            }
        }

        private bool IsComplete()
        {
            if (_answeredQuestions.Length == 0)
            {
                return false;
            }

            for (int i = 0; i < _answeredQuestions.Length; i++)
            {
                if (!_answeredQuestions[i])
                {
                    return false;
                }
            }

            return true;
        }

        private bool HasQuestions()
        {
            return _questions != null && _questions.Length > 0;
        }

        private void EnsureAnsweredArray()
        {
            int length = _questions != null ? _questions.Length : 0;
            if (_answeredQuestions.Length != length)
            {
                _answeredQuestions = new bool[length];
            }
        }

        private void CacheConfiguredQuestions()
        {
            if (_configuredQuestionsCached)
            {
                return;
            }

            _configuredQuestions = _questions ?? Array.Empty<MeasurementQuestion>();
            _configuredQuestionsCached = true;
        }

        private void BindSubmitButton()
        {
            if (_submitButtonBound || _submitButton == null)
            {
                return;
            }

            _submitButton.onClick.AddListener(ValidateCurrentQuestion);
            _submitButtonBound = true;
        }

        private void UnbindSubmitButton()
        {
            if (!_submitButtonBound || _submitButton == null)
            {
                return;
            }

            _submitButton.onClick.RemoveListener(ValidateCurrentQuestion);
            _submitButtonBound = false;
        }

        private void BindPageButtons()
        {
            if (_pageButtonsBound)
            {
                return;
            }

            if (_openNotebookButton != null)
            {
                _openNotebookButton.onClick.AddListener(ShowAnswerPage);
            }

            if (_returnToMeasurementButton != null)
            {
                _returnToMeasurementButton.onClick.AddListener(ShowMeasurementPage);
            }

            _pageButtonsBound = true;
        }

        private void UnbindPageButtons()
        {
            if (!_pageButtonsBound)
            {
                return;
            }

            if (_openNotebookButton != null)
            {
                _openNotebookButton.onClick.RemoveListener(ShowAnswerPage);
            }

            if (_returnToMeasurementButton != null)
            {
                _returnToMeasurementButton.onClick.RemoveListener(ShowMeasurementPage);
            }

            _pageButtonsBound = false;
        }

        private static void SetPageActive(GameObject pageRoot, bool active)
        {
            if (pageRoot != null)
            {
                pageRoot.SetActive(active);
            }
        }
    }
}
