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
            .OrderByDescending(r =>
            {
                var lc = r.lapCounter;
                var cpm = lc.checkpointManager;

                int N = cpm.allCheckPoints.Count;
                int nextId = lc.nextCheckpoint.checkpointId; // 1-based
                int prevIx = (nextId - 2 + N) % N;
                var prev = cpm.allCheckPoints[prevIx];

                float segLen = Vector3.Distance(prev.transform.position, lc.nextCheckpoint.transform.position);
                float dist = Vector3.Distance(r.transform.position, lc.nextCheckpoint.transform.position);
                float progress = (segLen > 0f) ? Mathf.Clamp01(1f - dist / segLen) : 0f;

                return lc.currentLap * N + (nextId - 1) + progress;
            })
            .ToList();

        for (int i = 0; i < racers.Count; i++)
            racers[i].currentRank = i + 1;
    }

    public void SaveRanking()
    {
        racers = racers.Where(r => r != null).ToList();
        racers.Sort((a, b) => a.currentRank.CompareTo(b.currentRank));
    }
}