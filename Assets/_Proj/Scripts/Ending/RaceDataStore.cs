using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RaceDataStore : MonoBehaviour
{
  public static RaceDataStore Instance;
  public static List<TimeManager.PlayerTimeData> RankingData {  get; set; }
}
