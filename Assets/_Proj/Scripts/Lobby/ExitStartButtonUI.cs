using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ExitStartButtonUI : MonoBehaviour
{
    public void GameExit()
    {
        Debug.Log("게임 종료 요청");
        Application.Quit();
    }

    public void GameStart()
    {
        SceneManager.LoadScene("SampleScene");
    }
}
