using System.Collections.Generic;
using UnityEngine;

public class ChekcpointManager : MonoBehaviour
{
    public List<Checkpoint> allCheckPoints;
    public Checkpoint firstCheckpoint;

    void Start()
    {
        InitCheckPoints();

        // 모든 차량의 첫 번째 체크포인트를 설정합니다.
        foreach (var racer in RaceManager.Instance.racers)
        {
            racer.lapCounter.nextCheckpoint = firstCheckpoint;
        }
    }

    void InitCheckPoints()
    {
        for (int i = 0; i < allCheckPoints.Count; i++)
        {
            if (i < allCheckPoints.Count - 1)
            {
                allCheckPoints[i].SetNextCheckpoint(allCheckPoints[i + 1]);
            }
            else
            {
                allCheckPoints[i].SetNextCheckpoint(allCheckPoints[0]);
                allCheckPoints[i].isFinalCheckpoint = true;
            }
        }

        firstCheckpoint = allCheckPoints[0]; 
    }
}