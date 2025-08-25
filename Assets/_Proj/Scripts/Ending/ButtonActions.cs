using UnityEngine;

public class ButtonActions : MonoBehaviour
{
  [SerializeField] private AudioSource source;
  public AudioClip clip;
  public void GoToLobby()
  {
    SceneManagers.LoadScene("Lobby");
    source.PlayOneShot(clip);
  }

  public void QuitGame()
  {
#if UNITY_EDITOR
    UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
            source.PlayOneShot(clip);
#endif
  }
}
