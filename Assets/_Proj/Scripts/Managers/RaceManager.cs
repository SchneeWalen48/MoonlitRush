using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

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
        .Where(r => r && r.lapCounter && r.lapCounter.checkpointManager && r.lapCounter.nextCheckpoint)
        .OrderBy((a) => 0)
        .ThenBy(a => 0)
        .ToList();

    //    .OrderByDescending(r =>
    //    {
    //        var lc = r.lapCounter;
    //        var cpm = lc.checkpointManager;

    //        int N = cpm.allCheckPoints.Count;
    //        int nextId = lc.nextCheckpoint.checkpointId; // 1-based
    //        int prevIx = (nextId - 2 + N) % N;
    //        var prev = cpm.allCheckPoints[prevIx];

    //        float segLen = Vector3.Distance(prev.transform.position, lc.nextCheckpoint.transform.position);
    //        float dist = Vector3.Distance(r.transform.position, lc.nextCheckpoint.transform.position);
    //        float progress = (segLen > 0f) ? Mathf.Clamp01(1f - dist / segLen) : 0f;

    //        return lc.currentLap * N + (nextId - 1) + progress;
    //    })
    //    .ToList();

    //for (int i = 0; i < racers.Count; i++)
    //    racers[i].currentRank = i + 1;

    racers.Sort((a, b) =>
    {
      if (a.finished != b.finished) return a.finished ? -1 : 1;
      if(a.finished && b.finished)
      {
        int cmpF = a.finishOrder.CompareTo(b.finishOrder);
        if (cmpF != 0) return cmpF;
      }

      int lapA = a.lapCounter?.currentLap ?? 0;
      int lapB = b.lapCounter?.currentLap ?? 0;
      int cmpLap = lapB.CompareTo(lapA);
      if(cmpLap != 0) return cmpLap;

      int cpA = a.lapCounter?.nextCheckpoint ? a.lapCounter.nextCheckpoint.checkpointId : 0;
      int cpB = b.lapCounter?.nextCheckpoint ? b.lapCounter.nextCheckpoint.checkpointId : 0;
      int cmpCp = cpB.CompareTo(cpA);
      if (cmpCp != 0) return cmpCp;

      float dA = float.MaxValue, dB = float.MaxValue;
      if (a.lapCounter?.nextCheckpoint)
        dA = Vector3.Distance(a.transform.position, a.lapCounter.nextCheckpoint.transform.position);
      if (b.lapCounter?.nextCheckpoint)
        dB = Vector3.Distance(b.transform.position, b.lapCounter.nextCheckpoint.transform.position);
      return dA.CompareTo(dB);
    });
    for (int i = 0; i < racers.Count; i++)
      racers[i].currentRank = i + 1;
  }

  public void SaveRanking()
    {
        racers = racers.Where(r => r != null).ToList();
        racers.Sort((a, b) => a.currentRank.CompareTo(b.currentRank));
    }
}