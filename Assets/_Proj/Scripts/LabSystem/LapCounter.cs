
using TMPro;
using UnityEngine;

public class LapCounter : MonoBehaviour
{
  public TextMeshProUGUI lapText;
  public TextMeshProUGUI timeText;
  private float lapStartTime;

  public int currentLap = 0;
  //public int nextCheckpointIndex = 0;
  private bool raceFinished = false;
  public Checkpoint nextCheckpoint;  // 다음 체크포인트의 오브젝트를 직접 참조

  public CheckpointManager checkpointManager;

  private void Start()
  {
    if (checkpointManager == null)
    {
      checkpointManager = FindObjectOfType<CheckpointManager>();
    }

    if (checkpointManager != null && checkpointManager.allCheckPoints.Count > 0)
    {
      nextCheckpoint = checkpointManager.allCheckPoints[0];
    }

    lapStartTime = Time.time;
  }

  private void Update()
  {
    if (raceFinished) return;

    if (timeText != null && !raceFinished)
    {
      var tm = TimeManager.Instance;
      if (tm != null)
      {
        
      timeText.text = tm.GetFormatRaceTime();
      }
      else
      {
        print("TimeManager is null");
      }
    }
  }




  public void PassCheckpoint(Checkpoint passedCheckpoint)
  {
    if (raceFinished) return;

    // 통과한 체크포인트가 다음 체크포인트와 일치하는지 확인
    if (passedCheckpoint == nextCheckpoint)
    {
      // 다음 체크포인트를 설정
      nextCheckpoint = passedCheckpoint.nextCheckpoint;

      // 마지막 체크포인트를 통과했는지 확인
      if (passedCheckpoint.isFinalCheckpoint)
      {
        currentLap++;

        nextCheckpoint = checkpointManager.allCheckPoints[0];
        Debug.Log("1바퀴 끝. 체크포인트 다시 시작");

        if (currentLap >= RaceManager.Instance.totalLaps)
        {
          raceFinished = true;
          Debug.Log(name + " Finished Race!");

          //최종 기록 전달
          TimeManager.Instance.StopTimer();
          //TimeManager.Instance.RecordFinishTime(
          //    gameObject.name,
          //    TimeManager.Instance.RaceDuration);

          RaceManager.Instance.ActivateEndTrigger();
        }

        //if (currentLap == RaceManager.Instance.totalLaps)
        //{

        //}


        //TimeManager.Instance.StopTimer(); //시간 저장
        ////최종 기록 전달
        //TimeManager.Instance.RecordFinishTime(
        //    gameObject.name,
        //    TimeManager.Instance.RaceDuration);

        //currentLap++;
        //// 랩이 특정 횟수를 채웠는지 확인
        //if (currentLap >= RaceManager.Instance.totalLaps)
        //{
        //    raceFinished = true;
        //    Debug.Log(name + " Finished Race!");

        //    //체크포인트 다 돌면 활성화
        //    RaceManager.Instance.ActivateEndTrigger();
        //}


      }
      if (lapText != null)
      {
        lapText.text = $"{currentLap + 1} / {RaceManager.Instance.totalLaps} Lap";
      }


    }


  }

  //public void PassCheckpoint(int checkpointIndex)
  //{
  //    if (raceFinished) return;

  //    if (checkpointIndex == nextCheckpointIndex)
  //    {
  //        nextCheckpointIndex++;

  //        if (nextCheckpointIndex >= RaceManager.Instance.checkpoints.Count)
  //        {
  //            nextCheckpointIndex = 0;
  //            currentLap = 1;
  //            raceFinished = true;

  //            Debug.Log(name + " Finished Race!");
  //        }
  //    }
  //}
}
