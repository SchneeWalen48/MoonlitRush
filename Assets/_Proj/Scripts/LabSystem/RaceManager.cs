
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.TextCore;

public class RaceManager : MonoBehaviour
{
    public static RaceManager Instance;

    public int totalLaps = 1;
    public List<Checkpoint> checkpoints = new List<Checkpoint>();
    public List<RacerInfo> racers = new List<RacerInfo>();

    private void Awake()
    {
        Instance = this;
    }

    public void RegisterCheckpoint(Checkpoint cp)
    {
        if (!checkpoints.Contains(cp))
            checkpoints.Add(cp);
    }

    public void RegisterRacer(RacerInfo racer)
    {
        if (!racers.Contains(racer))
            racers.Add(racer);
    }

    private void Update()
    {
        UpdateRanking();
    }

    void UpdateRanking()
    {
        racers = racers
            .OrderByDescending(r => r.lapCounter.currentLap)
            .ThenByDescending(r => r.lapCounter.nextCheckpointIndex)
            .ThenBy(r => Vector3.Distance(
                r.transform.position,
                checkpoints[r.lapCounter.nextCheckpointIndex].transform.position))
            .ToList();

        for (int i = 0; i < racers.Count; i++)
        {
            racers[i].currentRank = i + 1;
        }
    }
}
