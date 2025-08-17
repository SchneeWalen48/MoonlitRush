
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class RaceManager : MonoBehaviour
{
    public static RaceManager Instance;

    public int totalLaps = 1;
    //public List<Checkpoint_Me> checkpoints = new List<Checkpoint_Me>();
    public List<RacerInfo> racers = new List<RacerInfo>();
    public EndTrigger endTrigger;

    private void Awake()
    {
        if (Instance == null)
        {
        Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    //checkpoint 등록
    //public void RegisterCheckpoint(Checkpoint_Me cp)
    //{
    //    if (!checkpoints.Contains(cp))
    //        checkpoints.Add(cp);
    //}

    //차량 등록
    public void RegisterRacer(RacerInfo racer)
    {
        if (!racers.Contains(racer))
            racers.Add(racer);
    }

    private void Update()
    {
        UpdateRanking();
    }

    //endtrigger 활성화
    public void ActivateEndTrigger()
    {
        if (endTrigger != null)
        {
            endTrigger.ActiveTrigger();
        }
    }

    void UpdateRanking()
    {
        racers = racers
            //1차 정렬(내림차순): 현재 완료한 바퀴 수
            .OrderByDescending(r => r.lapCounter.currentLap)
            //2차 정렬(내림차순): 통과한 checkpoint 수
            //.ThenByDescending(r => r.lapCounter.nextCheckpointIndex)
            .ThenByDescending(r => r.lapCounter.nextCheckpoint.checkpointId)
            //3차 정렬(오름차순): 다음 checkpoint까지 거리가 짧은 정도
            .ThenBy(r => Vector3.Distance(
                r.transform.position,
            // checkpoints[r.lapCounter.nextCheckpointIndex].transform.position))
            r.lapCounter.nextCheckpoint.transform.position))
                .ToList(); //LINQ 결과 리스트 생성

        //정렬된 리스트 순서대로 랭킹 번호 부여
        for (int i = 0; i < racers.Count; i++)
        {
            racers[i].currentRank = i + 1;
        }
    }
}
