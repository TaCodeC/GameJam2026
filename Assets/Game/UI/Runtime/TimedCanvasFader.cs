using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public sealed class TimedCanvasFader : MonoBehaviour
{
    public const string DefaultSceneHintName = "Instructions";

    [Header("Canvas")]
    [SerializeField] private Canvas _canvas;
    [SerializeField] private CanvasGroup _canvasGroup;

    [Header("Timing")]
    [SerializeField, Min(0f)] private float _visibleDuration = 4f;
    [SerializeField, Min(0f)] private float _fadeDuration = 0.5f;
    [SerializeField] private bool _playOnStart = true;
    [SerializeField] private bool _disableAfterFade;

    private Coroutine _showRoutine;
    private TMP_Text[] _messageTexts;
    private string[] _originalMessages;
    private bool _hasMessageOverride;

    public static bool ShowSceneHint(string hintName = DefaultSceneHintName)
    {
        return ShowSceneHintInternal(hintName, null);
    }

    public static bool ShowSceneHintMessage(string message, string hintName = DefaultSceneHintName)
    {
        return ShowSceneHintInternal(hintName, message);
    }

    private static bool ShowSceneHintInternal(string hintName, string message)
    {
        bool shown = false;
        Scene activeScene = SceneManager.GetActiveScene();
        TimedCanvasFader[] faders = Resources.FindObjectsOfTypeAll<TimedCanvasFader>();

        for (int i = 0; i < faders.Length; i++)
        {
            TimedCanvasFader fader = faders[i];
            if (fader == null || fader.gameObject.scene != activeScene)
                continue;

            if (!string.Equals(fader.gameObject.name, hintName, StringComparison.Ordinal))
                continue;

            if (string.IsNullOrEmpty(message))
                fader.Show();
            else
                fader.Show(message);

            shown = true;
        }

        return shown;
    }

    private void Reset()
    {
        _canvas = GetComponent<Canvas>();
        _canvasGroup = GetComponent<CanvasGroup>();
    }

    private void Awake()
    {
        ResolveCanvasGroup();
    }

    private void Start()
    {
        if (_playOnStart)
        {
            Show();
            return;
        }

        HideInstant();
    }

    public void Show()
    {
        RestoreOriginalMessages();
        ShowResolved();
    }

    public void Show(string message)
    {
        ApplyMessageOverride(message);
        ShowResolved();
    }

    private void ShowResolved()
    {
        ResolveCanvasGroup();

        if (_canvasGroup == null)
            return;

        SetVisible(true);

        if (_showRoutine != null)
            StopCoroutine(_showRoutine);

        _showRoutine = StartCoroutine(ShowRoutine());
    }

    public void HideInstant()
    {
        ResolveCanvasGroup();

        if (_canvasGroup == null)
            return;

        if (_showRoutine != null)
        {
            StopCoroutine(_showRoutine);
            _showRoutine = null;
        }

        SetVisible(false);
        SetAlpha(0f);
        RestoreOriginalMessages();
    }

    private IEnumerator ShowRoutine()
    {
        SetVisible(true);
        SetAlpha(1f);

        if (_visibleDuration > 0f)
            yield return new WaitForSecondsRealtime(_visibleDuration);

        yield return FadeTo(0f, _fadeDuration);
        RestoreOriginalMessages();

        if (_disableAfterFade)
        {
            _showRoutine = null;
            SetVisible(false);
            yield break;
        }

        _showRoutine = null;
    }

    private IEnumerator FadeTo(float targetAlpha, float duration)
    {
        float startAlpha = _canvasGroup.alpha;

        if (duration <= 0f)
        {
            SetAlpha(targetAlpha);
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);
            SetAlpha(Mathf.Lerp(startAlpha, targetAlpha, progress));
            yield return null;
        }

        SetAlpha(targetAlpha);
    }

    private void ResolveCanvasGroup()
    {
        if (_canvas == null)
            _canvas = GetComponent<Canvas>();

        if (_canvasGroup == null && _canvas != null)
            _canvasGroup = _canvas.GetComponent<CanvasGroup>();

        if (_canvasGroup == null)
            _canvasGroup = GetComponent<CanvasGroup>();

        if (_canvasGroup == null && _canvas != null)
            _canvasGroup = _canvas.gameObject.AddComponent<CanvasGroup>();
    }

    private void ApplyMessageOverride(string message)
    {
        ResolveMessageTexts();
        if (_messageTexts == null || _messageTexts.Length == 0)
            return;

        if (_originalMessages == null || _originalMessages.Length != _messageTexts.Length)
        {
            _originalMessages = new string[_messageTexts.Length];
            for (int i = 0; i < _messageTexts.Length; i++)
                _originalMessages[i] = _messageTexts[i] != null ? _messageTexts[i].text : string.Empty;
        }

        for (int i = 0; i < _messageTexts.Length; i++)
        {
            if (_messageTexts[i] != null)
                _messageTexts[i].text = message;
        }

        _hasMessageOverride = true;
    }

    private void RestoreOriginalMessages()
    {
        if (!_hasMessageOverride || _originalMessages == null)
            return;

        ResolveMessageTexts();
        int count = Mathf.Min(_messageTexts.Length, _originalMessages.Length);
        for (int i = 0; i < count; i++)
        {
            if (_messageTexts[i] != null)
                _messageTexts[i].text = _originalMessages[i];
        }

        _hasMessageOverride = false;
    }

    private void ResolveMessageTexts()
    {
        if (_messageTexts == null || _messageTexts.Length == 0)
            _messageTexts = GetComponentsInChildren<TMP_Text>(true);
    }

    private void SetVisible(bool visible)
    {
        GameObject target = _canvas != null ? _canvas.gameObject : _canvasGroup.gameObject;
        target.SetActive(visible);
    }

    private void SetAlpha(float alpha)
    {
        _canvasGroup.alpha = alpha;
        _canvasGroup.blocksRaycasts = alpha > 0.01f;
        _canvasGroup.interactable = alpha > 0.99f;
    }
}
