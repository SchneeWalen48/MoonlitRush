using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ExitStartButtonUI : MonoBehaviour
{
    public GameObject exit;

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
        exit?.SetActive(false);
    }

    public void GameStart()
    {
        SceneManager.LoadScene("SampleScene");
    }

    public void Lobby() //로비로 가기 및 돌아가기
    {
        SceneManager.LoadScene("Lobby");
    }


}
