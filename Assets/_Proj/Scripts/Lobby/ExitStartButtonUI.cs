using UnityEngine;

public class ExitStartButtonUI : MonoBehaviour
{
  public GameObject exit;
  public CarSelectionUI carSelectionUI;

  [SerializeField] private AudioSource source;
  public AudioClip buttonSound;

  private void Awake()
  {
    exit.SetActive(false);
  }

  private void Update()
  {
    if (Input.GetKeyUp(KeyCode.Escape))
    {
      OPExitPanel();
    }
  }

  public void GameExit()
  {
    //Debug.Log("게임 종료 요청");        
    Application.Quit();
    source.PlayOneShot(buttonSound);
  }

  public void OPExitPanel()
  {
    exit.SetActive(true);
    source.PlayOneShot(buttonSound);
  }

  public void CloseExitPanel()
  {
    exit.SetActive(false);
    source.PlayOneShot(buttonSound);
  }

  //로비용
  public void GameStart()
  {
    // 선택 정보 저장
    carSelectionUI.SaveSelection();
    UnityEngine.SceneManagement.SceneManager.LoadScene("Game");
    source.PlayOneShot(buttonSound);
  }

  //결과창에서 재시작
  public void ReGameStart()
  {
    UnityEngine.SceneManagement.SceneManager.LoadScene("Game");
    source.PlayOneShot(buttonSound);
  }

  public void Lobby() //로비로 가기 및 돌아가기
  {
    UnityEngine.SceneManagement.SceneManager.LoadScene("Lobby");
    source.PlayOneShot(buttonSound);
  }
}
