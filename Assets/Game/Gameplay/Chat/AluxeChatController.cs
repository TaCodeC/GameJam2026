using System;
using GameJam.Creatures;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace GameJam.Gameplay.Chat
{
    [DisallowMultipleComponent]
    public sealed class AluxeChatController : MonoBehaviour
    {
        private enum Speaker
        {
            Chaak,
            Zaazil
        }

        private readonly struct DialogueLine
        {
            public DialogueLine(Speaker speaker, string text)
            {
                Speaker = speaker;
                Text = text;
            }

            public Speaker Speaker { get; }
            public string Text { get; }
        }

        private static readonly DialogueLine[] Dialogue =
        {
            new DialogueLine(Speaker.Chaak, "Está prohibido el paso a partir de este punto."),
            new DialogueLine(Speaker.Zaazil, "¿Qué eres y por qué no me dejas pasar? :0"),
            new DialogueLine(Speaker.Chaak, "Soy Cháak, Alux guardián de este cenote."),
            new DialogueLine(Speaker.Chaak, "Mi misión es ahuyentar a todo aquel que intente pasar y hacer daño a este recinto sagrado, así que te pido de la manera más pacífica posible que te retires."),
            new DialogueLine(Speaker.Zaazil, "Espera, espera, me llamo Záazil y mi objetivo también es cuidar y preservar el cenote y todo lo que se encuentre dentro."),
            new DialogueLine(Speaker.Zaazil, "¡Te lo juro! Dame una oportunidad para explorar y, si quieres, acompañarme y vigilarme para que veas que digo la verdad."),
            new DialogueLine(Speaker.Chaak, "Está bien Záazil, acepto."),
            new DialogueLine(Speaker.Chaak, "Pero te advierto que en cuanto hagas algo raro recibirás una maldición que los mismísimos dioses me permiten ejecutar sobre todo aquel que osa dañar la naturaleza.")
        };

        [Header("References")]
        [SerializeField] private TMP_Text _dialogueText;
        [SerializeField] private TMP_Text _nameText;
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private Image _aluxeImage;
        [SerializeField] private UiSpriteFrameAnimator _aluxeSpriteAnimator;
        [SerializeField] private Animator _aluxeAnimator;

        [Header("Visual States")]
        [SerializeField] private Color _aluxeSpeakingColor = Color.white;
        [SerializeField] private Color _aluxeListeningColor = new Color(0.566f, 0.566f, 0.566f, 1f);
        [SerializeField] private bool _hideOnAwake = true;

        [Header("Speaker Names")]
        [SerializeField] private string _zaazilNameText = "<i><color=#2ee3ff>Záazil</color></i>";
        [SerializeField] private string _chaakNameText = "<i><color=#f037b8>Cháak</color></i>";

        [Header("Typewriter")]
        [SerializeField, Min(1f)] private float _typewriterCharactersPerSecond = 42f;
        [SerializeField] private bool _useUnscaledTime = true;

        private int _lineIndex = -1;
        private bool _isPlaying;
        private bool _isTyping;
        private int _currentLineCharacterCount;
        private float _visibleCharacterProgress;
        private Action _onCompleted;

        public bool IsPlaying => _isPlaying;

        private void Awake()
        {
            ResolveReferences();

            if (_hideOnAwake)
                HideImmediate();
        }

        private void Update()
        {
            if (!_isPlaying)
                return;

            if (WasAdvancePressed())
            {
                Advance();
                return;
            }

            UpdateTypewriter();
        }

        public bool StartDialogue(Action onCompleted = null)
        {
            if (_isPlaying)
                return false;

            _onCompleted = onCompleted;
            _lineIndex = 0;
            _isPlaying = true;

            Show();
            ApplyCurrentLine();
            return true;
        }

        public void Advance()
        {
            if (!_isPlaying)
                return;

            if (_isTyping)
            {
                CompleteTypewriter();
                return;
            }

            if (_lineIndex < Dialogue.Length - 1)
            {
                _lineIndex++;
                ApplyCurrentLine();
                return;
            }

            CompleteDialogue();
        }

        public void HideImmediate()
        {
            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 0f;
                _canvasGroup.interactable = false;
                _canvasGroup.blocksRaycasts = false;
            }

            SetAluxeSpeaking(false);
            ResetTypewriter();
        }

        private void CompleteDialogue()
        {
            _isPlaying = false;
            HideImmediate();

            Action completed = _onCompleted;
            _onCompleted = null;
            completed?.Invoke();
        }

        private void Show()
        {
            gameObject.SetActive(true);

            if (_canvasGroup == null)
                return;

            _canvasGroup.alpha = 1f;
            _canvasGroup.interactable = true;
            _canvasGroup.blocksRaycasts = true;
        }

        private void ApplyCurrentLine()
        {
            if (_lineIndex < 0 || _lineIndex >= Dialogue.Length)
                return;

            DialogueLine line = Dialogue[_lineIndex];

            if (_dialogueText != null)
                StartTypewriter(line.Text);

            if (_nameText != null)
                _nameText.text = GetSpeakerName(line.Speaker);

            SetAluxeSpeaking(line.Speaker == Speaker.Chaak);
        }

        private void UpdateTypewriter()
        {
            if (!_isTyping || _dialogueText == null)
                return;

            float deltaTime = _useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            _visibleCharacterProgress += _typewriterCharactersPerSecond * deltaTime;

            int visibleCharacters = Mathf.FloorToInt(_visibleCharacterProgress);
            _dialogueText.maxVisibleCharacters = Mathf.Clamp(visibleCharacters, 0, _currentLineCharacterCount);

            if (_dialogueText.maxVisibleCharacters >= _currentLineCharacterCount)
                CompleteTypewriter();
        }

        private void StartTypewriter(string text)
        {
            _dialogueText.text = text;
            _dialogueText.ForceMeshUpdate();
            _currentLineCharacterCount = _dialogueText.textInfo.characterCount;

            if (_typewriterCharactersPerSecond <= 0f || _currentLineCharacterCount == 0)
            {
                CompleteTypewriter();
                return;
            }

            _visibleCharacterProgress = 0f;
            _dialogueText.maxVisibleCharacters = 0;
            _isTyping = true;
        }

        private void CompleteTypewriter()
        {
            _isTyping = false;
            _visibleCharacterProgress = _currentLineCharacterCount;

            if (_dialogueText != null)
                _dialogueText.maxVisibleCharacters = _currentLineCharacterCount;
        }

        private void ResetTypewriter()
        {
            _isTyping = false;
            _currentLineCharacterCount = 0;
            _visibleCharacterProgress = 0f;

            if (_dialogueText != null)
                _dialogueText.maxVisibleCharacters = int.MaxValue;
        }

        private string GetSpeakerName(Speaker speaker)
        {
            return speaker == Speaker.Chaak ? _chaakNameText : _zaazilNameText;
        }

        private void SetAluxeSpeaking(bool isSpeaking)
        {
            if (_aluxeImage != null)
                _aluxeImage.color = isSpeaking ? _aluxeSpeakingColor : _aluxeListeningColor;

            if (_aluxeSpriteAnimator != null)
                _aluxeSpriteAnimator.SetPlaying(isSpeaking);

            if (_aluxeAnimator != null)
                _aluxeAnimator.speed = isSpeaking ? 1f : 0f;
        }

        private void ResolveReferences()
        {
            if (_canvasGroup == null)
                _canvasGroup = GetComponent<CanvasGroup>();

            TMP_Text[] texts = GetComponentsInChildren<TMP_Text>(true);
            if (_nameText == null)
                _nameText = FindNameText(texts);

            if (_dialogueText == null)
                _dialogueText = FindDialogueText(texts, _nameText);

            if (_aluxeImage == null)
            {
                Transform aluxe = transform.Find("Aluxe_First");
                _aluxeImage = aluxe != null
                    ? aluxe.GetComponent<Image>()
                    : GetComponentInChildren<Image>(true);
            }

            if (_aluxeSpriteAnimator == null)
                _aluxeSpriteAnimator = GetComponentInChildren<UiSpriteFrameAnimator>(true);

            if (_aluxeAnimator == null)
                _aluxeAnimator = GetComponentInChildren<Animator>(true);
        }

        private static TMP_Text FindNameText(TMP_Text[] texts)
        {
            foreach (TMP_Text text in texts)
            {
                if (text.name.Equals("nombre", StringComparison.OrdinalIgnoreCase))
                    return text;
            }

            foreach (TMP_Text text in texts)
            {
                string value = text.text;
                if (value.Contains("Zaazil", StringComparison.OrdinalIgnoreCase)
                    || value.Contains("Záazil", StringComparison.OrdinalIgnoreCase))
                    return text;
            }

            return null;
        }

        private static TMP_Text FindDialogueText(TMP_Text[] texts, TMP_Text nameText)
        {
            foreach (TMP_Text text in texts)
            {
                if (text != nameText)
                    return text;
            }

            return null;
        }

        private static bool WasAdvancePressed()
        {
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null
                && (Keyboard.current.spaceKey.wasPressedThisFrame
                    || Keyboard.current.enterKey.wasPressedThisFrame
                    || Keyboard.current.numpadEnterKey.wasPressedThisFrame
                    || Keyboard.current.eKey.wasPressedThisFrame))
                return true;

            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
                return true;

            if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
                return true;
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
            if (Input.GetKeyDown(KeyCode.Space)
                || Input.GetKeyDown(KeyCode.Return)
                || Input.GetKeyDown(KeyCode.KeypadEnter)
                || Input.GetKeyDown(KeyCode.E)
                || Input.GetMouseButtonDown(0))
                return true;
#endif

            return false;
        }
    }
}
