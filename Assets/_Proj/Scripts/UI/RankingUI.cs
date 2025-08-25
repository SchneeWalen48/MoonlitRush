using UnityEngine;
using System.Collections.Generic;
using PlayerTimeData = TimeManager.PlayerTimeData;

public class RankingUI : MonoBehaviour
{
    [Header("References")]
    public GameObject rankingPanel;
    public Transform rankingListParent;
    public GameObject rankingEntryPrefab;

    [Header("Options")]
    public bool autoPopulateOnStart = false; // 필요할 때만 Start에서 자동 채움

    void Start()
    {
        if (!autoPopulateOnStart) return;

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

        for (int i = 0; i < results.Count; i++)
        {
            var row = Instantiate(rankingEntryPrefab, rankingListParent);
            var ui = row.GetComponent<RankingPrefab>();
            if (ui != null) ui.Set(i + 1, results[i].playerName, TimeManager.FormatTime(results[i].finishTime));
        }
    }
}