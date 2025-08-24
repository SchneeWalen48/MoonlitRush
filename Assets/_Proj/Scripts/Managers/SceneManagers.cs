using UnityEngine;
using UnityEngine.SceneManagement;

// Use it by attaching it to an empty game object.
public class SceneManagers : MonoBehaviour
{
  public static SceneManagers Instance;

  void Awake()
  {
    if(Instance == null)
    {
      Instance = this;
      DontDestroyOnLoad(gameObject); // Maintain when changing scenes
    }
    else
    {
      Destroy(gameObject); // Duplicate prevention
    }
  }

  // Load by scene name
  public void LoadScene(string sceneName)
  {
    if(!string.IsNullOrEmpty(sceneName))
      UnityEngine.SceneManagement.SceneManager.LoadScene(sceneName);
  }

  // Load by scene index
  public void LoadScene(int sceneIdx)
  {
    if(sceneIdx >= 0 && sceneIdx < UnityEngine.SceneManagement.SceneManager.sceneCountInBuildSettings)
      UnityEngine.SceneManagement.SceneManager.LoadScene(sceneIdx);
  }

  // Reload current scene
  public void ReloadScene()
  {
    UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
  }

  public void QuitGame()
  {
#if UNITY_EDITOR
    UnityEditor.EditorApplication.isPlaying = false;
#else
    Application.Quit();
#endif
  }
}
