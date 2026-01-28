using UnityEngine;
using System.Collections.Generic;
using PlayerTimeData = TimeManager.PlayerTimeData;

/// <summary>
/// 엔딩 씬 순위 스크립트
/// </summary>

public class RankingUI : MonoBehaviour
{
    [Header("References")]
    public GameObject rankingPanel;
    public Transform rankingListParent;
    public GameObject rankingEntryPrefab; // 랭킹 한 줄(row) 프리팹

    [Header("Options")]
    public bool autoPopulateOnStart = false; // 필요할 때만 Start에서 자동 채움

    void Start()
    {
        if (!autoPopulateOnStart) return;

        // TimeManager로부터 현재 랭킹 데이터 요청
        var data = TimeManager.Instance ? TimeManager.Instance.GetRanking() : null;
        if (data != null) ShowRanking(data);
    }

    public void ShowRanking(List<PlayerTimeData> results)
    {
        if (rankingPanel) rankingPanel.SetActive(true);
        if (rankingListParent == null || rankingEntryPrefab == null || results == null)
        {
            //Debug.LogWarning("[RankingUI] refs or data missing.");
            return;
        }

        for (int i = rankingListParent.childCount - 1; i >= 0; i--)
            Destroy(rankingListParent.GetChild(i).gameObject);

        // 결과 데이터 수만큼 랭킹 UI 생성
        for (int i = 0; i < results.Count; i++)
        {
            var row = Instantiate(rankingEntryPrefab, rankingListParent);
            var ui = row.GetComponent<RankingPrefab>();
            if (ui != null) ui.Set(i + 1, results[i].playerName, TimeManager.FormatTime(results[i].finishTime));
        }
    }
}