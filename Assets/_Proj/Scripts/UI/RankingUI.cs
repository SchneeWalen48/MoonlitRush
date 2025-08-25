using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using PlayerTimeData = TimeManager.PlayerTimeData;

public class RankingUI : MonoBehaviour
{
    [Header("References")]
    public GameObject rankingPanel;      // 패널 GameObject
    public Transform rankingListParent;  // Content (위에 설명한 Content)
    public GameObject rankingEntryPrefab;

    [Header("Options")]
    public bool autoPopulateOnStart = false;

    void Start()
    {
        if (!autoPopulateOnStart) return;
        var data = TimeManager.Instance ? TimeManager.Instance.GetRankingFull() : null;
        if (data != null) ShowRanking(data);
    }

    public void ShowRanking(List<PlayerTimeData> results)
    {
        if (!rankingPanel || !rankingListParent || !rankingEntryPrefab || results == null)
        {
            Debug.LogError("[RankingUI] refs or data missing"); return;
        }

        // 패널 ON
        if (!rankingPanel.activeSelf) rankingPanel.SetActive(true);

        // 기존 항목 제거
        for (int i = rankingListParent.childCount - 1; i >= 0; i--)
            Destroy(rankingListParent.GetChild(i).gameObject);

        Debug.Log($"[RankingUI] spawn {results.Count} entries");

        // N명 전부 생성
        for (int i = 0; i < results.Count; i++)
        {
            var row = Instantiate(rankingEntryPrefab, rankingListParent, false); // worldPositionStays=false 중요
            var rt = row.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.anchorMin = new Vector2(0, 1);
                rt.anchorMax = new Vector2(1, 1);
                rt.pivot = new Vector2(0.5f, 1);
                rt.anchoredPosition = Vector2.zero;
                rt.localScale = Vector3.one;

                // 행 높이(없으면 LayoutElement 추가)
                var le = row.GetComponent<LayoutElement>() ?? row.AddComponent<LayoutElement>();
                if (le.preferredHeight <= 0f) le.preferredHeight = 36f;
                le.flexibleHeight = 0f;
            }

            var p = results[i];
            var timeText = TimeManager.FormatTimeOrNoRecord(p);
            var ui = row.GetComponent<RankingPrefab>();
            if (ui) ui.Set(i + 1, p.playerName, timeText);
            else
            {
                // 백업: 이름으로 TMP_Text 찾기
                var tms = row.GetComponentsInChildren<TMP_Text>(true);
                TMP_Text r = null, n = null, t = null;
                foreach (var tx in tms)
                {
                    var id = tx.name.ToLower();
                    if (id.Contains("rank")) r = tx;
                    else if (id.Contains("name")) n = tx;
                    else if (id.Contains("time")) t = tx;
                }
                if (r) r.text = (i + 1).ToString();
                if (n) n.text = p.playerName;
                if (t) t.text = timeText;
            }
        }

        // 레이아웃 즉시 갱신
        var contentRT = rankingListParent as RectTransform;
        if (contentRT) LayoutRebuilder.ForceRebuildLayoutImmediate(contentRT);
        Canvas.ForceUpdateCanvases();
    }
}