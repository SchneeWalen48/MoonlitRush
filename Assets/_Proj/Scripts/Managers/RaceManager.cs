using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using static TimeManager;

public class RaceManager : MonoBehaviour
{
  public static RaceManager Instance;

  [Header("Race")]
  public int totalLaps = 2;
  public List<RacerInfo> racers = new List<RacerInfo>();
  public EndTrigger endTrigger;

  [Header("UI (optional)")]
  public TextMeshProUGUI lapText;
  public TextMeshProUGUI timeText;

  public int finishCounter = 0; //완주 순서 부여용
  void Awake()
  {
    if (Instance == null) Instance = this;
    else Destroy(gameObject);
  }

  public void RegisterRacer(RacerInfo racer)
  {
    if (racer && !racers.Contains(racer))
      racers.Add(racer);
  }

  void Update() => UpdateRanking();

  public void ActivateEndTrigger()
  {
    if (endTrigger) endTrigger.ActiveTrigger();
  }

  void UpdateRanking()
  {
    racers = racers
        .Where(r => r && r.lapCounter && r.lapCounter.checkpointManager)
        .ToList();

    racers.Sort((a, b) =>
    {
      // 완주자 우선 + finishOrder (작을수록 먼저 들어옴)
      if (a.finished != b.finished) return a.finished ? -1 : 1;
      if (a.finished && b.finished)
      {
        int cmpF = a.finishOrder.CompareTo(b.finishOrder);
        if (cmpF != 0) return cmpF;
      }

      // 랩 수 (내림차순)
      int lapA = a.lapCounter?.currentLap ?? 0;
      int lapB = b.lapCounter?.currentLap ?? 0;
      int cmpLap = lapB.CompareTo(lapA);
      if (cmpLap != 0) return cmpLap;

      // 다음 체크포인트 인덱스 (내림차순: 큰 값이 앞)
      int cpA = a.lapCounter?.nextCheckpoint ? a.lapCounter.nextCheckpoint.checkpointId : 0;
      int cpB = b.lapCounter?.nextCheckpoint ? b.lapCounter.nextCheckpoint.checkpointId : 0;
      int cmpCp = cpB.CompareTo(cpA);
      if (cmpCp != 0) return cmpCp;

      // 다음 체크포인트까지 거리 (오름차순: 가까운 쪽이 앞)
      float dA = float.MaxValue, dB = float.MaxValue;
      if (a.lapCounter?.nextCheckpoint)
        dA = Vector3.Distance(a.transform.position, a.lapCounter.nextCheckpoint.transform.position);
      if (b.lapCounter?.nextCheckpoint)
        dB = Vector3.Distance(b.transform.position, b.lapCounter.nextCheckpoint.transform.position);
      return dA.CompareTo(dB);
    });
    // 등수 번호 부여
    for (int i = 0; i < racers.Count; i++)
      racers[i].currentRank = i + 1;
  }
  
  public void SaveRanking()
  {
    racers = racers.Where(r => r != null).ToList();
    racers.Sort((a, b) => a.currentRank.CompareTo(b.currentRank));
  }
}