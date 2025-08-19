using UnityEngine;
using System.Collections.Generic;
using TMPro; // Text Mesh Pro(Not Legacy Text)

public class RankingUI : MonoBehaviour
{
  public GameObject rankingPanel; // for RankingUI activate/deactivate
  [Tooltip("Layout Group")]
  public Transform rankingListParent; // ex) layout group
  public GameObject rankingEntryPrefab; // A prefab that displays one player's record

  public void ShowRanking(List<TimeManager.PlayerTimeData> ranking)
  {
    rankingPanel.SetActive(true); // open the panel

    foreach (Transform child in rankingListParent)
      Destroy(child.gameObject); // Remove existing content

    // Iterate through the ranking list and create UI items one by one
    for (int i = 0; i < ranking.Count; i++)
    {
      var entry = Instantiate(rankingEntryPrefab, rankingListParent);
      var text = entry.GetComponent<TextMeshProUGUI>();
      text.text = $"{i + 1}. {ranking[i].playerName} - {FormatTime(ranking[i].finishTime)}";
    }
  }

  string FormatTime(float time)
  {
    int minutes = Mathf.FloorToInt(time / 60f);
    float seconds = time % 60f;
    return $"{minutes:00}:{seconds:00.00}";
  }
}
