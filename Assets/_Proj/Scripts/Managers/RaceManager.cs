using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class RaceManager : MonoBehaviour
{
  public List<PlayerRaceStatus> allPlayers;

  void Update()
  {
    CalculateRankings();
  }

  void CalculateRankings()
  {
    allPlayers = allPlayers.OrderByDescending(p => p.currLap)
      .ThenByDescending(p => p.lastCheckpointId)
      .ThenBy(p => p.distToNextCP)
      .ToList();

    for (int i = 0; i < allPlayers.Count; i++)
    {
      Debug.Log($"{i + 1}위 : {allPlayers[i].gameObject.name}");
    }
  }
}
