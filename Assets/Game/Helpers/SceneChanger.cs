using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class SceneChanger : MonoBehaviour
{
    [SerializeField] private CanvasGroup _fadeCanvasGroup;
    [SerializeField, Min(0f)] private float _fadeOutDuration = 0.35f;
    [SerializeField] private bool _useUnscaledTime = true;

    private bool _isChangingScene;

    public void ChangeScene(string sceneName)
    {
        if (_isChangingScene)
            return;

        if (_fadeCanvasGroup == null || _fadeOutDuration <= 0f)
        {
            SceneManager.LoadScene(sceneName);
            return;
        }

        StartCoroutine(FadeOutThenChangeScene(sceneName));
    }

    private IEnumerator FadeOutThenChangeScene(string sceneName)
    {
        _isChangingScene = true;
        _fadeCanvasGroup.gameObject.SetActive(true);
        _fadeCanvasGroup.alpha = 0f;
        _fadeCanvasGroup.interactable = false;
        _fadeCanvasGroup.blocksRaycasts = true;

        float elapsed = 0f;
        while (elapsed < _fadeOutDuration)
        {
            elapsed += _useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            _fadeCanvasGroup.alpha = Mathf.Clamp01(elapsed / _fadeOutDuration);
            yield return null;
        }

        _fadeCanvasGroup.alpha = 1f;
        Time.timeScale = 1f;
        SceneManager.LoadScene(sceneName);
    }
}
