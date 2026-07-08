using System;
using System.Collections;
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

    public static bool ShowSceneHint(string hintName = DefaultSceneHintName)
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

            fader.Show();
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
    }

    private IEnumerator ShowRoutine()
    {
        SetVisible(true);
        SetAlpha(1f);

        if (_visibleDuration > 0f)
            yield return new WaitForSecondsRealtime(_visibleDuration);

        yield return FadeTo(0f, _fadeDuration);

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
