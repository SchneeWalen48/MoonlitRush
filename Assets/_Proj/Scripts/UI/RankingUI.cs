using UnityEngine;
using System.Collections.Generic;

// Ending Ranking UI
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
      var rankPrefab = Instantiate(rankingEntryPrefab, rankingListParent);
      var rankUI = rankPrefab.GetComponent<RankingPrefab>();
      rankUI.SetUI(i + 1, ranking[i].playerName, ranking[i].finishTime);
    }
  }
}
