using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class SceneChanger : MonoBehaviour
{
    public void ChangeScene(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }
}
