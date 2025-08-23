
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

public class RaceManager : MonoBehaviour
{
    public static RaceManager Instance;

    public int totalLaps = 2;
  //  public List<Checkpoint> checkpoints = new List<Checkpoint>();
    public List<RacerInfo> racers = new List<RacerInfo>();
    public EndTrigger endTrigger;
    public List<RacerInfo> finalRank = new List<RacerInfo>();
    public TextMeshProUGUI lapText;
    public TextMeshProUGUI timeText;
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

    //void UpdateRanking()
    //{
    //    racers = racers
    //       // .Where(r => r != null) // null인 요소 제외
    //        //1차 정렬(내림차순): 현재 완료한 바퀴 수
    //        .OrderByDescending(r => r.lapCounter.currentLap)
    //        //2차 정렬(내림차순): 통과한 checkpoint 수
    //        //.ThenByDescending(r => r.lapCounter.nextCheckpointIndex)
    //        .ThenByDescending(r => r.lapCounter.nextCheckpoint.checkpointId)
    //        //3차 정렬(오름차순): 다음 checkpoint까지 거리가 짧은 정도
    //        .ThenBy(r => Vector3.Distance(
    //            r.transform.position,
    //        // checkpoints[r.lapCounter.nextCheckpointIndex].transform.position))
    //        r.lapCounter.nextCheckpoint.transform.position))
    //            .ToList(); //LINQ 결과 리스트 생성

    //    //정렬된 리스트 순서대로 랭킹 번호 부여
    //    for (int i = 0; i < racers.Count; i++)
    //    {
    //        racers[i].currentRank = i + 1;
    //    }
    //}
    void UpdateRanking()
    {
        int N;
        racers = racers
            .Where(r => r != null && r.lapCounter != null && r.lapCounter.checkpointManager != null && r.lapCounter.nextCheckpoint != null)
            .OrderByDescending(r =>
            {
                var lc = r.lapCounter;
                var cpm = lc.checkpointManager;
                N = cpm.allCheckPoints.Count;

                var next = lc.nextCheckpoint;
                var nextId = next.checkpointId;                 // 1-based라고 가정
                var prevIdx = (nextId - 2 + N) % N;             // 바로 직전에 통과한 cp idx
                var prev = cpm.allCheckPoints[prevIdx];

                float segLen = Vector3.Distance(prev.transform.position, next.transform.position);
                float distToNext = Vector3.Distance(r.transform.position, next.transform.position);
                float segProgress01 = (segLen > 0f) ? Mathf.Clamp01(1f - distToNext / segLen) : 0f;

                // 진행도 점수 (클수록 앞)
                return lc.currentLap * N + (nextId - 1) + segProgress01;
            })
            .ToList();

        for (int i = 0; i < racers.Count; i++)
            racers[i].currentRank = i + 1;
    }

    //랭킹 저장

    public void SaveRanking()
    {
        finalRank = racers.OrderBy(r => r.currentRank).ToList();
    }
}
