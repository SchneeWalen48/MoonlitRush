using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class FinalCount : MonoBehaviour
{
    public TextMeshProUGUI finalCountText;

   // public CarController playerCar;
    public AICarController AICar;
    

    private bool isGameEnding = false;

    private void Awake()
    {
        finalCountText.gameObject.SetActive(false);
    }

    public void Finish()
    {
        if (isGameEnding == false) {
        isGameEnding = true;
            Debug.Log("카운트 시작");
            finalCountText.gameObject.SetActive(true);
            StartCoroutine(EndCount());
        }
    }

    IEnumerator EndCount()
    {
        for (int i = 10; i > 0; i--)
        {
            finalCountText.text = i.ToString();
            yield return new WaitForSeconds(1f);
        }

        finalCountText.text = "Game End!";
        yield return new WaitForSeconds(1f);

        finalCountText.text = "";
        //엔딩
    }
    
}

//플레이어, AI에 결승선 트리거 처리 코드 추가