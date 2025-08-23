using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ExitStartButtonUI : MonoBehaviour
{
    public GameObject exit;
    public CarSelectionUI carSelectionUI;
    
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
        Debug.Log("게임 종료 요청");        
        Application.Quit();
    }

    public void OPExitPanel()
    {
        exit.SetActive(true);
    }
    
   public void CloseExitPanel()
    {
        exit.SetActive(false);
    }

    //로비용
    public void GameStart()
    {
        // 선택 정보 저장
        carSelectionUI.SaveSelection();
    UnityEngine.SceneManagement.SceneManager.LoadScene("Game");
    }

    //결과창에서 재시작
    public void ReGameStart()
    {
    UnityEngine.SceneManagement.SceneManager.LoadScene("Game");
    }
        
    public void Lobby() //로비로 가기 및 돌아가기
    {
    UnityEngine.SceneManagement.SceneManager.LoadScene("Lobby");
    }

   


}
