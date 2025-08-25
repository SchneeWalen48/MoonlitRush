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
    }
    else
    {
      Destroy(gameObject); // Duplicate prevention
    }
  }

  // Load by scene name
  public static void LoadScene(string sceneName)
  {
    if(!string.IsNullOrEmpty(sceneName))
      SceneManager.LoadScene(sceneName);
  }

  // Load by scene index
  public void LoadScene(int sceneIdx)
  {
    if(sceneIdx >= 0 && sceneIdx < SceneManager.sceneCountInBuildSettings)
      SceneManager.LoadScene(sceneIdx);
  }

  // Reload current scene
  public void ReloadScene()
  {
    SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
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
