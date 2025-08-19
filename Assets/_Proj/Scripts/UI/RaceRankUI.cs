using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class RaceRankUI : MonoBehaviour
{
    [Header("UI References")]
    public List<TextMeshProUGUI> nameTexts; // 이미 배치된 TextMeshProUGUI 리스트
    public List<TextMeshProUGUI> rankTexts;

    void Update()
    {
        var racers = RaceManager.Instance.racers;

        for (int i = 0; i < rankTexts.Count; i++)
        {
            if (i < racers.Count)
            {
                // 순위와 이름 업데이트
                rankTexts[i].text = $"{racers[i].currentRank}";
                rankTexts[i].gameObject.SetActive(true);
            }
            else
            {
                // 남은 텍스트는 비활성화
                rankTexts[i].gameObject.SetActive(false);
            }
        }

        for (int i = 0; i < nameTexts.Count; i++)
        {
            if (i < racers.Count)
            {
                // 순위와 이름 업데이트
                nameTexts[i].text = $"{racers[i].racerName}";
                nameTexts[i].gameObject.SetActive(true);
            }
            else
            {
                // 남은 텍스트는 비활성화
                nameTexts[i].gameObject.SetActive(false);
            }
        }


    }
}
