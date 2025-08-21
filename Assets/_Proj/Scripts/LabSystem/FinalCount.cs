using System.Collections;
using TMPro;
using UnityEngine;
using System;

public class FinalCount : MonoBehaviour
{
    public static FinalCount Instance;

    public TextMeshProUGUI finalCountText;
    public int defaultSeconds = 10;

    public CarController playerCar;
    public AICarController AICar;
    
    private bool isGameEnding = false;
    public event Action OnCountdownFinished;

    private void Awake()
    {
        Instance = this;
        if (finalCountText) finalCountText.gameObject.SetActive(false);
    }
    public void StartCountdown(int seconds)
    {
        if (isGameEnding) return;
        isGameEnding = true;
        
        finalCountText.gameObject.SetActive(true);
        StartCoroutine(CoCount(seconds > 0 ? seconds : defaultSeconds));
    }

    public void Finish() => StartCountdown(defaultSeconds);
    IEnumerator CoCount(int sec)
    {
        for (int i = sec; i > 0; i--)
        {
            finalCountText.text = i.ToString();
            yield return new WaitForSeconds(1f);
        }

        finalCountText.text = "Finish!";
        yield return new WaitForSeconds(1f);

        finalCountText.text = "";
        finalCountText.gameObject.SetActive(false);

        OnCountdownFinished?.Invoke();
    }


    //private void Awake()
    //{
    //    finalCountText.gameObject.SetActive(false);
    //}

    //public void Finish()
    //{
    //    if (isGameEnding == false) {
    //    isGameEnding = true;
    //        TimeManager.Instance.PauseTimer(); //시간 멈춤
    //        Debug.Log("카운트 시작");
    //        finalCountText.gameObject.SetActive(true);
    //        StartCoroutine(EndCount());
    //    }
    //}

    //IEnumerator EndCount()
    //{
    //    for (int i = 10; i > 0; i--)
    //    {
    //        finalCountText.text = i.ToString();
    //        yield return new WaitForSeconds(1f);
    //    }

    //    finalCountText.text = "Game End!";
    //    yield return new WaitForSeconds(1f);

    //    finalCountText.text = "";
    //    //엔딩
    //}

}

//플레이어, AI에 결승선 트리거 처리 코드 추가