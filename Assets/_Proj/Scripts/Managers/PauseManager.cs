using UnityEngine;

public class PauseManager : MonoBehaviour
{
  private bool isPaused = false;

  void Update()
  {
    if (Input.GetKeyDown(KeyCode.Escape))
    {
      if (!isPaused)
        PauseGame();
      else
        ResumeGame();
    }
  }

  public void PauseGame()
  {
    isPaused = true;
    Time.timeScale = 0f; // Pause the game
    TimeManager.Instance?.PauseTimer(); // Pause timer
  }

  public void ResumeGame()
  {
    isPaused = false;
    Time.timeScale = 1f;
    TimeManager.Instance?.ResumeTimer(); // Resume Timer(Not Restart)
  }
}
